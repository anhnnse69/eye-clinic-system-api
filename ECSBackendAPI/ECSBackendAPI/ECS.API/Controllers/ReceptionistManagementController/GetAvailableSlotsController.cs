using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for retrieving doctor schedule availability and matrices.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/scheduler")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class GetAvailableSlotsController : ControllerBase
    {
        private readonly IGetAvailableSlotsService _schedulerService;

        /// <summary>
        /// Initializes a new instance of <see cref="GetAvailableSlotsController"/> with required dependencies.
        /// </summary>
        /// <param name="schedulerService">The service handling available time slots queries.</param>
        public GetAvailableSlotsController(IGetAvailableSlotsService schedulerService)
        {
            _schedulerService = schedulerService;
        }

        /// <summary>
        /// Retrieves the clinic scheduler matrix containing available time slots for receptionists.
        /// </summary>
        /// <param name="request">The scheduler filter parameters including work date, shift type, or specialty.</param>
        /// <returns>
        /// <c>200 OK</c> with the list of available doctor shift matrices;
        /// <c>401 Unauthorized</c> if the user context is invalid or missing name identifier;
        /// <c>403 Forbidden</c> if the user is not a receptionist.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetClinicSchedulerMatrix([FromQuery] GetAvailableSlotsRequest request)
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            {
                return Unauthorized();
            }
            request.CurrentUserId = userId;
            var result = await _schedulerService.Process(request);
            return Ok(result);
        }
    }
}