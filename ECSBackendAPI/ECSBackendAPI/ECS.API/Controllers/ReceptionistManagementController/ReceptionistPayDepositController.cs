using ECS.Application.Services.ReceptionistManagementServices.ReceptionistPayDepositServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for committing downstream monetary transaction safety blocks and updating appointment financial states.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/appointments/pay-deposit")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistPayDepositController : ControllerBase
    {
        private readonly IReceptionistPayDepositService _payDepositService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistPayDepositController"/> with required transactional service components.
        /// </summary>
        /// <param name="payDepositService">The service orchestrating functional state verification and down-stream financial processing pipeline.</param>
        public ReceptionistPayDepositController(IReceptionistPayDepositService payDepositService)
        {
            _payDepositService = payDepositService;
        }

        /// <summary>
        /// Processes inline financial commitment confirmations, transitioning targeted appointment deposit validation matrices.
        /// </summary>
        /// <param name="request">The structural presentation criteria package encompassing the designated target appointment identifier.</param>
        /// <returns>
        /// <c>200 OK</c> packing a synchronized telemetry data packet mapping final structural updates;
        /// <c>400 BadRequest</c> containing localized error payloads if validation boundaries or identity verifications drop.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PayDeposit([FromBody] ReceptionistPayDepositRequest request)
        {
            var result = await _payDepositService.Process(request);
            return result.CodeMessage == "APP_MESSAGE_2000" ? Ok(result) : BadRequest(result);
        }
    }
}