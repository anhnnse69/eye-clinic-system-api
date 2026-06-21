using ECS.Application.Services.PatientAppointmentManagementServices.GetPatientProfilesForBookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// Exposes distributed application network endpoint routing boundaries to manage patient profile selections for booking operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/profiles")]
    [Authorize(Roles = "PATIENT")]
    public class PatientProfileBookingController : ControllerBase
    {
        private readonly IGetPatientProfilesForBookingService _service;

        /// <summary>
        /// Initializes a new operational controller boundary instance with injected orchestration handler dependencies.
        /// </summary>
        /// <param name="service">The abstract application boundary contract execution instance handling profile options logic.</param>
        public PatientProfileBookingController(IGetPatientProfilesForBookingService service)
        {
            _service = service;
        }

        /// <summary>
        /// Resolves authorized patient profiles collection data payloads tailored specifically to render input selection layouts.
        /// </summary>
        /// <returns>An asynchronous task executing network results containing HTTP 200 state payload structural definitions.</returns>
        [HttpGet("booking-options")]
        public async Task<IActionResult> GetBookingOptions()
        {
            var result = await _service.Process();
            return Ok(result);
        }
    }
}