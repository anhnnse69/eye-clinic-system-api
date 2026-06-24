using ECS.Application.Services.PatientAppointmentManagementServices.SubmitFeedbackServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// API Controller exposing backend secure access gates for authorized patients to submit feedback for completed appointments.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/appointments")]
    [Authorize(Roles = "PATIENT")]
    public class SubmitFeedbackController : ControllerBase
    {
        private readonly ISubmitFeedbackService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitFeedbackController"/> class with injected processing services pipelines.
        /// </summary>
        /// <param name="service">The structural business application workflow framework handling feedback submission data streams.</param>
        public SubmitFeedbackController(ISubmitFeedbackService service)
        {
            _service = service;
        }

        /// <summary>
        /// Submits feedback and rating for a completed appointment.
        /// </summary>
        /// <param name="appointmentId">The unique identifier of the appointment to rate.</param>
        /// <param name="request">The feedback submission request containing ratings and comment.</param>
        /// <returns>An HTTP 200 OK action outcome holding decoupled presentation payloads layers.</returns>
        [HttpPost("{appointmentId}/feedback")]
        public async Task<IActionResult> SubmitFeedback(
            [FromRoute] Guid appointmentId,
            [FromBody] SubmitFeedbackRequest request)
        {
            // Set the appointment ID from route
            request.AppointmentId = appointmentId;

            // Execute application workflows through asynchronous pipeline layers
            var result = await _service.Process(request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}