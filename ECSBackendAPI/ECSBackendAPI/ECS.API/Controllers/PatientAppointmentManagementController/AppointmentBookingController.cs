using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// Exposes distributed application network endpoint routing boundaries to execute and manage appointment creation transactions.
    /// </summary>
    [ApiController]
    [Route("api/v1/appointments")]
    [Authorize(Roles = "PATIENT")]
    public class AppointmentBookingController : ControllerBase
    {
        private readonly ICreateAppointmentService _service;

        /// <summary>
        /// Initializes a new operational controller boundary instance with injected orchestration handler dependencies.
        /// </summary>
        /// <param name="service">The abstract application boundary contract execution instance handling transactional appointment creation.</param>
        public AppointmentBookingController(ICreateAppointmentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Dispatches inbound payload configurations onto core processing layers to create a persistent patient appointment record securely.
        /// </summary>
        /// <param name="request">The data container tracking request parameters and structural entity keys from the presentation boundary.</param>
        /// <returns>An asynchronous task executing network results containing HTTP 200 transaction outcome states payload definitions.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            var result = await _service.Process(request);
            return Ok(result);
        }
    }
}