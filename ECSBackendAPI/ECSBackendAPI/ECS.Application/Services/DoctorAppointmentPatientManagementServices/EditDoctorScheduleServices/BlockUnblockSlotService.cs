using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.EditDoctorScheduleServices
{
    /// <summary>
    /// Toggles a single time slot between AVAILABLE and BLOCKED.
    /// Blocked entirely if the slot is currently booked.
    /// </summary>
    public class BlockUnblockSlotService : IBlockUnblockSlotService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
            _doctorRepo;
        private readonly IRepositoryQueryBase<TimeSlot, Guid, AppDbContext>
            _slotQueryRepo;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>
            _slotCommandRepo;

        /// <summary>
        /// Initializes the service.
        /// </summary>
        public BlockUnblockSlotService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<TimeSlot, Guid, AppDbContext> slotQueryRepo,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotCommandRepo)
        {
            _doctorRepo = doctorRepo;
            _slotQueryRepo = slotQueryRepo;
            _slotCommandRepo = slotCommandRepo;
        }

        /// <summary>
        /// Blocks or unblocks the specified time slot.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <param name="slotId">Identifier of the time slot to toggle.</param>
        /// <param name="request">Whether to block or unblock the slot.</param>
        /// <returns>A successful response containing the new slot status.</returns>
        public async Task<ApiResponse<string>> Process(
            Guid userId,
            Guid slotId,
            BlockUnblockSlotRequest request)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);
            var slot = await ResolveOwnedSlotAsync(doctorProfile.Id, slotId);
            EnsureNotBooked(slot);
            ApplyStatus(slot, request.Block);
            await PersistChangesAsync(slot);
            return CreateSuccessResponse(slot.Status.ToString());
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
        /// Resolves the slot, ensuring it belongs to a schedule owned
        /// by the given doctor. Throws when not found.
        /// </summary>
        private async Task<TimeSlot> ResolveOwnedSlotAsync(
            Guid doctorId,
            Guid slotId)
        {
            var slot = await _slotQueryRepo
                .FindByCondition(s =>
                    s.Id == slotId &&
                    s.Schedule.DoctorId == doctorId)
                .FirstOrDefaultAsync();
            if (slot is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4004.ToString());
            return slot;
        }

        /// <summary>
        /// Throws if the slot is currently booked.
        /// </summary>
        private static void EnsureNotBooked(TimeSlot slot)
        {
            if (slot.Status == SlotStatus.BOOKED)
                throw new InvalidOperationException(
                    GeneralCode.APP_MESSAGE_4009.ToString());
        }

        /// <summary>
        /// Sets the slot's status based on the requested block flag.
        /// </summary>
        private static void ApplyStatus(TimeSlot slot, bool block)
        {
            slot.Status = block ? SlotStatus.BLOCKED : SlotStatus.AVAILABLE;
        }

        /// <summary>
        /// Persists the slot status change.
        /// </summary>
        private async Task PersistChangesAsync(TimeSlot slot)
        {
            await _slotCommandRepo.UpdateAsync(slot);
            await _slotCommandRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<string> CreateSuccessResponse(string status)
        {
            return ApiResponse<string>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                status);
        }
    }
}