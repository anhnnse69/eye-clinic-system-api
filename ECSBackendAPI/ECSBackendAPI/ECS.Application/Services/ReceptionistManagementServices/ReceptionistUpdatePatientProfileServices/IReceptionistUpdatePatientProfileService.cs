using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices
{
    /// <summary>
    /// Interface for the service updating patient administrative metadata profiles.
    /// </summary>
    public interface IReceptionistUpdatePatientProfileService
    {
        /// <summary>
        /// Processes core mutative adjustments against administrative patient boundaries.
        /// </summary>
        /// <param name="patientId">The unique structural identifier tracking the target patient database row context.</param>
        /// <param name="request">The parameter package detailing updating demographic layouts parameters mapping.</param>
        /// <returns>An <see cref="ApiResponse{ReceptionistUpdatePatientProfileResponse}"/> wrapping tracking mutation feedback markers.</returns>
        Task<ApiResponse<ReceptionistUpdatePatientProfileResponse>> Process(Guid patientId, ReceptionistUpdatePatientProfileRequest request);
    }
}
