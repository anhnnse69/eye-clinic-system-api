using ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentDetailServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// API Controller exposing backend secure access gates for authorized patients to retrieve detailed appointment information.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/appointments")]
    [Authorize(Roles = "PATIENT")]
    public class GetAppointmentDetailController : ControllerBase
    {
        private readonly IGetAppointmentDetailService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAppointmentDetailController"/> class with injected processing services pipelines.
        /// </summary>
        /// <param name="service">The structural business application workflow framework handling appointment detail data streams.</param>
        public GetAppointmentDetailController(IGetAppointmentDetailService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves comprehensive appointment details for a specific appointment ID.
        /// </summary>
        /// <param name="appointmentId">The unique identifier of the appointment to retrieve.</param>
        /// <returns>An HTTP 200 OK action outcome holding decoupled presentation payloads layers.</returns>
        [HttpGet("{appointmentId}")]
        public async Task<IActionResult> GetAppointmentDetail([FromRoute] Guid appointmentId)
        {
            var request = new GetAppointmentDetailRequest
            {
                AppointmentId = appointmentId
            };

            // Execute application workflows through asynchronous pipeline layers
            var result = await _service.Process(request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}
