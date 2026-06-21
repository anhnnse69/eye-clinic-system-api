using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreateDoctorScheduleService
{
    /// <summary>
    /// Handles creating one or more doctor schedules (shifts) across
    /// multiple work dates, auto-generating hourly time slots and
    /// skipping date/shift combinations that already exist.
    /// </summary>
    public class CreateDoctorScheduleService : ICreateDoctorScheduleService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
            _doctorRepo;
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>
            _roomRepo;
        private readonly IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>
            _scheduleQueryRepo;
        private readonly IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext>
            _scheduleCommandRepo;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>
            _slotCommandRepo;
        private readonly AppDbContext _dbContext;

        private static readonly Dictionary<ShiftType, (int StartHour, int EndHour)>
            ShiftHourRanges = new()
            {
                [ShiftType.MORNING] = (8, 12),
                [ShiftType.AFTERNOON] = (12, 17),
                [ShiftType.EVENING] = (17, 20),
            };

        private const int DefaultMaxPatientsPerSlot = 8;

        /// <summary>
        /// Initializes a new instance of <see cref="CreateDoctorScheduleService"/>.
        /// </summary>
        public CreateDoctorScheduleService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepo,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> scheduleQueryRepo,
            IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext> scheduleCommandRepo,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotCommandRepo,
            AppDbContext dbContext)
        {
            _doctorRepo = doctorRepo;
            _roomRepo = roomRepo;
            _scheduleQueryRepo = scheduleQueryRepo;
            _scheduleCommandRepo = scheduleCommandRepo;
            _slotCommandRepo = slotCommandRepo;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Creates schedules for every (work date, shift type) combination
        /// requested, skipping combinations that already exist for the doctor.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <param name="request">
        /// Work dates, shift types and room for the new schedules.
        /// </param>
        /// <returns>
        /// A successful response containing created and skipped schedule items.
        /// </returns>
        public async Task<ApiResponse<CreateDoctorScheduleResponse>> Process(
            Guid userId,
            CreateDoctorScheduleRequest request)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);
            var room = await ResolveActiveRoomAsync(request.RoomId);
            var shiftTypes = ParseShiftTypes(request.ShiftTypes);
            ValidateRequest(request, shiftTypes);
            var distinctDates = request.WorkDates.Distinct().ToList();
            var distinctShifts = shiftTypes.Distinct().ToList();
            var existingPairs = await FetchExistingSchedulePairsAsync(
                doctorProfile.Id,
                distinctDates,
                distinctShifts);
            var response = await BuildScheduleResultsAsync(
                doctorProfile.Id,
                room,
                distinctDates,
                distinctShifts,
                existingPairs);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the active doctor profile for the specified user.
        /// Throws when not found.
        /// </summary>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(
            Guid userId)
        {
            var doctorProfile = await _doctorRepo
                .FindByCondition(d =>
                    d.UserId == userId &&
                    d.IsActive)
                .FirstOrDefaultAsync();
            if (doctorProfile is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());
            return doctorProfile;
        }

        /// <summary>
        /// Resolves the active facility room by id.
        /// Throws when not found.
        /// </summary>
        private async Task<FacilityRoom> ResolveActiveRoomAsync(Guid roomId)
        {
            var room = await _roomRepo
                .FindByCondition(r =>
                    r.Id == roomId &&
                    r.IsActive)
                .FirstOrDefaultAsync();
            if (room is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4004.ToString());
            return room;
        }

        /// <summary>
        /// Parses raw shift type strings into valid <see cref="ShiftType"/>
        /// enum values, ignoring any unparseable entries.
        /// </summary>
        private static List<ShiftType> ParseShiftTypes(List<string> shiftTypeStrings)
        {
            return shiftTypeStrings
                .Select(s => Enum.TryParse<ShiftType>(s, ignoreCase: true, out var parsed)
                    ? parsed
                    : (ShiftType?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
        }

        /// <summary>
        /// Validates the request: at least one work date and one shift type,
        /// and no past dates allowed.
        /// </summary>
        private static void ValidateRequest(
            CreateDoctorScheduleRequest request,
            List<ShiftType> shiftTypes)
        {
            if (request.WorkDates == null || request.WorkDates.Count == 0)
                throw new ArgumentException(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            if (shiftTypes == null || shiftTypes.Count == 0)
                throw new ArgumentException(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (request.WorkDates.Any(d => d < today))
                throw new ArgumentException(
                    GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// Fetches the set of (work date, shift type) pairs that already
        /// exist for the doctor, used to detect duplicates across the
        /// requested date × shift combination.
        /// </summary>
        private async Task<HashSet<(DateOnly, ShiftType)>> FetchExistingSchedulePairsAsync(
            Guid doctorId,
            List<DateOnly> workDates,
            List<ShiftType> shiftTypes)
        {
            var dateTimes = workDates
                .Select(d => d.ToDateTime(TimeOnly.MinValue))
                .ToList();
            var existing = await _scheduleQueryRepo
                .FindByCondition(s =>
                    s.DoctorId == doctorId &&
                    shiftTypes.Contains(s.ShiftType) &&
                    dateTimes.Contains(s.WorkDate) && !s.IsDeleted)
                .Select(s => new { s.WorkDate, s.ShiftType })
                .ToListAsync();
            return existing
                .Select(e => (DateOnly.FromDateTime(e.WorkDate), e.ShiftType))
                .ToHashSet();
        }

        /// <summary>
        /// Iterates every (work date, shift type) combination, creating a
        /// schedule when no duplicate exists, or recording it as skipped.
        /// </summary>
        private async Task<CreateDoctorScheduleResponse> BuildScheduleResultsAsync(
            Guid doctorId,
            FacilityRoom room,
            List<DateOnly> workDates,
            List<ShiftType> shiftTypes,
            HashSet<(DateOnly, ShiftType)> existingPairs)
        {
            var response = new CreateDoctorScheduleResponse();

            foreach (var workDate in workDates)
            {
                foreach (var shiftType in shiftTypes)
                {
                    if (existingPairs.Contains((workDate, shiftType)))
                    {
                        response.Skipped.Add(new SkippedScheduleItem
                        {
                            WorkDate = workDate,
                            ShiftType = shiftType,
                            Reason = "Đã tồn tại ca này trong ngày đã chọn",
                        });
                        continue;
                    }
                    var schedule = await CreateScheduleWithSlotsAsync(
                        doctorId, room.Id, workDate, shiftType);
                    response.Created.Add(new CreatedScheduleItem
                    {
                        ScheduleId = schedule.Id,
                        WorkDate = workDate,
                        ShiftType = shiftType,
                        RoomName = room.RoomName,
                        SlotCount = schedule.TimeSlots?.Count ?? 0,
                    });
                }
            }
            return response;
        }

        /// <summary>
        /// Creates a single <see cref="DoctorSchedule"/> using the room's
        /// foreign key value directly, avoiding EF Core re-inserting the
        /// already-existing <see cref="FacilityRoom"/>.
        /// </summary>
        private async Task<DoctorSchedule> CreateScheduleWithSlotsAsync(
            Guid doctorId,
            Guid roomId,
            DateOnly workDate,
            ShiftType shiftType)
        {
            var schedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                WorkDate = workDate.ToDateTime(TimeOnly.MinValue),
                ShiftType = shiftType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _dbContext.Entry(schedule).Property("RoomId").CurrentValue = roomId;
                await _scheduleCommandRepo.CreateAsync(schedule);
                await _scheduleCommandRepo.SaveChangesAsync();
            var slots = GenerateHourlySlots(schedule.Id, workDate, shiftType);
            foreach (var slot in slots)
                await _slotCommandRepo.CreateAsync(slot);
                await _slotCommandRepo.SaveChangesAsync();
            schedule.TimeSlots = slots;
            return schedule;
        }

        /// <summary>
        /// Generates 1-hour <see cref="TimeSlot"/> entries spanning
        /// the configured hour range of the given shift type.
        /// </summary>
        private static List<TimeSlot> GenerateHourlySlots(
            Guid scheduleId,
            DateOnly workDate,
            ShiftType shiftType)
        {
            var (startHour, endHour) = ShiftHourRanges[shiftType];
            var slots = new List<TimeSlot>();
            for (var hour = startHour; hour < endHour; hour++)
            {
                var start = workDate.ToDateTime(new TimeOnly(hour, 0));
                var end = start.AddHours(1);
                slots.Add(new TimeSlot
                {
                    Id = Guid.NewGuid(),
                    ScheduleId = scheduleId,
                    StartTime = start,
                    EndTime = end,
                    MaxPatients = DefaultMaxPatientsPerSlot,
                    CurrentPatients = 0,
                    Status = SlotStatus.AVAILABLE,
                });
            }
            return slots;
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<CreateDoctorScheduleResponse> CreateSuccessResponse(
            CreateDoctorScheduleResponse response)
        {
            return ApiResponse<CreateDoctorScheduleResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}