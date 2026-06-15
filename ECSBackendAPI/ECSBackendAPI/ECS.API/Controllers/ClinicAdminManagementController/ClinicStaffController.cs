using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Controller managing clinic data query channels and account profile lists for administration monitors.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/staff")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ViewListStaffController : ControllerBase
    {
        private readonly IViewListStaffService _viewListStaffService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewListStaffController"/> class with injected execution workflows.
        /// </summary>
        /// <param name="viewListStaffService">The domain service engine processing multi-state facility staff lists queries.</param>
        public ViewListStaffController(IViewListStaffService viewListStaffService)
        {
            _viewListStaffService = viewListStaffService;
        }

        /// <summary>
        /// Retrieves a paginated, multi-state visible, filtered, and searched list of facility staff accounts.
        /// </summary>
        /// <param name="request">Query metrics including string lookups, pagination page, size bounds, and visibility toggles.</param>
        /// <returns>An authorized HTTP action result encapsulating matching staff account profile information packages.</returns>
        [HttpGet("accounts")]
        [ProducesResponseType(typeof(ApiResponse<List<StaffAccountResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<StaffAccountResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStaffList([FromQuery] ViewListStaffRequest request)
        {
            var result = await _viewListStaffService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}