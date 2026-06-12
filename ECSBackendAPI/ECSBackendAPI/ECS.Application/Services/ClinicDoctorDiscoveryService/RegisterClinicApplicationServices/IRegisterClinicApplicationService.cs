using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.RegisterClinicApplicationServices
{
    /// <summary>
    /// Defines the contract for clinic application registration service.
    /// </summary>
    public interface IRegisterClinicApplicationService
    {
        /// <summary>
        /// Processes a clinic application registration request.
        /// </summary>
        Task<ApiResponse<bool>> Process(RegisterClinicApplicationRequest request);
    }
}
