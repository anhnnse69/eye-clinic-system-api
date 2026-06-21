using ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentHistoryServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// API Controller exposing backend secure access gates for authorized profiles to evaluate appointment history catalogs.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/appointments/history")]
    [Authorize(Roles = "PATIENT")]
    public class GetAppointmentHistoryController : ControllerBase
    {
        private readonly IGetAppointmentHistoryService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAppointmentHistoryController"/> class with injected processing services pipelines.
        /// </summary>
        /// <param name="service">The structural business application workflow framework handling histories data streams.</param>
        public GetAppointmentHistoryController(IGetAppointmentHistoryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves a paginated layout array of structural appointment details matching targeted keyword queries boundaries.
        /// </summary>
        /// <param name="request">The parameters capturing query targets alongside navigation parameters metrics.</param>
        /// <returns>An HTTP 200 OK action outcome holding decoupled presentation payloads layers.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAppointmentHistory([FromQuery] GetAppointmentHistoryRequest request)
        {
            // Execute application workflows through asynchronous pipeline layers
            var result = await _service.Process(request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}
