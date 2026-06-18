using ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for mutating administrative patient profiles.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/patients")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistUpdatePatientProfileController : ControllerBase
    {
        private readonly IReceptionistUpdatePatientProfileService _updateService;
        private readonly IValidator<ReceptionistUpdatePatientProfileRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistUpdatePatientProfileController"/> with required dependencies.
        /// </summary>
        /// <param name="updateService">The service handling mutation processes for patient profile records.</param>
        /// <param name="validator">The validator enforcing business constraints on incoming payload records.</param>
        public ReceptionistUpdatePatientProfileController(
            IReceptionistUpdatePatientProfileService updateService,
            IValidator<ReceptionistUpdatePatientProfileRequest> validator)
        {
            _updateService = updateService;
            _validator = validator;
        }

        /// <summary>
        /// Updates the administrative sections of a core patient database record.
        /// </summary>
        /// <param name="id">The unique identifier of the patient profile to update.</param>
        /// <param name="request">The payload containing updated demographic metadata details.</param>
        /// <returns>
        /// <c>200 OK</c> with summarized change metrics;
        /// <c>400 Bad Request</c> if input criteria or validation rules fail;
        /// <c>401 Unauthorized</c> if user context validation is unsuccessful;
        /// <c>403 Forbidden</c> if the user lacks the receptionist role;
        /// <c>404 Not Found</c> if target records are missing.
        /// </returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePatientAdministrativeProfile([FromRoute] Guid id, [FromBody] ReceptionistUpdatePatientProfileRequest request)
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            {
                return Unauthorized();
            }
            // Create a specific validation context wrapping the payload data
            var context = new ValidationContext<ReceptionistUpdatePatientProfileRequest>(request);
            // Attach the target PatientId to RootContextData for uniqueness checks exclusion
            context.RootContextData["TargetPatientId"] = id;
            // Execute asynchronous validation via FluentValidation
            var validationResult = await _validator.ValidateAsync(context);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                var errorCode = firstError?.ErrorMessage ?? "APP_MESSAGE_4019";
                return BadRequest(new { CodeMessage = errorCode });
            }
            try
            {
                var result = await _updateService.Process(id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { CodeMessage = ex.Message });
            }
        }
    }
}