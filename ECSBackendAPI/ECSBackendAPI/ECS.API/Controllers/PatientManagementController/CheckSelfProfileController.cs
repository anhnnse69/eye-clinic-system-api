using ECS.Application.Services.PatientProfileManagementServices.CheckSelfProfileServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// Web API Controller endpoints managing operations for checking patient self profile ownership.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/profiles")]
    [Authorize(Roles = "PATIENT")]
    public class CheckSelfProfileController : ControllerBase
    {
        private readonly ICheckSelfProfileService _checkSelfProfileService;

        /// <summary>
        /// Initializes a new instance of <see cref="CheckSelfProfileController"/> with required dependencies.
        /// </summary>
        /// <param name="checkSelfProfileService">Service that handles the self patient profile existence checking business logic.</param>
        public CheckSelfProfileController(ICheckSelfProfileService checkSelfProfileService)
        {
            _checkSelfProfileService = checkSelfProfileService;
        }

        /// <summary>
        /// Verifies whether the currently authenticated patient user already owns a self patient profile record.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the self profile existence check result wrapped in a standard api response.</returns>
        [HttpGet("check-self-profile")]
        public async Task<IActionResult> CheckSelfProfile()
        {
            // Execute application workflows through asynchronous pipeline layers
            var result = await _checkSelfProfileService.Process();

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}
