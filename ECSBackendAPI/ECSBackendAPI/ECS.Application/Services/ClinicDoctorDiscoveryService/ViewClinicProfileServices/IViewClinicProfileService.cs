using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices
{
    /// <summary>
    /// Defines the contract for viewing a clinic profile.
    /// </summary>
    public interface IViewClinicProfileService
    {
        /// <summary>
        /// Returns the full profile of a clinic by its ID.
        /// </summary>
        Task<ApiResponse<ViewClinicProfileResponse>> Process(Guid clinicId);
    }
}
