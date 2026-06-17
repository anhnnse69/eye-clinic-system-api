using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.UpdatePatientProfileServices
{
    /// <summary>
    /// Defines business transaction workflows for patient profile update inside scoped user session context.
    /// </summary>
    public interface IUpdatePatientProfileService
    {
        /// <summary>
        /// Executes orchestration process logic to update an existing patient profile and validate ownership boundaries.
        /// </summary>
        /// <param name="profileId">The unique target patient profile identifier sequence.</param>
        /// <param name="request">The structural data parameter carrier specifying target profile settings attributes.</param>
        /// <returns>An encapsulated standard workflow execution response tracking model wrapper.</returns>
        Task<ApiResponse<UpdatePatientProfileResponse>> Process(Guid profileId, UpdatePatientProfileRequest request);
    }
}
