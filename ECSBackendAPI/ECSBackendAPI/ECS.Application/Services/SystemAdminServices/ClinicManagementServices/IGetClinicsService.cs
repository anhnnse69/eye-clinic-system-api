using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.ClinicManagementServices
{
    /// <summary>
    /// Service interface for retrieving clinics dataset.
    /// </summary>
    public interface IGetClinicsService
    {
        /// <summary>
        /// Processes the clinic retrieval request.
        /// </summary>
        /// <param name="request">The pagination and filtering request criteria.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of formatted clinic details and metadata.</returns>
        Task<ApiResponse<List<GetClinicResponse>>> Process(GetClinicsRequest request);
    }
}
