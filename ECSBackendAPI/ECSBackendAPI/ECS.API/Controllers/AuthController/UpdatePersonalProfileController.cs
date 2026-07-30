using ECS.Application.Services.AuthServices.UpdatePersonalProfileServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.AuthController
{
    /// <summary>
    /// Handles authenticated user endpoints for updating personal profile information.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth/profile")]
    [Authorize(Roles = "PATIENT,DOCTOR,CLINIC_ADMIN,RECEPTIONIST,SYSTEM_ADMIN")]
    public class UpdatePersonalProfileController : ControllerBase
    {
        private readonly IUpdatePersonalProfileService _profileService;
        private readonly IValidator<UpdatePersonalProfileRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdatePersonalProfileController"/> with required dependencies.
        /// </summary>
        /// <param name="profileService">The service handling personal profile update business logic.</param>
        /// <param name="validator">The validator for the personal profile update request.</param>
        public UpdatePersonalProfileController(
            IUpdatePersonalProfileService profileService,
            IValidator<UpdatePersonalProfileRequest> validator)
        {
            _profileService = profileService;
            _validator = validator;
        }

        /// <summary>
        /// Updates the authenticated user's profile details based on their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="request">The updated personal profile data payload.</param>
        /// <returns>
        /// <c>200 OK</c> with the updated profile details on success;
        /// <c>400 Bad Request</c> if validation fails.
        /// </returns>
        [HttpPut("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateMyProfile([FromRoute] Guid userId, [FromBody] UpdatePersonalProfileRequest request)
        {
            // Create a specific validation context wrapping the payload data
            var context = new ValidationContext<UpdatePersonalProfileRequest>(request);
            // Attach the user ID to RootContextData for internal validator identification
            context.RootContextData["TargetUserId"] = userId;
            // Execute asynchronous validation with the configured context
            var validationResult = await _validator.ValidateAsync(context);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                var errorCode = firstError?.ErrorMessage ?? "APP_MESSAGE_4019";
                return BadRequest(new { CodeMessage = errorCode });
            }
            // Execute business logic process flow
            var result = await _profileService.Process(userId, request);
            return Ok(result);
        }
    }
}