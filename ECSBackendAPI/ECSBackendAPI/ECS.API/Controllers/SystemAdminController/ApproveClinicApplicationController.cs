using ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.SystemAdminController
{
    [ApiController]
    [Route("api/v1/system-admin/applications")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class ApproveClinicApplicationController : ControllerBase
    {
        private readonly IApproveClinicApplicationService _approveService;

        public ApproveClinicApplicationController(IApproveClinicApplicationService approveService)
        {
            _approveService = approveService;
        }

        [HttpPost("{id:guid}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Approve(Guid id)
        {
            // Lấy ID của System Admin đang đăng nhập từ Token
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(adminIdClaim, out Guid adminId);
            var result = await _approveService.Process(id, adminId);
            return Ok(result);
        }
    }
}
