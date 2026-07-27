using ECS.Application.Common.Helpers;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorScheduleManagementServices.BatchCreateDoctorScheduleServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService
{
    /// <summary>
    /// Handles creating schedules in bulk for multiple (doctor, room)
    /// assignments across multiple work dates and shifts in a single request.
    /// Reuses the same clinic-hour clipping and duplicate-skip rules as
    /// <see cref="CreateDoctorScheduleService"/>, plus an additional rule:
    /// two different doctors cannot share the same room on the same
    /// (work date, shift) — including rooms already occupied outside this batch.
    /// </summary>
    public class BatchCreateDoctorScheduleService : IBatchCreateDoctorScheduleService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepo;
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> _roomRepo;
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepo;
        private readonly IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> _scheduleQueryRepo;
        private readonly IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext> _scheduleCommandRepo;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotCommandRepo;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of <see cref="BatchCreateDoctorScheduleService"/>.
        /// </summary>
        public BatchCreateDoctorScheduleService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepo,
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepo,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> scheduleQueryRepo,
            IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext> scheduleCommandRepo,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotCommandRepo,
            AppDbContext dbContext)
        {
            _staffClinicRepo = staffClinicRepo;
            _doctorRepo = doctorRepo;
            _roomRepo = roomRepo;
            _clinicRepo = clinicRepo;
            _scheduleQueryRepo = scheduleQueryRepo;
            _scheduleCommandRepo = scheduleCommandRepo;
            _slotCommandRepo = slotCommandRepo;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Creates schedules for every (doctor, room) assignment × work date ×
        /// shift combination requested, skipping combinations that already
        /// exist, fall outside clinic hours, or would double-book a room
        /// across two different doctors on the same date/shift.
        /// </summary>
        public async Task<ApiResponse<BatchCreateDoctorScheduleResponse>> Process(
            Guid receptionistUserId,
            BatchCreateDoctorScheduleRequest request)
        {
            // 1. Validate request
            ValidateBatchRequest(request);
            // 2. Resolve clinic information
            var clinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var clinic = await ResolveClinicAsync(clinicId);
            // 3. Prepare distinct dates and shifts
            var distinctDates = request.WorkDates.Distinct().ToList();
            var distinctShifts = ScheduleHelper.ParseShiftTypes(request.ShiftTypes);
            // 4. Get distinct room IDs from assignments
            var distinctRoomIds = GetDistinctRoomIds(request.Assignments);
            // 5. Fetch existing room occupancy to prevent conflicts
            var roomOccupancy = await FetchRoomOccupancyAsync(
                distinctDates,
                distinctShifts,
                distinctRoomIds);
            // 6. Build batch results
            var response = await BuildBatchResultsAsync(
                request.Assignments,
                clinic,
                distinctDates,
                distinctShifts,
                roomOccupancy);
            // 7. Return success response
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Gets the distinct room IDs from the requested doctor-room assignments.
        /// </summary>
        /// <param name="assignments">The doctor-room assignments.</param>
        /// <returns>A list of distinct room IDs.</returns>
        private static List<Guid> GetDistinctRoomIds(
            List<DoctorRoomAssignment> assignments)
        {
            return assignments
                .Select(a => a.RoomId)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Iterates every requested (doctor, room) assignment, resolving and
        /// validating each, then building its per-doctor created/skipped result.
        /// Assignments with an inactive/foreign doctor or room are silently
        /// excluded from the response, matching the previous behavior.
        /// </summary>
        private async Task<BatchCreateDoctorScheduleResponse> BuildBatchResultsAsync(
            List<DoctorRoomAssignment> assignments,
            Clinic clinic,
            List<DateOnly> workDates,
            List<ShiftType> shiftTypes,
            Dictionary<(DateOnly, ShiftType, Guid), Guid> roomOccupancy)
        {
            var response = new BatchCreateDoctorScheduleResponse();
            var clinicId = clinic.Id;

            foreach (var assignment in assignments)
            {
                // Resolve and validate doctor
                var doctorProfile = await ResolveActiveDoctorInClinicAsync(assignment.DoctorId, clinicId);
                if (doctorProfile is null) continue;
                // Resolve and validate room
                var room = await ResolveActiveRoomInClinicAsync(assignment.RoomId, clinicId);
                if (room is null) continue;
                // Build result for this doctor
                var doctorResult = await BuildDoctorResultAsync(
                    doctorProfile,
                    room,
                    clinic,
                    workDates,
                    shiftTypes,
                    roomOccupancy);
                response.Results.Add(doctorResult);
            }
            return response;
        }

        /// <summary>
        /// Builds the created/skipped result for a single doctor by walking
        /// every (work date, shift type) combination requested for them.
        /// </summary>
        private async Task<DoctorBatchResult> BuildDoctorResultAsync(
            DoctorProfile doctorProfile,
            FacilityRoom room,
            Clinic clinic,
            List<DateOnly> workDates,
            List<ShiftType> shiftTypes,
            Dictionary<(DateOnly, ShiftType, Guid), Guid> roomOccupancy)
        {
            var doctorResult = new DoctorBatchResult
            {
                DoctorId = doctorProfile.Id,
                DoctorName = doctorProfile.User?.FullName ?? doctorProfile.Id.ToString(),
            };

            // Fetch existing schedules for this doctor to detect duplicates
            var existingPairs = await FetchExistingPairsAsync(doctorProfile.Id, workDates, shiftTypes);

            foreach (var workDate in workDates)
            {
                foreach (var shiftType in shiftTypes)
                {
                    // Check for duplicate schedule
                    if (existingPairs.Contains((workDate, shiftType)))
                    {
                        doctorResult.Skipped.Add(ScheduleHelper.CreateDuplicateSkipItem(workDate, shiftType));
                        continue;
                    }
                    // Check if shift is within clinic hours
                    if (!ScheduleHelper.TryClipShiftToClinicHours(shiftType, clinic, out var start, out var end))
                    {
                        doctorResult.Skipped.Add(ScheduleHelper.CreateOutsideClinicHoursSkipItem(workDate, shiftType, clinic));
                        continue;
                    }
                    // Check for room conflict
                    var roomKey = (workDate, shiftType, room.Id);
                    if (roomOccupancy.TryGetValue(roomKey, out var occupantDoctorId) &&
                        occupantDoctorId != doctorProfile.Id)
                    {
                        doctorResult.Skipped.Add(ScheduleHelper.CreateRoomConflictSkipItem(workDate, shiftType, room.RoomName));
                        continue;
                    }
                    // Create schedule and slots
                    var (schedule, slotCount) = await CreateScheduleWithSlotsAsync(
                        doctorProfile.Id,
                        room.Id,
                        workDate,
                        start,
                        end);
                    // Update room occupancy
                    roomOccupancy[roomKey] = doctorProfile.Id;
                    // Add to created list
                    doctorResult.Created.Add(new CreatedScheduleItem
                    {
                        ScheduleId = schedule.Id,
                        WorkDate = workDate,
                        ShiftType = shiftType,
                        RoomName = room.RoomName,
                        SlotCount = slotCount,
                    });
                }
            }

            return doctorResult;
        }

        /// <summary>
        /// Creates a single <see cref="DoctorSchedule"/> using the room's
        /// foreign key value directly, then generates its 30-minute slots.
        /// Returns the schedule together with its slot count so the caller
        /// does not need to navigate a (possibly null) navigation property.
        /// </summary>
        private async Task<(DoctorSchedule Schedule, int SlotCount)> CreateScheduleWithSlotsAsync(
            Guid doctorId,
            Guid roomId,
            DateOnly workDate,
            TimeOnly shiftStart,
            TimeOnly shiftEnd)
        {
            var schedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                WorkDate = workDate.ToDateTime(TimeOnly.MinValue),
                ShiftType = ScheduleHelper.GetShiftTypeFromBounds(shiftStart, shiftEnd),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _dbContext.Entry(schedule).Property("RoomId").CurrentValue = roomId;
            await _scheduleCommandRepo.CreateAsync(schedule);
            await _scheduleCommandRepo.SaveChangesAsync();

            var slots = ScheduleHelper.GenerateTimeSlots(schedule.Id, workDate, shiftStart, shiftEnd);
            foreach (var slot in slots)
                await _slotCommandRepo.CreateAsync(slot);
            await _slotCommandRepo.SaveChangesAsync();

            schedule.TimeSlots = slots;
            return (schedule, slots.Count);
        }

        /// <summary>
        /// Fetches the set of (work date, shift type) pairs that already
        /// exist for the doctor, used to detect duplicates.
        /// </summary>
        private async Task<HashSet<(DateOnly, ShiftType)>> FetchExistingPairsAsync(
            Guid doctorId,
            List<DateOnly> dates,
            List<ShiftType> shifts)
        {
            var dateTimes = dates.Select(d => d.ToDateTime(TimeOnly.MinValue)).ToList();
            var existing = await _scheduleQueryRepo
                .FindByCondition(s =>
                    s.DoctorId == doctorId &&
                    shifts.Contains(s.ShiftType) &&
                    dateTimes.Contains(s.WorkDate) &&
                    !s.IsDeleted)
                .Select(s => new { s.WorkDate, s.ShiftType })
                .ToListAsync();

            return existing
                .Select(e => (DateOnly.FromDateTime(e.WorkDate), e.ShiftType))
                .ToHashSet();
        }

        /// <summary>
        /// Retrieves room occupancy for the specified dates and shifts.
        /// </summary>
        /// <param name="dates">Working dates.</param>
        /// <param name="shifts">Shift types.</param>
        /// <param name="roomIds">Room IDs.</param>
        /// <returns>
        /// A dictionary mapping (WorkDate, ShiftType, RoomId) to DoctorId.
        /// </returns>
        private async Task<Dictionary<(DateOnly WorkDate, ShiftType ShiftType, Guid RoomId), Guid>>
            FetchRoomOccupancyAsync(List<DateOnly> dates, List<ShiftType> shifts, List<Guid> roomIds)
        {
            var dateTimes = dates.Select(d => d.ToDateTime(TimeOnly.MinValue)).ToList();

            var existing = await _dbContext.Set<DoctorSchedule>()
                .Where(s =>
                    !s.IsDeleted &&
                    shifts.Contains(s.ShiftType) &&
                    dateTimes.Contains(s.WorkDate))
                .Select(s => new
                {
                    s.WorkDate,
                    s.ShiftType,
                    s.DoctorId,
                    RoomId = EF.Property<Guid>(s, "RoomId"),
                })
                .Where(x => roomIds.Contains(x.RoomId))
                .ToListAsync();

            var occupancy = new Dictionary<(DateOnly, ShiftType, Guid), Guid>();
            foreach (var e in existing)
            {
                var key = (DateOnly.FromDateTime(e.WorkDate), e.ShiftType, e.RoomId);
                if (!occupancy.ContainsKey(key))
                    occupancy[key] = e.DoctorId;
            }

            return occupancy;
        }

        /// <summary>
        /// Resolves the active clinic ID of a receptionist.
        /// </summary>
        private async Task<Guid> ResolveReceptionistClinicIdAsync(Guid userId)
        {
            var sc = await _staffClinicRepo
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .FirstOrDefaultAsync();

            if (sc is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());

            return sc.ClinicId;
        }

        /// <summary>
        /// Resolves an active clinic by its ID.
        /// </summary>
        private async Task<Clinic> ResolveClinicAsync(Guid clinicId)
        {
            var clinic = await _clinicRepo
                .FindByCondition(c => c.Id == clinicId && c.IsActive)
                .FirstOrDefaultAsync();

            if (clinic is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());

            return clinic;
        }

        /// <summary>
        /// Resolves an active doctor profile belonging to the given clinic.
        /// Returns null (rather than throwing) so the caller can skip this
        /// assignment without failing the whole batch.
        /// </summary>
        private async Task<DoctorProfile?> ResolveActiveDoctorInClinicAsync(Guid doctorId, Guid clinicId)
        {
            var doctorProfile = await _doctorRepo
                .FindByCondition(d => d.Id == doctorId && d.IsActive)
                .FirstOrDefaultAsync();

            return doctorProfile is not null && doctorProfile.ClinicId == clinicId
                ? doctorProfile
                : null;
        }

        /// <summary>
        /// Resolves an active facility room belonging to the given clinic.
        /// Returns null (rather than throwing) so the caller can skip this
        /// assignment without failing the whole batch.
        /// </summary>
        private async Task<FacilityRoom?> ResolveActiveRoomInClinicAsync(Guid roomId, Guid clinicId)
        {
            return await _roomRepo
                .FindByCondition(r => r.Id == roomId && r.IsActive && r.ClinicId == clinicId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Validates the batch schedule creation request.
        /// </summary>
        private static void ValidateBatchRequest(BatchCreateDoctorScheduleRequest request)
        {
            if (request.Assignments == null || request.Assignments.Count == 0)
                throw new ArgumentException(GeneralCode.APP_MESSAGE_4001.ToString());

            ScheduleHelper.ValidateWorkDates(request.WorkDates);
            ScheduleHelper.ValidateShiftTypes(request.ShiftTypes);
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<BatchCreateDoctorScheduleResponse> CreateSuccessResponse(
            BatchCreateDoctorScheduleResponse response)
        {
            return ApiResponse<BatchCreateDoctorScheduleResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), response);
        }
    }
}