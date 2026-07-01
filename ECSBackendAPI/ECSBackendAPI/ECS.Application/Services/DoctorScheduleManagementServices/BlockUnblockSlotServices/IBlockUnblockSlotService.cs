using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices
{
    /// <summary>
    /// Defines operations for blocking or unblocking doctor time slots.
    /// </summary>
    public interface IBlockUnblockSlotService
    {
        /// <summary>
        /// Updates the block status of a time slot.
        /// </summary>
        /// <param name="userId">Doctor user identifier.</param>
        /// <param name="slotId">Time slot identifier.</param>
        /// <param name="request">Block status information.</param>
        /// <returns>The operation result.</returns>
        Task<ApiResponse<string>> Process(
            Guid receptionistUserId,
            Guid doctorId,
            Guid slotId,
            BlockUnblockSlotRequest request);
    }
}