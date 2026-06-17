using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientProfileManagementServices.ViewPatientProfileDetailServices
{
    /// <summary>
    /// Service contract defining operations for retrieving a detailed patient profile accessible by the authenticated patient.
    /// </summary>
    public interface IViewPatientProfileDetailService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve complete profile details for a specific patient record.
        /// </summary>
        /// <param name="patientProfileId">The unique patient profile identifier parameter.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<ViewPatientProfileDetailResponse>> Process(Guid patientProfileId);
    }
}
