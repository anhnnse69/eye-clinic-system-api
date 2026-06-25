using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentDetailServices
{
    /// <summary>
    /// Service contract defining operations for retrieving detailed appointment information for the logged-in patient.
    /// </summary>
    public interface IGetAppointmentDetailService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve comprehensive appointment details.
        /// </summary>
        /// <param name="request">The appointment detail request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<GetAppointmentDetailResponse>> Process(GetAppointmentDetailRequest request);
    }
}
