using ECS.Application.Common.Response;
using ECS.Application.Services.AuthServices.ViewAccountInfoServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.AuthController
{
    /// <summary>
    /// Handles account information endpoints.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    [Authorize]
    public class ViewAccountInfoController : ControllerBase
    {
        private readonly IViewAccountInfoService _service;

        public ViewAccountInfoController(IViewAccountInfoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Returns the account information for the specified user.
        /// If <paramref name="userId"/> is omitted, the id is taken from the JWT.
        /// </summary>
        [HttpGet("me")]
        [HttpGet("user-info/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ViewAccountInfoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ViewAccountInfoResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get([FromRoute] Guid? userId)
        {
            if (!TryResolveUserId(userId, out var resolvedId, out var error))
                return BadRequest(error);

            var result = await _service.Process(new ViewAccountInfoRequest { UserId = resolvedId });
            return result.Data is null ? BadRequest(result) : Ok(result);
        }

        private bool TryResolveUserId(Guid? routeId, out Guid userId, out ApiResponse<ViewAccountInfoResponse>? error)
        {
            error = null;

            if (routeId is { } id && id != Guid.Empty)
            {
                userId = id;
                return true;
            }

            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (Guid.TryParse(claim, out userId))
                return true;

            error = ApiResponse<ViewAccountInfoResponse>.Fail(
                Domain.Enums.GeneralCode.APP_MESSAGE_4003.ToString());
            return false;
        }
    }
}
