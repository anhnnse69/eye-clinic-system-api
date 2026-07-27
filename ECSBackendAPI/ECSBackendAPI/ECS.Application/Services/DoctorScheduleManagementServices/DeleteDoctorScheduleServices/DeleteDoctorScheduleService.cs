using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorScheduleManagementServices.DeleteDoctorScheduleServices
{
    /// <summary>
    /// Service responsible for soft-deleting an entire doctor schedule. 
    /// Enforces cross-clinic boundary validation and completely blocks the deletion 
    /// if any individual time slot within the target schedule has already been booked by a patient.
    /// </summary>
    public class DeleteDoctorScheduleService : IDeleteDoctorScheduleService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepo;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteDoctorScheduleService"/> class.
        /// </summary>
        /// <param name="staffClinicRepo">Repository interface to verify receptionist identities and clinic mapping boundaries.</param>
        /// <param name="doctorRepo">Repository interface to query and validate active doctor profiles.</param>
        /// <param name="dbContext">The primary Entity Framework database context used to track and save changes.</param>
        public DeleteDoctorScheduleService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            AppDbContext dbContext)
        {
            _staffClinicRepo = staffClinicRepo;
            _doctorRepo = doctorRepo;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Processes the request to soft-delete a doctor's schedule after performing necessary cross-clinic and booking checks.
        /// </summary>
        /// <param name="receptionistUserId">The unique identifier of the performing receptionist.</param>
        /// <param name="doctorId">The unique identifier of the target doctor profile.</param>
        /// <param name="scheduleId">The unique identifier of the specific schedule to delete.</param>
        /// <returns>An API standard template response wrapping the structured schedule deletion response payload.</returns>
        public async Task<ApiResponse<DeleteDoctorScheduleResponse>> Process(
            Guid receptionistUserId,
            Guid doctorId,
            Guid scheduleId)
        {
            var clinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var doctorProfile = await ResolveActiveDoctorProfileAsync(doctorId);
            EnsureSameClinic(clinicId, doctorProfile.ClinicId);
            var schedule = await ResolveOwnedScheduleAsync(doctorProfile.Id, scheduleId);
            EnsureNoBookedSlots(schedule);
            await ApplySoftDeleteAndSaveAsync(schedule);
            return CreateSuccessResponse(BuildResponse(schedule));
        }

        /// <summary>
        /// Resolves the clinic identifier associated with the active receptionist user.
        /// </summary>
        /// <param name="receptionistUserId">The unique identifier of the receptionist user.</param>
        /// <returns>The clinic identifier mapped to the specified receptionist.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the receptionist is not found or is inactive.</exception>
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
        /// Resolves the active doctor profile matching the specified identifier.
        /// </summary>
        /// <param name="doctorId">The unique identifier of the doctor profile.</param>
        /// <returns>The active doctor profile entity.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no active doctor profile matches the given identifier.</exception>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(Guid doctorId)
        {
            var doctorProfile = await _doctorRepo
                .FindByCondition(d => d.Id == doctorId && d.IsActive)
                .FirstOrDefaultAsync();
            if (doctorProfile is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());
            return doctorProfile;
        }

        /// <summary>
        /// Validates that both the performing receptionist and target doctor belong to the exact same clinic.
        /// </summary>
        /// <param name="receptionistClinicId">The clinic identifier of the receptionist.</param>
        /// <param name="doctorClinicId">The clinic identifier of the doctor.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when clinic cross-boundaries are violated.</exception>
        private static void EnsureSameClinic(Guid receptionistClinicId, Guid doctorClinicId)
        {
            if (receptionistClinicId != doctorClinicId)
                throw new UnauthorizedAccessException(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// Resolves and fetches the targeted schedule ensuring it belongs strictly to the requested doctor and is not already deleted.
        /// </summary>
        /// <param name="doctorId">The unique identifier of the specific doctor profile.</param>
        /// <param name="scheduleId">The unique identifier of the target schedule.</param>
        /// <returns>The tracked doctor schedule entity with all its related time slots pre-loaded.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the schedule cannot be found for the specified doctor.</exception>
        private async Task<DoctorSchedule> ResolveOwnedScheduleAsync(Guid doctorId, Guid scheduleId)
        {
            var schedule = await _dbContext.Set<DoctorSchedule>()
                .Include(s => s.TimeSlots)
                .FirstOrDefaultAsync(s =>
                    s.Id == scheduleId &&
                    s.DoctorId == doctorId &&
                    !s.IsDeleted);
            if (schedule is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4004.ToString());
            return schedule;
        }

        /// <summary>
        /// Validates that none of the time slots tied to the target schedule have been booked by a patient.
        /// </summary>
        /// <param name="schedule">The doctor schedule entity holding the time slots to check.</param>
        /// <exception cref="InvalidOperationException">Thrown when trying to delete a schedule containing at least one booked slot.</exception>
        private static void EnsureNoBookedSlots(DoctorSchedule schedule)
        {
            var slots = schedule.TimeSlots;
            if (slots != null)
            {
                var hasBookedSlot = slots.Any(slot => slot.Status == SlotStatus.BOOKED);
                if (hasBookedSlot)
                    throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4009.ToString());
            }
        }

        /// <summary>
        /// Applies the soft-delete tracking flags and updates audit timestamps on the target schedule.
        /// </summary>
        /// <param name="schedule">The target doctor schedule entity to be soft-deleted.</param>
        private async Task ApplySoftDeleteAndSaveAsync(DoctorSchedule schedule)
        {
            schedule.IsDeleted = true;
            schedule.DeletedAt = DateTime.UtcNow;
            schedule.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Maps the soft-deleted schedule entity state into a specific structured response payload.
        /// </summary>
        /// <param name="schedule">The modified doctor schedule entity reference.</param>
        /// <returns>A new populated instance of <see cref="DeleteDoctorScheduleResponse"/>.</returns>
        private static DeleteDoctorScheduleResponse BuildResponse(DoctorSchedule schedule)
        {
            return new DeleteDoctorScheduleResponse
            {
                ScheduleId = schedule.Id,
                DeletedAt = schedule.DeletedAt!.Value
            };
        }

        /// <summary>
        /// Wraps the deletion response data into a standardized API success template response wrapper.
        /// </summary>
        /// <param name="response">The structured response payload containing schedule deletion metadata.</param>
        /// <returns>The API standard outcome object encapsulating the deletion details.</returns>
        private static ApiResponse<DeleteDoctorScheduleResponse> CreateSuccessResponse(
            DeleteDoctorScheduleResponse response)
        {
            return ApiResponse<DeleteDoctorScheduleResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}