using ECS.Application.Services.PatientProfileManagementServices.CreatePatientProfileServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// Web API Controller endpoints managing operations for physical Patient Medical Profiles.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient-profiles")]
    [Authorize(Roles = "PATIENT")]
    public class CreatePatientProfileController : ControllerBase
    {
        private readonly ICreatePatientProfileService _createPatientProfileService;

        /// <summary>
        /// Initializes a new instance of <see cref="CreatePatientProfileController"/> with required dependencies.
        /// </summary>
        /// <param name="createPatientProfileService">Service that handles the patient profile creation business logic.</param>
        public CreatePatientProfileController(ICreatePatientProfileService createPatientProfileService)
        {
            _createPatientProfileService = createPatientProfileService;
        }

        /// <summary>
        /// Registers a new medical profile linked into the authorized requesting user data session boundary scope.
        /// </summary>
        /// <param name="request">The detailed serialization structural payload containing identity attributes variables.</param>
        /// <returns>An <see cref="IActionResult"/> containing the profile creation result wrapped in a standard api response.</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateProfile([FromBody] CreatePatientProfileRequest request)
        {
            // Execute application workflows through asynchronous pipeline layers
            var result = await _createPatientProfileService.Process(request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}