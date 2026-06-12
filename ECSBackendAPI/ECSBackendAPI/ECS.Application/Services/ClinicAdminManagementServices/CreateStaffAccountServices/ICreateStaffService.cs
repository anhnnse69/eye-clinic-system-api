using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices
{
    /// <summary>
    /// Defines business transaction workflows for administrative staff record onboarding inside scoped clinic context.
    /// </summary>
    public interface ICreateStaffService
    {
        /// <summary>
        /// Executes orchestration process logic to register a new user identity and associate specific clinic mapping boundaries.
        /// </summary>
        /// <param name="request">The structural data parameter carrier specifying target account settings attributes.</param>
        /// <returns>A encapsulated standard workflow execution response tracking model wrapper.</returns>
        Task<ApiResponse<CreateStaffResponse>> Process(CreateStaffRequest request);
    }
}
