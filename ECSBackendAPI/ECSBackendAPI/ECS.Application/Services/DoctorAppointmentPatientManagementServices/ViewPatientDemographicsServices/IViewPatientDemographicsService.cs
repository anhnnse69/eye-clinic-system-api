using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// Service contract defining operations for viewing patient demographics list.
    /// </summary>
    public interface IViewPatientDemographicsService
    {
        /// <summary>
        /// Processes the request to retrieve a paginated list of patient demographics with associated medical records.
        /// </summary>
        /// <param name="request">The view patient demographics request criteria.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<ViewPatientDemographicsListResponse>> Process(ViewPatientDemographicsRequest request);
    }
}
