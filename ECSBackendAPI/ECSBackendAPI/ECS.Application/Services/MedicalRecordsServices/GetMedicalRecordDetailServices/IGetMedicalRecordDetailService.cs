using ECS.Application.Common.Response;
using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Get medical record detail service interface
    /// </summary>
    public interface IGetMedicalRecordDetailService
    {
        /// <summary>
        /// Retrieves complete details of a specific medical record for the authenticated doctor.
        /// Includes all related entities: diagnoses, prescriptions, clinical notes, eye examinations, etc.
        /// </summary>
        /// <param name="request">The medical record ID to retrieve</param>
        /// <returns>API response with complete medical record detail or error code</returns>
        Task<ApiResponse<GetMedicalRecordDetailResponse>> Process(GetMedicalRecordDetailRequest request);
    }
}
