using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetMyQueueListServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Controller for getting current doctor's queue list.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class GetMyQueueListController : ControllerBase
    {
        private readonly IGetMyQueueListService _service;

        public GetMyQueueListController(IGetMyQueueListService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the queue list for the current authenticated doctor.
        /// Automatically resolves doctor profile from JWT token.
        /// </summary>
        /// <param name="date">The date to get queue list (defaults to today).</param>
        /// <returns>Queue list with statistics.</returns>
        [HttpGet("me/queue")]
        public async Task<IActionResult> GetMyQueueList([FromQuery] DateOnly? date = null)
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.Now);
            var result = await _service.Process(targetDate);
            return Ok(result);
        }
    }
}
