using ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    [ApiController]
    [Route("api/v1/system-admin/applications")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class GetReviewClinicApplicationController : ControllerBase
    {
        private readonly IGetClinicApplicationDetailService _detailService;

        public GetReviewClinicApplicationController(IGetClinicApplicationDetailService detailService)
        {
            _detailService = detailService;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _detailService.Process(id);
            return Ok(result);
        }
    }
}
