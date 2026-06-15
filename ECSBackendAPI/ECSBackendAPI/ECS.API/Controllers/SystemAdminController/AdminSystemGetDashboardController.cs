using ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles system administrator dashboard and analytics endpoints.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/dashboard")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemGetDashboardController : ControllerBase
    {
        private readonly IAdminSystemGetDashboardService _dashboardService;

        /// <summary>
        /// Initializes a new instance of <see cref="AdminSystemGetDashboardController"/> with required dependencies.
        /// </summary>
        /// <param name="dashboardService">The service handling system dashboard data aggregation.</param>
        public AdminSystemGetDashboardController(IAdminSystemGetDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Retrieves aggregated system-wide dashboard metrics filtered by clinic or date range.
        /// </summary>
        /// <param name="clinicId">Optional clinic identifier to filter metrics.</param>
        /// <param name="startDate">Optional start date for time-bounded metrics.</param>
        /// <param name="endDate">Optional end date for time-bounded metrics.</param>
        /// <returns>
        /// <c>200 OK</c> with the aggregated dashboard statistics;
        /// <c>401 Unauthorized</c> if the user is not authenticated;
        /// <c>403 Forbidden</c> if the user is not a system administrator.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSystemDashboard(
            [FromQuery] Guid? clinicId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var request = new AdminSystemGetDashboardRequest
            {
                ClinicId = clinicId,
                StartDate = startDate,
                EndDate = endDate
            };
            var result = await _dashboardService.Process(request);
            return Ok(result);
        }
    }
}