using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.CheckSelfProfileServices
{
    /// <summary>
    /// Service contract defining operations for checking if user has a self profile.
    /// </summary>
    public interface ICheckSelfProfileService
    {
        /// <summary>
        /// Executes the application workflow process to check if the logged-in user has a self profile.
        /// </summary>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<CheckSelfProfileResponse>> Process();
    }
}