using ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices;
using ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.SystemAdminController
{
    [ApiController]
    [Route("api/v1/system-admin/applications")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class RejectClinicApplicationController : ControllerBase
    {
        private readonly IRejectClinicApplicationService _rejectService;

        public RejectClinicApplicationController(IRejectClinicApplicationService rejectService)
        {
            _rejectService = rejectService;
        }

        [HttpPost("{id:guid}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectClinicApplicationRequest request)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(adminIdClaim, out Guid adminId);
            var result = await _rejectService.Process(id, request, adminId);
            return Ok(result);
        }
    }
}
