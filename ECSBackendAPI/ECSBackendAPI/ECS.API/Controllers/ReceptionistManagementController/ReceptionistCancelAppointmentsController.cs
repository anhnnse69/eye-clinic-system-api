using ECS.Application.Common.Response;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCancelAppointmentsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for cancelling existing customer appointments.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/appointments/cancel")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistCancelAppointmentsController : ControllerBase
    {
        private readonly IReceptionistCancelAppointmentsService _cancelService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistCancelAppointmentsController"/> with required dependencies.
        /// </summary>
        /// <param name="cancelService">The service handling appointment cancellation workflows.</param>
        public ReceptionistCancelAppointmentsController(IReceptionistCancelAppointmentsService cancelService)
        {
            _cancelService = cancelService;
        }

        /// <summary>
        /// Cancels a specific medical appointment based on receptionist request inputs.
        /// </summary>
        /// <param name="request">The cancellation details including target appointment identity and mandatory justification notes.</param>
        /// <returns>
        /// <c>200 OK</c> with the finalized cancellation state payload;
        /// <c>400 BadRequest</c> if request data annotations fail validation or business rule constraints are breached.
        /// </returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReceptionistCancelAppointmentsResponse>>> CancelAppointment(
            [FromBody] ReceptionistCancelAppointmentsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ReceptionistCancelAppointmentsResponse>.Fail("INVALID_REQUEST_PARAMETERS"));
            }
            var result = await _cancelService.Process(request);
            if (result.CodeMessage != "APP_MESSAGE_2000")
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}