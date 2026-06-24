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
    /// Handles editing an existing doctor schedule: changing the work date
    /// (moving the shift and its slots) and/or the assigned room.
    /// Blocked entirely if any slot in the schedule is already booked.
    /// </summary>
    public class EditDoctorScheduleService : IEditDoctorScheduleService
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

        /// <summary>
        /// Initializes a new instance of the service.
        /// </summary>
        public EditDoctorScheduleService(
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
        /// Edits a doctor's schedule (work date and/or room).
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <param name="scheduleId">Identifier of the schedule to edit.</param>
        /// <param name="request">Fields to update; null fields are left unchanged.</param>
        /// <returns>A successful response containing the updated schedule.</returns>
        public async Task<ApiResponse<EditDoctorScheduleResponse>> Process(
            Guid userId,
            Guid scheduleId,
            EditDoctorScheduleRequest request)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);
            var schedule = await ResolveOwnedScheduleAsync(doctorProfile.Id, scheduleId);
            EnsureNoBookedSlots(schedule);
            var room = await ResolveTargetRoomAsync(schedule, request.RoomId);
            await EnsureNoDuplicateOnNewDateAsync(doctorProfile.Id, schedule, request.WorkDate);
            ApplyChanges(schedule, room, request);
            await PersistChangesAsync(schedule);
            var response = BuildResponse(schedule, room);
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
                    GeneralCode.APP_MESSAGE_4011.ToString());
            return doctorProfile;
        }

        /// <summary>
        /// Resolves the schedule, ensuring it belongs to the given doctor,
        /// including its time slots. Throws when not found.
        /// </summary>
        private async Task<DoctorSchedule> ResolveOwnedScheduleAsync(
            Guid doctorId,
            Guid scheduleId)
        {
            var schedule = await _scheduleQueryRepo
                .FindByCondition(s =>
                    s.Id == scheduleId &&
                    s.DoctorId == doctorId)
                .Include(s => s.TimeSlots)
                .FirstOrDefaultAsync();
            if (schedule is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4012.ToString());
            return schedule;
        }

        /// <summary>
        /// Throws if any slot in the schedule is currently booked,
        /// since edits are not allowed once a patient has booked.
        /// </summary>
        private static void EnsureNoBookedSlots(DoctorSchedule schedule)
        {
            var hasBookedSlot = (schedule.TimeSlots ?? [])
                .Any(slot => slot.Status == SlotStatus.BOOKED);
            if (hasBookedSlot)
                throw new InvalidOperationException(
                    GeneralCode.APP_MESSAGE_4013.ToString());
        }

        /// <summary>
        /// Resolves the target room for the update. Returns the schedule's
        /// current room when no new room id is provided.
        /// </summary>
        private async Task<FacilityRoom> ResolveTargetRoomAsync(
            DoctorSchedule schedule,
            Guid? newRoomId)
        {
            var roomId = newRoomId ?? GetCurrentRoomId(schedule);

            if (roomId is null)
                throw new InvalidOperationException(
                    GeneralCode.APP_MESSAGE_4008.ToString());
            var room = await _roomRepo
                .FindByCondition(r =>
                    r.Id == roomId.Value &&
                    r.IsActive)
                .FirstOrDefaultAsync();
            if (room is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4019.ToString());
            return room;
        }

        /// <summary>
        /// Reads the current room id via the shadow FK property.
        /// Returns null if the schedule currently has no room assigned.
        /// </summary>
        private Guid? GetCurrentRoomId(DoctorSchedule schedule)
        {
            return (Guid?)_dbContext
                .Entry(schedule)
                .Property("RoomId")
                .CurrentValue;
        }

        /// <summary>
        /// Throws if the doctor already has a schedule with the same shift
        /// type on the target work date (excluding the current schedule).
        /// Does nothing when no new work date is requested.
        /// </summary>
        private async Task EnsureNoDuplicateOnNewDateAsync(
            Guid doctorId,
            DoctorSchedule schedule,
            DateOnly? newWorkDate)
        {
            if (!newWorkDate.HasValue)
                return;
            var newWorkDateTime = newWorkDate.Value.ToDateTime(TimeOnly.MinValue);
            var duplicateExists = await _scheduleQueryRepo
                .FindByCondition(s =>
                    s.DoctorId == doctorId &&
                    s.Id != schedule.Id &&
                    s.ShiftType == schedule.ShiftType &&
                    s.WorkDate == newWorkDateTime &&
                    !s.IsDeleted)
                .AnyAsync();
            if (duplicateExists)
                throw new InvalidOperationException(
                    GeneralCode.APP_MESSAGE_4015.ToString());
        }

        /// <summary>
        /// Applies the requested changes to the schedule entity and,
        /// when the work date changes, shifts every slot's start/end
        /// time onto the new date while preserving the original hours.
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
        /// Recomputes each slot's start/end time onto the new work date,
        /// preserving the original hour-of-day for each slot.
        /// </summary>
        private static void ShiftSlotsToNewDate(
            DoctorSchedule schedule,
            DateOnly newWorkDate)
        {
            foreach (var slot in schedule.TimeSlots ?? [])
            {
                var duration = slot.EndTime - slot.StartTime;
                var newStart = newWorkDate.ToDateTime(TimeOnly.FromTimeSpan(
                    slot.StartTime.TimeOfDay));
                slot.StartTime = newStart;
                slot.EndTime = newStart.Add(duration);
            }
        }

        /// <summary>
        /// Persists the schedule and its time slot changes.
        /// </summary>
        private async Task PersistChangesAsync(DoctorSchedule schedule)
        {
            await _scheduleCommandRepo.UpdateAsync(schedule);
            await _scheduleCommandRepo.SaveChangesAsync();
            foreach (var slot in schedule.TimeSlots ?? [])
                await _slotCommandRepo.UpdateAsync(slot);
            await _slotCommandRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Builds the response DTO from the updated schedule and room.
        /// </summary>
        private static EditDoctorScheduleResponse BuildResponse(
            DoctorSchedule schedule,
            FacilityRoom room)
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
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<EditDoctorScheduleResponse> CreateSuccessResponse(
            EditDoctorScheduleResponse response)
        {
            return ApiResponse<EditDoctorScheduleResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}