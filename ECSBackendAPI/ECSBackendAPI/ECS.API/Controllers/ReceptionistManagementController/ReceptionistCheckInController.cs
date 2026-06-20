using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCheckInServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for capturing patient arrivals, managing real-time check-in sequences, and orchestrating downstream room queue assignments.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/appointments/arrive")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistCheckInController : ControllerBase
    {
        private readonly IReceptionistCheckInService _checkInService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCheckInController"/> with required orchestration service dependencies.
        /// </summary>
        /// <param name="checkInService">The service handling isolated check-in verification pipelines, sequential index calculation, and database transaction tracking.</param>
        public ReceptionistCheckInController(IReceptionistCheckInService checkInService)
        {
            _checkInService = checkInService;
        }

        /// <summary>
        /// Processes a secure patient check-in request, mutating appointment statuses and allocating an incremental line token node within designated clinic boundaries.
        /// </summary>
        /// <param name="request">The comprehensive criteria payload enclosing the targeted appointment identity token reference.</param>
        /// <returns>
        /// <c>200 OK</c> packing a synchronized telemetry data packet mapping final structural status changes and structural queue parameters;
        /// <c>400 BadRequest</c> containing localized error codes if domain constraints validation drops.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckIn([FromBody] ReceptionistCheckInRequest request)
        {
            var result = await _checkInService.Process(request);
            return result.CodeMessage == "APP_MESSAGE_2000" ? Ok(result) : BadRequest(result);
        }
    }
}