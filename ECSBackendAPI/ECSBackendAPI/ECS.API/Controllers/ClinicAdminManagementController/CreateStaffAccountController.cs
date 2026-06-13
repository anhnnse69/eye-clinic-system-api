using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Processes specific incoming operational request commands related to medical clinic staff profiles initialization.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/staff")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class CreateStaffAccountController : ControllerBase
    {
        private readonly ICreateStaffService _createStaffService;

        /// <summary>
        /// Initializes a new instance of <see cref="StaffManagementController"/> with the required workflow orchestration services.
        /// </summary>
        /// <param name="createStaffService">The operational creation workflow service module link.</param>
        public CreateStaffAccountController(ICreateStaffService createStaffService)
        {
            _createStaffService = createStaffService;
        }

        /// <summary>
        /// Onboards a new operational staff identity profile linked directly to the authenticated admin's clinic domain workspace.
        /// </summary>
        /// <param name="request">The incoming data structural configuration attributes tracking input options payload.</param>
        /// <returns>
        /// <c>200 OK</c> along with success metadata validation logs on active creation state;
        /// <c>400 Bad Request</c> containing structured verification mismatch envelopes on tracking collisions.
        /// </returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse<CreateStaffResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateStaffResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            var workflowResult = await _createStaffService.Process(request);

            if (workflowResult.Data is null)
            {
                return BadRequest(workflowResult);
            }

            return Ok(workflowResult);
        }
    }
}