using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCheckInServices
{
    /// <summary>
    /// Interface for the service orchestrating logical check-in pipelines, live room-bound index progression sweeps, and atomicity tracking controls.
    /// </summary>
    public interface IReceptionistCheckInService
    {
        /// <summary>
        /// Processes, secures, and maps transactional arrival states matching required structural UI presentation layouts.
        /// </summary>
        /// <param name="request">The search filter packages and context arguments mapped from routing entry layers.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> wrapping finalized data rows and queue tracking structures.</returns>
        Task<ApiResponse<ReceptionistCheckInResponse>> Process(ReceptionistCheckInRequest request);
    }
}
