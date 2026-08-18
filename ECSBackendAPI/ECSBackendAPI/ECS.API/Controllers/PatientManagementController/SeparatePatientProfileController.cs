using ECS.Application.Services.PatientProfileManagementServices.SeparateProfileServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// API Controller for separating dependent child profiles into independent accounts.
    /// Handles the business logic for detaching child profiles from parent accounts
    /// and creating standalone user accounts for children.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/profiles")]
    [Authorize(Roles = "PATIENT")]
    public class SeparatePatientProfileController : ControllerBase
    {
        private readonly ISeparatePatientProfileService _separateService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeparatePatientProfileController"/> class.
        /// </summary>
        /// <param name="separateService">The service for separating dependent profiles into independent accounts.</param>
        public SeparatePatientProfileController(ISeparatePatientProfileService separateService)
        {
            _separateService = separateService;
        }


        /// <response code="200">Profile separation successful</response>
        /// <response code="400">Validation failed or business rule violation</response>
        /// <response code="401">User not authenticated or lacks PATIENT role</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{patientProfileId}/separate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SeparateProfile(
            [FromRoute] string patientProfileId,
            [FromBody] SeparateProfileRequest request)
        {
            // Execute profile separation service
            var result = await _separateService.Process(request);

            // Return response with appropriate HTTP status
            return Ok(result);
        }
    }
}
