using ECS.Application.Services.DoctorScheduleManagementServices.DoctorDashboardServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// Controller for doctor dashboard operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class DoctorDashboardController : ControllerBase
    {
        private readonly IDoctorDashboardService _service;

        /// <summary>
        /// Initializes a new instance of the controller.
        /// </summary>
        /// <param name="service">Doctor dashboard service.</param>
        public DoctorDashboardController(IDoctorDashboardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves dashboard statistics for the specified doctor.
        /// </summary>
        /// <param name="id">Doctor identifier.</param>
        /// <param name="startDate">Start date of the reporting period.</param>
        /// <param name="endDate">End date of the reporting period.</param>
        /// <returns>Dashboard statistics.</returns>
        [HttpGet("{id:guid}/dashboard")]
        public async Task<IActionResult> GetDashboard(
            Guid id,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null)
        {
            var request = new DoctorDashboardRequest
            {
                StartDate = startDate,
                EndDate = endDate,
            };
            var result = await _service.Process(id, request);
            return Ok(result);
        }
    }
}
