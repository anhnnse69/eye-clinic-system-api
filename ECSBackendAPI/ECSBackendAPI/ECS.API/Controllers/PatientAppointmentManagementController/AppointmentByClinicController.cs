using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// API Controller exposing backend secure access gates for authorized patients to create appointments based on clinic working hours.
    /// </summary>
    [ApiController]
    [Route("api/v1/appointments")]
    [Authorize(Roles = "PATIENT")]
    public class AppointmentByClinicController : ControllerBase
    {
        private readonly ICreateAppointmentByClinicService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppointmentByClinicController"/> class with injected processing services pipelines.
        /// </summary>
        /// <param name="service">The structural business application workflow framework handling appointment creation by clinic data streams.</param>
        public AppointmentByClinicController(ICreateAppointmentByClinicService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates a new appointment based on clinic working hours without specific doctor selection.
        /// </summary>
        /// <param name="request">The appointment creation request parameters.</param>
        /// <returns>An HTTP 200 OK action outcome holding decoupled presentation payloads layers.</returns>
        [HttpPost("by-clinic")]
        public async Task<IActionResult> CreateAppointmentByClinic([FromBody] CreateAppointmentByClinicRequest request)
        {
            var result = await _service.Process(request);
            return Ok(result);
        }
    }
}
