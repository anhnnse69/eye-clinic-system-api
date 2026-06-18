using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices
{
    /// <summary>
    /// Service contract defining operations for getting patient demographics details.
    /// </summary>
    public interface IGetDetailPatientDemographicsService
    {
        /// <summary>
        /// Processes the request to retrieve detailed demographics information for a specific patient.
        /// </summary>
        /// <param name="request">The request containing the patient identifier.</param>
        /// <returns>An <see cref="ApiResponse{GetDetailPatientDemographicsResponse}"/> containing patient demographics details.</returns>
        Task<ApiResponse<GetDetailPatientDemographicsResponse>> Process(GetDetailPatientDemographicsRequest request);
    }
}
