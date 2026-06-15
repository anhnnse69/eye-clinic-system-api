using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices;


namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles clinic service portfolio management requests initiated by certified clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/clinic-services")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ViewListServiceController : ControllerBase
    {
        private readonly IViewClinicServicesService _viewClinicServicesService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicServiceController"/> with specialized lookup handlers.
        /// </summary>
        /// <param name="viewClinicServicesService">The application workflow engine routing medical service listings.</param>
        public ViewListServiceController(IViewClinicServicesService viewClinicServicesService)
        {
            _viewClinicServicesService = viewClinicServicesService;
        }

        /// <summary>
        /// Retrieves a paginated, filterable collection of medical services tied to the authenticated manager's clinic.
        /// </summary>
        /// <param name="request">The parameters holding explicit page offsets, capacity blocks, and search terms.</param>
        /// <returns>
        /// <c>200 OK</c> detailing an integrated collection of target models matched with tracking metadata boundaries;
        /// <c>400 Bad Request</c> if verification routines trip data integrity blocks or system parameters fail validation.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ViewClinicServiceResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<ViewClinicServiceResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetClinicServices([FromQuery] ViewClinicServicesRequest request)
        {
            var result = await _viewClinicServicesService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}