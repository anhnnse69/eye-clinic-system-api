using ECS.Application.Services.PatientAppointmentManagementServices.CancelAppointmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// API Controller exposing backend secure access gates for authorized patients to cancel their appointments.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/appointments")]
    [Authorize(Roles = "PATIENT")]
    public class CancelAppointmentController : ControllerBase
    {
        private readonly ICancelAppointmentService _cancelAppointmentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelAppointmentController"/> class with injected processing services pipelines.
        /// </summary>
        /// <param name="cancelAppointmentService">The structural business application workflow framework handling appointment cancellation.</param>
        public CancelAppointmentController(ICancelAppointmentService cancelAppointmentService)
        {
            _cancelAppointmentService = cancelAppointmentService;
        }

        /// <summary>
        /// Cancels an existing appointment by the patient with validation and slot release.
        /// </summary>
        /// <param name="appointmentId">The unique identifier of the appointment to cancel.</param>
        /// <param name="request">The parameters capturing cancellation reason.</param>
        /// <returns>An HTTP 200 OK action outcome holding decoupled presentation payloads layers.</returns>
        [HttpPatch("{appointmentId}/cancel")]
        public async Task<IActionResult> CancelAppointment(
            [FromRoute] Guid appointmentId,
            [FromBody] CancelAppointmentRequest request)
        {
            // Set the appointment ID from route
            request.AppointmentId = appointmentId;

            // Execute application workflows through asynchronous pipeline layers
            var result = await _cancelAppointmentService.Process(request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}