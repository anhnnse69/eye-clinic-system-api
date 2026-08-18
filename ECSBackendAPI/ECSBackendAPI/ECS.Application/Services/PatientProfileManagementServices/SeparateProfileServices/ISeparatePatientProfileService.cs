using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.SeparateProfileServices
{
    /// <summary>
    /// Interface for separate patient profile service.
    /// </summary>
    public interface ISeparatePatientProfileService
    {
        /// <summary>
        /// Processes the separation of a dependent child profile into an independent account.
        /// </summary>
        /// <param name="request">The separation request containing child profile ID and new credentials.</param>
        /// <returns>An ApiResponse containing the newly created user and patient profile IDs.</returns>
        Task<ApiResponse<SeparateProfileResponse>> Process(SeparateProfileRequest request);
    }
}
