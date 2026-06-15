using ECS.Application.Common.Response;
using ECS.Application.Services.AuthServices.ChangePasswordServices;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.AuthController
{
    [ApiController]
    [Route("api/v1/auth")]
    [Authorize]
    public class ChangePasswordController : ControllerBase
    {
        private readonly IChangePasswordService _changePasswordService;

        public ChangePasswordController(IChangePasswordService changePasswordService)
        {
            _changePasswordService = changePasswordService;
        }

        [HttpPut("change-password")]
        [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest changePasswordRequest)
        {
            if (!TryResolveUserId(out var userId, out var error))
                return BadRequest(error);

            var result = await _changePasswordService.Process(userId, changePasswordRequest);
            return result.Data is null ? BadRequest(result) : Ok(result);
        }

        private bool TryResolveUserId(out Guid userId, out ApiResponse<ChangePasswordResponse>? error)
        {
            error = null;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (Guid.TryParse(claim, out userId))
                return true;

            error = ApiResponse<ChangePasswordResponse>.Fail(
                GeneralCode.APP_MESSAGE_4003.ToString());
            return false;
        }
    }
}
