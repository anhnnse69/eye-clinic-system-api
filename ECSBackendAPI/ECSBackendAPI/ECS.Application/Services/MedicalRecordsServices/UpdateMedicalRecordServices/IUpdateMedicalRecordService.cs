using ECS.Application.Common.Response;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Interface for UpdateMedicalRecordService.
    /// </summary>
    public interface IUpdateMedicalRecordService
    {
        /// <summary>
        /// Processes the medical record update request.
        /// </summary>
        /// <param name="recordId">The medical record ID to update.</param>
        /// <param name="request">The update medical record request.</param>
        /// <returns>An ApiResponse containing the updated medical record details.</returns>
        Task<ApiResponse<UpdateMedicalRecordResponse>> Process(Guid recordId, UpdateMedicalRecordRequest request);
    }
}
