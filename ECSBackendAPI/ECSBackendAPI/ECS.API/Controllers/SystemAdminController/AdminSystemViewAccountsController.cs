using ECS.Application.Common.Response;
using ECS.Application.Services.SystemAdminServices.AdminSystemListAccountServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles system level authentication accounts view control administration workflow endpoint interfaces.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/accounts")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemViewAccountsController : ControllerBase
    {
        private readonly IGetAccountsService _getAccountsService;

        /// <summary>
        /// Initializes a new instance of <see cref="AccountsController"/> mapping infrastructure application services.
        /// </summary>
        /// <param name="getAccountsService">The targeted logic operation processing service initialization provider asset.</param>
        public AdminSystemViewAccountsController(IGetAccountsService getAccountsService)
        {
            _getAccountsService = getAccountsService;
        }

        /// <summary>
        /// Retrieves a paged and filterable list layout containing user accounts definitions mapping system profiles configurations records.
        /// </summary>
        /// <param name="request">The filtering inputs parameters, sorting constraints, and explicit segmentation criteria matrix context.</param>
        /// <returns>
        /// <c>200 OK</c> enclosing paginated account collection objects entries on success;
        /// <c>400 Bad Request</c> if administrative session privilege verification breaks validation metrics parameters.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<GetAccountResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<GetAccountResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAccounts([FromQuery] GetAccountsRequest request)
        {
            // Redirect requests specifications directly onto internal decoupled operational layer instances
            var result = await _getAccountsService.Process(request);

            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
