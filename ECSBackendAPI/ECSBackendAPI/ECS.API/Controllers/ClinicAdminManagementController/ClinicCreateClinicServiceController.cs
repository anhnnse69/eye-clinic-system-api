using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles medical service management endpoints for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/clinic-services/create")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicCreateClinicServiceController : ControllerBase
    {
        private readonly ICreateService _createService;
        private readonly IValidator<CreateServiceRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicServiceController"/> with business services.
        /// </summary>
        /// <param name="createService">The workflow processor class executing resource creations.</param>
        /// <param name="validator">The FluentValidation runtime execution engine contract.</param>
        public ClinicCreateClinicServiceController(
            ICreateService createService,
            IValidator<CreateServiceRequest> validator)
        {
            _createService = createService;
            _validator = validator;
        }

        /// <summary>
        /// Registers a new medical service line specific to the context location of the logged admin.
        /// </summary>
        /// <param name="request">The input parameter metrics identifying the properties of the new service.</param>
        /// <returns>
        /// <c>200 OK</c> with generation logs metadata details if successfully written;
        /// <c>400 Bad Request</c> if verification filters fail or validation constraints are breached.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateServiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateServiceResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            // Execute standalone validation workflows decoupling processing components
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                // Grabs the first assigned matching error profile mapping
                var firstError = validationResult.Errors.First();
                return BadRequest(ApiResponse<CreateServiceResponse>.Fail(firstError.ErrorCode));
            }

            // Route standard parameters downwards executing persistent records processing
            var result = await _createService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
