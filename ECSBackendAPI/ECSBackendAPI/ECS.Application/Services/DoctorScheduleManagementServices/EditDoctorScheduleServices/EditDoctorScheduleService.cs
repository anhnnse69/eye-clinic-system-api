using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorScheduleManagementServices.EditDoctorScheduleServices
{
    /// <summary>
    /// Service responsible for modifying an existing doctor's work schedule.
    /// handles business validations such as clinic boundaries, booked slot locks, and duplicate detection.
    /// </summary>
    public class EditDoctorScheduleService : IEditDoctorScheduleService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepo;
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> _roomRepo;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> _scheduleQueryRepo;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditDoctorScheduleService"/> class.
        /// Injecting required read-only repositories and the primary database context via Dependency Injection (DI).
        /// </summary>
        /// <param name="doctorRepo">Repository interface to query and validate active doctor profiles.</param>
        /// <param name="roomRepo">Repository interface to lookup and validate clinic room availability.</param>
        /// <param name="staffClinicRepo">Repository interface to verify receptionist identities and clinic mapping boundaries.</param>
        /// <param name="scheduleQueryRepo">Repository interface specialized in verifying existing schedules to prevent duplicate conflicts.</param>
        /// <param name="dbContext">The primary Entity Framework database context used to track changes, handle shadow properties, and commit transactions.</param>
        public EditDoctorScheduleService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepo,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> scheduleQueryRepo,
            AppDbContext dbContext)
        {
            _doctorRepo = doctorRepo;
            _roomRepo = roomRepo;
            _staffClinicRepo = staffClinicRepo;
            _scheduleQueryRepo = scheduleQueryRepo;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Orchestrates the business workflow to edit a doctor's schedule.
        /// </summary>
        public async Task<ApiResponse<EditDoctorScheduleResponse>> Process(
            Guid receptionistUserId,
            Guid doctorId,
            Guid scheduleId,
            EditDoctorScheduleRequest request)
        {
            var receptionistClinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var doctorProfile = await ResolveActiveDoctorProfileAsync(doctorId);
            EnsureSameClinic(receptionistClinicId, doctorProfile.ClinicId);
            var schedule = await ResolveOwnedScheduleAsync(doctorProfile.Id, scheduleId);
            EnsureNoBookedSlots(schedule);
            var room = await ResolveTargetRoomAsync(schedule, request.RoomId, receptionistClinicId);
            var effectiveWorkDate = request.WorkDate?.ToDateTime(TimeOnly.MinValue) ?? schedule.WorkDate;
            await EnsureNoDuplicateOnNewDateAsync(doctorProfile.Id, schedule, request.WorkDate);
            await EnsureNoRoomConflictAsync(doctorProfile.Id, schedule.Id, room.Id, effectiveWorkDate, schedule.ShiftType);

            ApplyChanges(schedule, room, request);
            await _dbContext.SaveChangesAsync();
            var response = BuildResponse(schedule, room);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the Clinic ID associated with the operating receptionist.
        /// </summary>
        private async Task<Guid> ResolveReceptionistClinicIdAsync(Guid receptionistUserId)
        {
            var staffClinic = await _staffClinicRepo
                .FindByCondition(sc => sc.UserId == receptionistUserId && sc.IsActive)
                .FirstOrDefaultAsync();
            if (staffClinic is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());
            return staffClinic.ClinicId;
        }

        /// <summary>
        /// Retrieves the doctor profile and ensures the doctor is currently active.
        /// </summary>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(Guid doctorId)
        {
            var doctorProfile = await _doctorRepo
                .FindByCondition(d => d.Id == doctorId && d.IsActive)
                .FirstOrDefaultAsync();
            if (doctorProfile is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4011.ToString());
            return doctorProfile;
        }

        /// <summary>
        /// Ensures that the receptionist and the doctor belong to the exact same clinic boundary.
        /// </summary>
        private static void EnsureSameClinic(Guid receptionistClinicId, Guid doctorClinicId)
        {
            if (receptionistClinicId != doctorClinicId)
                throw new UnauthorizedAccessException(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// Fetches the target schedule including its related time slots, filtering out logically deleted records.
        /// </summary>
        private async Task<DoctorSchedule> ResolveOwnedScheduleAsync(Guid doctorId, Guid scheduleId)
        {
            var schedule = await _dbContext.Set<DoctorSchedule>()
                .Include(s => s.TimeSlots)
                .FirstOrDefaultAsync(s =>
                    s.Id == scheduleId &&
                    s.DoctorId == doctorId &&
                    !s.IsDeleted);
            if (schedule is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4012.ToString());
            return schedule;
        }

        /// <summary>
        /// Blocks any modifications if a patient has already reserved/booked a time slot within this schedule.
        /// </summary>
        private static void EnsureNoBookedSlots(DoctorSchedule schedule)
        {
            var hasBookedSlot = HasBookedSlot(schedule);
            if (hasBookedSlot)
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4013.ToString());
        }

        /// <summary>
        /// Checks if the schedule contains any booked time slots.
        /// Extracted to separate method for 100% branch coverage testing.
        /// </summary>
        private static bool HasBookedSlot(DoctorSchedule schedule)
        {
            var slots = schedule.TimeSlots;
            if (slots == null) return false;
            foreach (var slot in slots)
            {
                if (slot.Status == SlotStatus.BOOKED) return true;
            }
            return false;
        }

        /// <summary>
        /// Resolves the valid room instance for the update, falling back to the existing room if no new RoomId is provided.
        /// </summary>
        private async Task<FacilityRoom> ResolveTargetRoomAsync(
            DoctorSchedule schedule,
            Guid? newRoomId,
            Guid clinicId)
        {
            var targetRoomId = newRoomId ?? GetCurrentRoomId(schedule);
            if (targetRoomId is null)
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4008.ToString());
            var room = await _roomRepo
                .FindByCondition(r =>
                    r.Id == targetRoomId.Value &&
                    r.IsActive &&
                    r.ClinicId == clinicId)
                .FirstOrDefaultAsync();
            if (room is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4019.ToString());
            return room;
        }

        /// <summary>
        /// Extracts the current shadow-property or regular property value of 'RoomId' from the EF Core tracking entry.
        /// </summary>
        private Guid? GetCurrentRoomId(DoctorSchedule schedule)
        {
            return (Guid?)_dbContext.Entry(schedule).Property("RoomId").CurrentValue;
        }

        /// <summary>
        /// Validates that changing the date does not create a overlapping shift type clash for the same doctor.
        /// </summary>
        private async Task EnsureNoDuplicateOnNewDateAsync(
            Guid doctorId,
            DoctorSchedule schedule,
            DateOnly? newWorkDate)
        {
            if (!newWorkDate.HasValue) return;
            var newWorkDateTime = newWorkDate.Value.ToDateTime(TimeOnly.MinValue);
            var duplicateExists = await _scheduleQueryRepo
                .FindByCondition(s =>
                    s.DoctorId == doctorId &&
                    s.Id != schedule.Id && // Exclude the current schedule being edited
                    s.ShiftType == schedule.ShiftType &&
                    s.WorkDate == newWorkDateTime &&
                    !s.IsDeleted)
                .AnyAsync();

            if (duplicateExists)
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4015.ToString());
        }

        /// <summary>
        /// Ensures the target room isn't already occupied by a DIFFERENT doctor
        /// on the same (work date, shift type) combination. Excludes the schedule
        /// being edited itself, and excludes conflicts with the same doctor
        /// (a doctor can't conflict with their own schedule).
        /// </summary>
        private async Task EnsureNoRoomConflictAsync(
            Guid doctorId,
            Guid scheduleId,
            Guid targetRoomId,
            DateTime workDate,
            ShiftType shiftType)
        {
            var conflictSchedules = await _dbContext.Set<DoctorSchedule>()
                .Where(s =>
                    s.Id != scheduleId &&
                    !s.IsDeleted &&
                    s.WorkDate == workDate &&
                    s.ShiftType == shiftType &&
                    s.DoctorId != doctorId)
                .Select(s => new
                {
                    s.DoctorId,
                    RoomId = EF.Property<Guid>(s, "RoomId"),
                })
                .ToListAsync();

            var hasConflict = conflictSchedules.Any(s => s.RoomId == targetRoomId);

            if (hasConflict)
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4058.ToString());
        }

        /// <summary>
        /// Updates the entity state and updates internal time slots to reflect a new date structure if required.
        /// </summary>
        private void ApplyChanges(
            DoctorSchedule schedule,
            FacilityRoom room,
            EditDoctorScheduleRequest request)
        {
            if (request.WorkDate.HasValue)
            {
                ShiftSlotsToNewDate(schedule, request.WorkDate.Value);
                schedule.WorkDate = request.WorkDate.Value.ToDateTime(TimeOnly.MinValue);
            }
            _dbContext.Entry(schedule).Property("RoomId").CurrentValue = room.Id;
            schedule.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Recalculates and updates the StartTime and EndTime of all children slots to align with the new calendar date.
        /// </summary>
        private static void ShiftSlotsToNewDate(DoctorSchedule schedule, DateOnly newWorkDate)
        {
            var slots = schedule.TimeSlots;
            if (slots == null) return;
            foreach (var slot in slots)
            {
                var duration = slot.EndTime - slot.StartTime;
                var newStart = newWorkDate.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime.TimeOfDay));
                slot.StartTime = newStart;
                slot.EndTime = newStart.Add(duration);
            }
        }

        /// <summary>
        /// Constructs a data transfer object output mapping data out from updated schemas.
        /// </summary>
        private static EditDoctorScheduleResponse BuildResponse(DoctorSchedule schedule, FacilityRoom room)
        {
            return new EditDoctorScheduleResponse
            {
                ScheduleId = schedule.Id,
                WorkDate = DateOnly.FromDateTime(schedule.WorkDate),
                ShiftType = schedule.ShiftType,
                RoomName = room.RoomName,
                UpdatedAt = schedule.UpdatedAt,
            };
        }

        /// <summary>
        /// Standardizes a successful operations layer payload wrap template response wrapper.
        /// </summary>
        private static ApiResponse<EditDoctorScheduleResponse> CreateSuccessResponse(
            EditDoctorScheduleResponse response)
        {
            return ApiResponse<EditDoctorScheduleResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), response);
        }
    }
}