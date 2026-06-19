using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices
{
    /// <summary>
    /// Interface for the service performing dynamic lookups on foundational user account records.
    /// </summary>
    public interface IReceptionistSearchAccountService
    {
        /// <summary>
        /// Drives the structural search and filtering pipeline for available user records.
        /// </summary>
        /// <param name="request">The parameter package detailing targeted identification filtering parameters.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> wrapping a list of matched <see cref="ReceptionistSearchAccountResponse"/> trackers.</returns>
        Task<ApiResponse<List<ReceptionistSearchAccountResponse>>> Process(ReceptionistSearchAccountRequest request);
    }
}