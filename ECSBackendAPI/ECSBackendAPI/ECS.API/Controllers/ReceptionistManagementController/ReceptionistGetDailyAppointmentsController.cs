using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for retrieving and monitoring daily localized appointments lists and metrics.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/appointments/daily")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistGetDailyAppointmentsController : ControllerBase
    {
        private readonly IReceptionistGetDailyAppointmentsService _appointmentsService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistGetDailyAppointmentsController"/> with required dependencies.
        /// </summary>
        /// <param name="appointmentsService">The service handling isolated daily appointments query, filtration, and pagination.</param>
        public ReceptionistGetDailyAppointmentsController(IReceptionistGetDailyAppointmentsService appointmentsService)
        {
            _appointmentsService = appointmentsService;
        }

        /// <summary>
        /// Retrieves a secure, paginated list of daily appointments filtered by clinic boundaries assigned to the active receptionist.
        /// </summary>
        /// <param name="request">The comprehensive filtration payload including target date, shift types, search queries, and pagination tokens.</param>
        /// <returns>
        /// <c>200 OK</c> with the secured, paginated list of appointments data structure and real-time dashboard tracking metadata metrics;
        /// <c>401 Unauthorized</c> if token mapping failures occur or claims are missing;
        /// <c>403 Forbidden</c> if user constraints validation fails.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDailyAppointmentsForReceptionist([FromQuery] ReceptionistGetDailyAppointmentsRequest request)
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            {
                return Unauthorized();
            }
            request.CurrentUserId = userId;
            var result = await _appointmentsService.Process(request);
            return Ok(result);
        }
    }
}