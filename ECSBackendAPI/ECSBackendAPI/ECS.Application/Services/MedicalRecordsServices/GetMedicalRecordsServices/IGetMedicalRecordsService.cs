using ECS.Application.Common.Response;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices
{
    /// <summary>
    /// Get medical records service interface
    /// </summary>
    public interface IGetMedicalRecordsService
    {
        /// <summary>
        /// Gets paginated list of medical records for the authenticated doctor.
        /// Records are filtered by the doctor's patients and include edit/view permission flags.
        /// </summary>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>API response with list of medical records and pagination metadata</returns>
        Task<ApiResponse<List<GetMedicalRecordsResponse>>> Process(
            GetMedicalRecordsRequest request);
    }
}
