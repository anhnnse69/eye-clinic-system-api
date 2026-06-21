using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistPayDepositServices
{
    /// <summary>
    /// Interface for the service orchestrating logical pipeline streams, data validation gates, and execution of down-stream financial balance commits.
    /// </summary>
    public interface IReceptionistPayDepositService
    {
        /// <summary>
        /// Processes, secures, and maps transactional progression flows matching required structural system frameworks.
        /// </summary>
        /// <param name="request">The request parameters containing criteria tokens mapped from entry layers.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> wrapping the finalized core data structure confirmation row.</returns>
        Task<ApiResponse<ReceptionistPayDepositResponse>> Process(ReceptionistPayDepositRequest request);
    }
}
