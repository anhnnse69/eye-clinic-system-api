using ECS.Application.Common.Response;

namespace ECS.Application.Services.SearchClinicDoctorServices
{
    /// <summary>
    /// Defines the contract for the search service.
    /// </summary>
    public interface ISearchClinicDoctorService
    {
        /// <summary>
        /// Searches clinics and doctors by keyword.
        /// </summary>
        Task<ApiResponse<SearchClinicDoctorResponse>> Process(
            SearchClinicDoctorRequest request);
    }
}