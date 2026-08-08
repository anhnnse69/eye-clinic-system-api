using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ExportClinicReportServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles report export endpoints for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/export-report")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicExportReportController : ControllerBase
    {
        private readonly IExportClinicReportService _exportReportService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicExportReportController"/>.
        /// </summary>
        /// <param name="exportReportService">Service for generating clinic report datasets.</param>
        public ClinicExportReportController(IExportClinicReportService exportReportService)
        {
            _exportReportService = exportReportService;
        }

        /// <summary>
        /// Compiles and retrieves full clinic operational data (profile, staff, medical records, services, rooms) for export.
        /// </summary>
        /// <returns>200 OK with full clinic report payload on success; 400 Bad Request otherwise.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ExportClinicReportResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ExportClinicReportResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportReport()
        {
            var result = await _exportReportService.Process();
            if (result.Data == null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
