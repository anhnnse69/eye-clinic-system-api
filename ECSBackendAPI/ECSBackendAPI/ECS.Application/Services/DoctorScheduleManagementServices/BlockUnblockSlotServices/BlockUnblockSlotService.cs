using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices
{
    /// <summary>
    /// Service responsible for toggling a specific doctor schedule time slot between AVAILABLE and BLOCKED states.
    /// Enforces clinic cross-boundaries and locks adjustments if a slot is already booked by a patient.
    /// </summary>
    public class BlockUnblockSlotService : IBlockUnblockSlotService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepo;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockUnblockSlotService"/> class.
        /// </summary>
        /// <param name="staffClinicRepo">Repository interface to verify receptionist identities and clinic mapping boundaries.</param>
        /// <param name="doctorRepo">Repository interface to query and validate active doctor profiles.</param>
        /// <param name="dbContext">The primary Entity Framework database context used to track and save changes.</param>
        public BlockUnblockSlotService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            AppDbContext dbContext)
        {
            _staffClinicRepo = staffClinicRepo;
            _doctorRepo = doctorRepo;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Processes the request to block or unblock an individual doctor's schedule time slot.
        /// </summary>
        /// <param name="receptionistUserId">The unique identifier of the performing receptionist.</param>
        /// <param name="doctorId">The unique identifier of the target doctor profile.</param>
        /// <param name="slotId">The unique identifier of the specific time slot to modify.</param>
        /// <param name="request">The request body payload containing the targeted boolean block status flag.</param>
        /// <returns>An API standard template response wrapping the updated status string descriptor.</returns>
        public async Task<ApiResponse<string>> Process(
            Guid receptionistUserId,
            Guid doctorId,
            Guid slotId,
            BlockUnblockSlotRequest request)
        {
            var receptionistClinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var doctorProfile = await ResolveActiveDoctorProfileAsync(doctorId);
            EnsureSameClinic(receptionistClinicId, doctorProfile.ClinicId);
            var slot = await ResolveDoctorTimeSlotAsync(slotId, doctorProfile.Id);
            EnsureSlotNotBooked(slot);
            UpdateSlotStatus(slot, request.Block);
            await _dbContext.SaveChangesAsync();
            return CreateSuccessResponse(slot.Status);
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
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4011.ToString());
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
        /// Resolves and fetches the targeted time slot ensuring it belongs strictly to the requested doctor's schedule.
        /// </summary>
        /// <param name="slotId">The unique identifier of the target time slot.</param>
        /// <param name="doctorId">The unique identifier of the specific doctor profile.</param>
        /// <returns>The tracked time slot entity with its parent schedule context pre-loaded.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the time slot cannot be found for the specified doctor.</exception>
        private async Task<TimeSlot> ResolveDoctorTimeSlotAsync(Guid slotId, Guid doctorId)
        {
            var slot = await _dbContext.Set<TimeSlot>()
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(s =>
                    s.Id == slotId &&
                    s.Schedule.DoctorId == doctorId);
            if (slot is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4004.ToString());
            return slot;
        }

        /// <summary>
        /// Validates that the requested time slot has not already been booked by a patient.
        /// </summary>
        /// <param name="slot">The target time slot entity to check.</param>
        /// <exception cref="InvalidOperationException">Thrown when trying to modify a slot that is already booked.</exception>
        private static void EnsureSlotNotBooked(TimeSlot slot)
        {
            if (slot.Status == SlotStatus.BOOKED)
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4009.ToString());
        }

        /// <summary>
        /// Updates the status state of the slot based on the block or unblock instruction flag.
        /// </summary>
        /// <param name="slot">The reference target time slot entity.</param>
        /// <param name="blockRequested">Determines whether to toggle the state to BLOCKED (true) or AVAILABLE (false).</param>
        private static void UpdateSlotStatus(TimeSlot slot, bool blockRequested)
        {
            slot.Status = blockRequested ? SlotStatus.BLOCKED : SlotStatus.AVAILABLE;
        }

        /// <summary>
        /// Wraps the newly updated slot status into a standardized API success template response wrapper.
        /// </summary>
        /// <param name="status">The updated slot status value descriptor.</param>
        /// <returns>The API standard outcome object encapsulating the updated state as a string descriptor.</returns>
        private static ApiResponse<string> CreateSuccessResponse(SlotStatus status)
        {
            return ApiResponse<string>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                status.ToString());
        }
    }
}
    
