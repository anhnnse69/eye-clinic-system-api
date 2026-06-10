using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    [ApiController]
    [Route("api/v1/system-admin/applications")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class GetClinicApplicationController : ControllerBase
    {
        private readonly IGetClinicApplicationService _applicationService;

        public GetClinicApplicationController(IGetClinicApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllApplications([FromQuery] GetClinicApplicationsRequest request)
        {
            var result = await _applicationService.Process(request);
            return Ok(result);
        }
    }
}