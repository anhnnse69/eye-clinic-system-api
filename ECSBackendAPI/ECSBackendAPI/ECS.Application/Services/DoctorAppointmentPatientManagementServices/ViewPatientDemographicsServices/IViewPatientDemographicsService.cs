using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// Service contract defining operations for viewing patient demographics and associated medical records list.
    /// </summary>
    public interface IViewPatientDemographicsService
    {
        /// <summary>
        /// Processes the internal data pipeline workflow to retrieve patient demographics and their medical records.
        /// </summary>
        /// <param name="request">The view patient demographics request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<ViewPatientDemographicsResponse>> Process(ViewPatientDemographicsRequest request);
    }
}
