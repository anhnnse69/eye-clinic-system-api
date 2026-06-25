using ECS.Application.Common.Response;
using ECS.Application.Services.SystemAdminServices.AdminSystemCreateAccountServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles user profile initialization and system access creations for system administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/accounts/create")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemCreateAccountController : ControllerBase
    {
        private readonly ICreateAccountService _createAccountService;
        private readonly IValidator<CreateAccountRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="UsersController"/> mapping control flows parameters.
        /// </summary>
        /// <param name="createAccountService">The business management boundary workflow logic processor instance.</param>
        /// <param name="validator">The input parameters criteria check model tracking provider.</param>
        public AdminSystemCreateAccountController(ICreateAccountService createAccountService, IValidator<CreateAccountRequest> validator)
        {
            _createAccountService = createAccountService;
            _validator = validator;
        }

        /// <summary>
        /// Registers a clean account identity profile entity preventing duplication constraints checks on baseline metrics.
        /// </summary>
        /// <param name="request">The core registration parameters values details container package data.</param>
        /// <returns>
        /// <c>200 OK</c> with registration structures outputs payloads details if successful;
        /// <c>400 Bad Request</c> if verification layers fail constraints parameters evaluations.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateAccountResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateAccountResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            // Execute independent pipeline verification screening layout rules properties matches
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var primaryError = validationResult.Errors.First();
                return BadRequest(ApiResponse<CreateAccountResponse>.Fail(primaryError.ErrorCode));
            }

            // Route process structures targets executing business flow operations lines
            var result = await _createAccountService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
