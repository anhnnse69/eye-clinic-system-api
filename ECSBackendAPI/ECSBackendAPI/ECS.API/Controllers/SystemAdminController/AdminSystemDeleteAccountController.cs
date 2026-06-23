using ECS.Application.Common.Response;
using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteAccountServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles user account soft-deletion, locking, and status activation management.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/accounts")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemDeleteAccountController : ControllerBase
    {
        private readonly IDeleteAccountService _deleteAccountService;
        private readonly IValidator<DeleteAccountRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="UserManagementController"/> with required endpoint dependencies.
        /// </summary>
        /// <param name="deleteAccountService">The core service processor driving user account deletion workflows.</param>
        /// <param name="validator">The standalone fluent model mapping validator pipeline execution boundary node.</param>
        public AdminSystemDeleteAccountController(
            IDeleteAccountService deleteAccountService,
            IValidator<DeleteAccountRequest> validator)
        {
            _deleteAccountService = deleteAccountService;
            _validator = validator;
        }

        /// <summary>
        /// Soft-deletes, locks, or restores a target user account by changing its active status flag.
        /// </summary>
        /// <param name="request">The matching parameters container detail containing targeted identification keys.</param>
        /// <returns>
        /// <c>200 OK</c> with descriptive status outcome metrics upon complete mutation;
        /// <c>400 Bad Request</c> if validation fails or target entities could not be resolved.
        /// </returns>
        [HttpPut("delete")]
        [ProducesResponseType(typeof(ApiResponse<DeleteAccountResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DeleteAccountResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            // Execute automated model checking layers using standalone validator components explicitly
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse<DeleteAccountResponse>.Fail(
                    validationResult.Errors.First().ErrorCode));
            }

            // Run processing lines across administrative operations models
            var result = await _deleteAccountService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
