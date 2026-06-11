using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices;
using ECS.Application.Services.ClinicDashboardServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles dashboard management endpoints for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/dashboard")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class DashboardController : ControllerBase
    {
        private readonly IViewClinicDashboardService _dashboardService;

        /// <summary>
        /// Initializes a new instance of <see cref="DashboardController"/>.
        /// </summary>
        /// <param name="dashboardService">
        /// The service responsible for retrieving clinic dashboard data.
        /// </param>
        public DashboardController(
            IViewClinicDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Retrieves dashboard statistics for the clinic associated with the authenticated administrator.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with dashboard metrics if found;
        /// <c>400 Bad Request</c> if user validation fails or clinic data cannot be resolved.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(
            typeof(ApiResponse<ViewClinicDashboardResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ApiResponse<ViewClinicDashboardResponse>),
            StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDashboard()
        {
            // Execute dashboard retrieval workflow using current user context
            var result =
                await _dashboardService.Process();

            if (result.Data == null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}