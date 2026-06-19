using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices
{
    /// <summary>
    /// Interface for the service driving structural setup and persistence for patient profiles.
    /// </summary>
    public interface IReceptionistCreatePatientProfileService
    {
        /// <summary>
        /// Orchestrates foundational workflows mapping patient profiles alongside automated account provisioning.
        /// </summary>
        /// <param name="request">The parameter payload packet describing patient metadata attributes.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> wrapping tracking creation telemetry blocks.</returns>
        Task<ApiResponse<ReceptionistCreatePatientProfileResponse>> Process(ReceptionistCreatePatientProfileRequest request);
    }
}
