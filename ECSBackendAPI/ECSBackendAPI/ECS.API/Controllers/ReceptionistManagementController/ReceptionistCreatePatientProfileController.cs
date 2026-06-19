using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for creating structural patient profiles.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/patients")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistCreatePatientProfileController : ControllerBase
    {
        private readonly IReceptionistCreatePatientProfileService _createService;
        private readonly IValidator<ReceptionistCreatePatientProfileRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCreatePatientProfileController"/> with required dependencies.
        /// </summary>
        /// <param name="createService">The service driving creation and orchestration pipelines for patient onboarding.</param>
        /// <param name="validator">The validator enforcing intake structural rules and uniqueness boundaries.</param>
        public ReceptionistCreatePatientProfileController(
            IReceptionistCreatePatientProfileService createService,
            IValidator<ReceptionistCreatePatientProfileRequest> validator)
        {
            _createService = createService;
            _validator = validator;
        }

        /// <summary>
        /// Registers a brand new patient administrative record linked to an existing user or newly auto-created account.
        /// </summary>
        /// <param name="request">The intake payload data structural criteria context.</param>
        /// <returns>
        /// <c>200 OK</c> with telemetry feedback highlighting newly persisted patient profiles;
        /// <c>400 Bad Request</c> if verification parameters, syntax rules, or constraints fail;
        /// <c>401 Unauthorized</c> if user context token evaluation is unsuccessful;
        /// <c>403 Forbidden</c> if the request scope lacks required receptionist boundaries;
        /// <c>404 Not Found</c> if selected cross-referenced identities are missing.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreatePatientAdministrativeProfile([FromBody] ReceptionistCreatePatientProfileRequest request)
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            {
                return Unauthorized();
            }
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                var errorCode = firstError?.ErrorMessage ?? "APP_MESSAGE_4019";
                return BadRequest(new { CodeMessage = errorCode });
            }
            try
            {
                var result = await _createService.Process(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { CodeMessage = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { CodeMessage = ex.Message });
            }
        }
    }
}