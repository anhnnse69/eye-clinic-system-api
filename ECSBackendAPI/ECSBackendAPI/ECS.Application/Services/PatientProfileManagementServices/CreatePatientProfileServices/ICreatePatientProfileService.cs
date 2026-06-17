using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.CreatePatientProfileServices
{
    /// <summary>
    /// Defines business transaction workflows for patient profile creation inside scoped user session context.
    /// </summary>
    public interface ICreatePatientProfileService
    {
        /// <summary>
        /// Executes orchestration process logic to register a new patient profile and associate relation mapping boundaries.
        /// </summary>
        /// <param name="request">The structural data parameter carrier specifying target profile settings attributes.</param>
        /// <returns>An encapsulated standard workflow execution response tracking model wrapper.</returns>
        Task<ApiResponse<CreatePatientProfileResponse>> Process(CreatePatientProfileRequest request);
    }
}
