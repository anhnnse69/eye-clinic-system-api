using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.GetActiveSpecialtiesServices
{
    /// <summary>
    /// Get active medical specialties service interface.
    /// </summary>
    public interface IGetActiveSpecialtiesService
    {
        /// <summary>
        /// Processes the active specialties retrieval logic.
        /// </summary>
        /// <returns>An <see cref="ApiResponse{List{GetActiveSpecialtiesResponse}}"/> containing the execution result.</returns>
        Task<ApiResponse<List<GetActiveSpecialtiesResponse>>> Process();
    }
}
