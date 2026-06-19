using ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for querying and discovering existing system user accounts.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/users")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistSearchAccountController : ControllerBase
    {
        private readonly IReceptionistSearchAccountService _searchService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistSearchAccountController"/> with required dependencies.
        /// </summary>
        /// <param name="searchService">The service driving the query pipeline and evaluation for user discovery.</param>
        public ReceptionistSearchAccountController(IReceptionistSearchAccountService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Searches existing user records within the system filtered by Name, Phone, or Email to facilitate profile linkage.
        /// </summary>
        /// <param name="request">The evaluation payload package containing dynamic filtering criteria parameters.</param>
        /// <returns>
        /// <c>200 OK</c> with a structured collection of matched user telemetry maps;
        /// <c>401 Unauthorized</c> if user context validation is unsuccessful;
        /// <c>403 Forbidden</c> if the request context lacks the receptionist role.
        /// </returns>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SearchAccounts([FromQuery] ReceptionistSearchAccountRequest request)
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out _))
            {
                return Unauthorized();
            }
            var result = await _searchService.Process(request);
            return Ok(result);
        }
    }
}