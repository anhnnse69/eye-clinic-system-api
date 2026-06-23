
using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CancelAppointmentServices
{
    /// <summary>
    /// Service contract defining operations for cancelling appointments by patients.
    /// </summary>
    public interface ICancelAppointmentService
    {
        /// <summary>
        /// Executes the application workflow process to cancel an existing appointment.
        /// </summary>
        /// <param name="request">The cancel appointment request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<CancelAppointmentResponse>> Process(CancelAppointmentRequest request);
    }
}
