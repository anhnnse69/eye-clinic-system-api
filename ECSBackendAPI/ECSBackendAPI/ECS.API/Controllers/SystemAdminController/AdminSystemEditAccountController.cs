using ECS.Application.Common.Response;
using ECS.Application.Services.SystemAdminServices.AdminSystemEditAccountServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Coordinates system management account properties modification endpoint routes processing vectors.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/account")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemEditAccountController : ControllerBase
    {
        private readonly IEditAccountService _editAccountService;

        /// <summary>
        /// Initializes a new instance of <see cref="AccountManagementController"/> parsing relevant logic endpoints actions.
        /// </summary>
        /// <param name="editAccountService">The target core workflow implementation handler injection pointer.</param>
        public AdminSystemEditAccountController(IEditAccountService editAccountService)
        {
            _editAccountService = editAccountService;
        }

        /// <summary>
        /// Processes modifications parameters mapping updates queries onto active persistent backend entities.
        /// </summary>
        /// <param name="request">The detailed target property configurations structure values model definitions.</param>
        /// <returns>
        /// <c>200 OK</c> with fresh data maps structural response on success;
        /// <c>400 Bad Request</c> if duplicate metrics hit or target boundaries are missing.
        /// </returns>
        [HttpPut("edit")]
        [ProducesResponseType(typeof(ApiResponse<EditAccountResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EditAccountResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditAccount([FromBody] EditAccountRequest request)
        {
            var result = await _editAccountService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
