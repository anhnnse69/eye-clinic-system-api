using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices
{
    /// <summary>
    /// Clinic details query service interface.
    /// </summary>
    public interface IGetClinicDetailsService
    {
        /// <summary>
        /// Processes the data retrieval flow for a clinic.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <returns>An <see cref="ApiResponse{GetClinicDetailsResponse}"/> containing the data entity details.</returns>
        Task<ApiResponse<GetClinicDetailsResponse>> Process(Guid id);
    }
}
