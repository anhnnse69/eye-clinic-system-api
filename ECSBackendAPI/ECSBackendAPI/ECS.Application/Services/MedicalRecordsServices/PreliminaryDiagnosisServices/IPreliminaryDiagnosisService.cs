using ECS.Application.Common.Response;

namespace ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices
{
    /// <summary>
    /// Interface for PreliminaryDiagnosisService.
    /// </summary>
    public interface IPreliminaryDiagnosisService
    {
        /// <summary>
        /// Processes the preliminary diagnosis request and creates a medical record.
        /// </summary>
        /// <param name="request">The preliminary diagnosis request.</param>
        /// <returns>An ApiResponse containing the created medical record details.</returns>
        Task<ApiResponse<PreliminaryDiagnosisResponse>> Process(PreliminaryDiagnosisRequest request);
    }
}
