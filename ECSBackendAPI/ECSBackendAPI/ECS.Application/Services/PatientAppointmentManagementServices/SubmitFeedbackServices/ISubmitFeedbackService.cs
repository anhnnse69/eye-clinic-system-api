using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.SubmitFeedbackServices
{
    /// <summary>
    /// Service contract defining operations for submitting feedback and rating for completed appointments.
    /// </summary>
    public interface ISubmitFeedbackService
    {
        /// <summary>
        /// Executes the application workflow process to submit feedback for a completed appointment.
        /// </summary>
        /// <param name="request">The feedback submission request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<SubmitFeedbackResponse>> Process(SubmitFeedbackRequest request);
    }
}
