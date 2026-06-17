using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for retrieving and filtering patient profiles list.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/patients")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistGetPatientsListController : ControllerBase
    {
        private readonly IReceptionistGetPatientsListService _patientsService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistGetPatientsListController"/> with required dependencies.
        /// </summary>
        /// <param name="patientsService">The service handling patient profiles query and pagination.</param>
        public ReceptionistGetPatientsListController(IReceptionistGetPatientsListService patientsService)
        {
            _patientsService = patientsService;
        }

        /// <summary>
        /// Retrieves a paginated list of patient profiles with active filtration for receptionists.
        /// </summary>
        /// <param name="request">The patient filter parameters including search name, phone, page number, and page size.</param>
        /// <returns>
        /// <c>200 OK</c> with the paginated list of patient profiles and metadata;
        /// <c>401 Unauthorized</c> if the user context is invalid or missing name identifier;
        /// <c>403 Forbidden</c> if the user is not a receptionist.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPatientsForReceptionist([FromQuery] ReceptionistGetPatientsListRequest request)
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            {
                return Unauthorized();
            }
            request.CurrentUserId = userId;
            var result = await _patientsService.Process(request);
            return Ok(result);
        }
    }
}