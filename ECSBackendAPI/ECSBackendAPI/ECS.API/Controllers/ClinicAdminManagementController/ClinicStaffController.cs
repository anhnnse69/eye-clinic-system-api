using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles clinic staff management endpoints for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/staff")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicStaffController : ControllerBase
    {
        private readonly IViewListStaffService _viewListStaffService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicStaffController"/>.
        /// </summary>
        /// <param name="viewListStaffService">The service handling clinic staff list business logic.</param>
        public ClinicStaffController(IViewListStaffService viewListStaffService)
        {
            _viewListStaffService = viewListStaffService;
        }

        /// <summary>
        /// Retrieves the list of staff accounts associated with the authenticated administrator's clinic.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with staff account details list if found;
        /// <c>400 Bad Request</c> if user validation fails or the clinic context does not exist.
        /// </returns>
        [HttpGet("accounts")]
        [ProducesResponseType(typeof(ApiResponse<List<StaffAccountResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<StaffAccountResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStaffAccounts()
        {
            // Execute the retrieval process with an empty request object
            var result = await _viewListStaffService.Process(new ViewListStaffRequest());
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}