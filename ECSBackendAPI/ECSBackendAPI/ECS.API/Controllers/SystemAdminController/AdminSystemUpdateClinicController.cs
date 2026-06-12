using ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles system administrator endpoints for updating clinic information.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/clinics")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemUpdateClinicController : ControllerBase
    {
        private readonly IUpdateClinicService _updateClinicService;
        private readonly IValidator<UpdateClinicRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="AdminSystemUpdateClinicController"/> with required dependencies.
        /// </summary>
        /// <param name="updateClinicService">The service handling clinic update business logic.</param>
        /// <param name="validator">The validator for the clinic update request.</param>
        public AdminSystemUpdateClinicController(
            IUpdateClinicService updateClinicService,
            IValidator<UpdateClinicRequest> validator)
        {
            _updateClinicService = updateClinicService;
            _validator = validator;
        }

        /// <summary>
        /// Updates a specific clinic's details based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <param name="request">The updated clinic data payload.</param>
        /// <returns>
        /// <c>200 OK</c> with the updated clinic details on success;
        /// <c>400 Bad Request</c> if validation fails.
        /// </returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateClinic([FromRoute] Guid id, [FromBody] UpdateClinicRequest request)
        {
            // Create a specific validation context wrapping the payload data
            var context = new ValidationContext<UpdateClinicRequest>(request);
            // Attach the clinic ID to RootContextData for internal validator identification
            context.RootContextData["ClinicId"] = id;
            // Execute asynchronous validation with the configured context
            var validationResult = await _validator.ValidateAsync(context);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                var errorCode = firstError?.ErrorMessage ?? "APP_MESSAGE_4019";
                return BadRequest(new { CodeMessage = errorCode });
            }
            // Execute business logic process flow
            var result = await _updateClinicService.Process(id, request);
            return Ok(result);
        }
    }
}