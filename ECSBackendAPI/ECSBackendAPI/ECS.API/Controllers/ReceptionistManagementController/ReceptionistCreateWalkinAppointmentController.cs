using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for registering and orchestrating walk-in clinic appointments.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/appointments/walk-in")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistCreateWalkinAppointmentController : ControllerBase
    {
        private readonly IReceptionistCreateWalkinAppointmentService _walkInService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCreateWalkinAppointmentController"/> with required dependencies.
        /// </summary>
        /// <param name="walkInService">The business service handling walk-in appointment registrations.</param>
        public ReceptionistCreateWalkinAppointmentController(IReceptionistCreateWalkinAppointmentService walkInService)
        {
            _walkInService = walkInService;
        }

        /// <summary>
        /// Registers a direct walk-in appointment at the counter and puts the patient into the active room queue.
        /// </summary>
        /// <param name="request">The walk-in registration arguments including profiles, services, and identifiers.</param>
        /// <returns>
        /// <c>200 OK</c> with registration details if processed successfully;
        /// <c>400 BadRequest</c> containing system validation failure messages if processing limits fail.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterWalkIn([FromBody] ReceptionistCreateWalkinAppointmentRequest request)
        {
            var result = await _walkInService.Process(request);
            return result.CodeMessage == "APP_MESSAGE_2000" ? Ok(result) : BadRequest(result);
        }
    }
}