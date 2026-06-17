using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices
{
    /// <summary>
    /// Interface for the service handling patient records lookup, filtration, and pagination within the clinic scope.
    /// </summary>
    public interface IReceptionistGetPatientsListService
    {
        /// <summary>
        /// Orchestrates and executes the complete process of receiving, validating, and paginating patient profiles.
        /// </summary>
        /// <param name="request">The search filter packages and receptionist context from the API Layer.</param>
        /// <returns>An <see cref="ApiResponse{List{ReceptionistGetPatientsListResponse}}"/> wrapping the paginated schedules and metadata.</returns>
        Task<ApiResponse<List<ReceptionistGetPatientsListResponse>>> Process(ReceptionistGetPatientsListRequest request);
    }
}