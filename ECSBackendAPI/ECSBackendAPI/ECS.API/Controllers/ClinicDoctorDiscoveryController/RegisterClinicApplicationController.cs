using ECS.Application.Services.ClinicDoctorDiscoveryService.RegisterClinicApplicationServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicDoctorDiscoveryController
{
    /// <summary>
    /// Controller responsible for clinic application registration.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class RegisterClinicApplicationController : ControllerBase
    {
        private readonly IRegisterClinicApplicationService
            _registerClinicApplicationService;

        public RegisterClinicApplicationController(
            IRegisterClinicApplicationService registerClinicApplicationService)
        {
            _registerClinicApplicationService =
                registerClinicApplicationService;
        }

        /// <summary>
        /// Submits a clinic application.
        /// </summary>
        [HttpPost("register-clinic-application")]
        public async Task<IActionResult> RegisterClinicApplication(
            [FromBody] RegisterClinicApplicationRequest request)
        {
            var result =
                await _registerClinicApplicationService.Process(request);
            return Ok(result);
        }
    }
}
