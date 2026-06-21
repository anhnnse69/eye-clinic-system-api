using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.DeleteDoctorScheduleServices
{
    /// <summary>
    /// Soft-deletes a doctor schedule. Blocked entirely if any slot
    /// in the schedule is already booked.
    /// </summary>
    public class DeleteDoctorScheduleService : IDeleteDoctorScheduleService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
            _doctorRepo;
        private readonly IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>
            _scheduleQueryRepo;
        private readonly IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext>
            _scheduleCommandRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteDoctorScheduleService"/> class.
        /// </summary>
        /// <param name="doctorRepo">
        /// Repository used to retrieve doctor profile information.
        /// </param>
        /// <param name="scheduleQueryRepo">
        /// Repository used to query doctor schedules.
        /// </param>
        /// <param name="scheduleCommandRepo">
        /// Repository used to perform delete operations on doctor schedules.
        /// </param>
        public DeleteDoctorScheduleService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> scheduleQueryRepo,
            IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext> scheduleCommandRepo)
        {
            _doctorRepo = doctorRepo;
            _scheduleQueryRepo = scheduleQueryRepo;
            _scheduleCommandRepo = scheduleCommandRepo;
        }

        /// <summary>
        /// Soft-deletes a doctor's schedule by id.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <param name="scheduleId">Identifier of the schedule to delete.</param>
        /// <returns>A successful response containing the deletion timestamp.</returns>
        public async Task<ApiResponse<DeleteDoctorScheduleResponse>> Process(
            Guid userId,
            Guid scheduleId)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);
            var schedule = await ResolveOwnedScheduleAsync(doctorProfile.Id, scheduleId);

            EnsureNoBookedSlots(schedule);

            ApplySoftDelete(schedule);
            await PersistChangesAsync(schedule);

            var response = BuildResponse(schedule);
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
        /// Resolves the schedule, ensuring it belongs to the given doctor
        /// and is not already deleted, including its time slots.
        /// Throws when not found.
        /// </summary>
        private async Task<DoctorSchedule> ResolveOwnedScheduleAsync(
            Guid doctorId,
            Guid scheduleId)
        {
            var schedule = await _scheduleQueryRepo
                .FindByCondition(s =>
                    s.Id == scheduleId &&
                    s.DoctorId == doctorId &&
                    !s.IsDeleted)
                .Include(s => s.TimeSlots)
                .FirstOrDefaultAsync();
            if (schedule is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4004.ToString());
            return schedule;
        }

        /// <summary>
        /// Throws if any slot in the schedule is currently booked,
        /// since the schedule cannot be deleted once a patient has booked.
        /// </summary>
        private static void EnsureNoBookedSlots(DoctorSchedule schedule)
        {
            var hasBookedSlot = (schedule.TimeSlots ?? [])
                .Any(slot => slot.Status == SlotStatus.BOOKED);
            if (hasBookedSlot)
                throw new InvalidOperationException(
                    GeneralCode.APP_MESSAGE_4009.ToString());
        }

        /// <summary>
        /// Marks the schedule as soft-deleted.
        /// </summary>
        private static void ApplySoftDelete(DoctorSchedule schedule)
        {
            schedule.IsDeleted = true;
            schedule.DeletedAt = DateTime.UtcNow;
            schedule.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Persists the soft-delete change.
        /// </summary>
        private async Task PersistChangesAsync(DoctorSchedule schedule)
        {
            await _scheduleCommandRepo.UpdateAsync(schedule);
            await _scheduleCommandRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Builds the response DTO from the deleted schedule.
        /// </summary>
        private static DeleteDoctorScheduleResponse BuildResponse(
            DoctorSchedule schedule)
        {
            return new DeleteDoctorScheduleResponse
            {
                ScheduleId = schedule.Id,
                DeletedAt = schedule.DeletedAt!.Value,
            };
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<DeleteDoctorScheduleResponse> CreateSuccessResponse(
            DeleteDoctorScheduleResponse response)
        {
            return ApiResponse<DeleteDoctorScheduleResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}