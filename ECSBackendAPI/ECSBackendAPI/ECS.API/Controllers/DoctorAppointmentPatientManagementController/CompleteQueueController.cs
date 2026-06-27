using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Handles queue completion endpoints.
    /// Allows doctors to mark queue items as completed after creating preliminary diagnosis.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor/queue")]
    [Authorize(Roles = "DOCTOR")]
    public class CompleteQueueController : ControllerBase
    {
        private readonly ICompleteQueueService _completeQueueService;

        /// <summary>
        /// Initializes a new instance of <see cref="CompleteQueueController"/>.
        /// </summary>
        /// <param name="completeQueueService">The complete queue service.</param>
        public CompleteQueueController(ICompleteQueueService completeQueueService)
        {
            _completeQueueService = completeQueueService;
        }

        /// <summary>
        /// Marks a queue item as completed.
        /// Patient will no longer appear in the queue list after completion.
        /// Requires that preliminary diagnosis has been created for the appointment.
        /// </summary>
        /// <param name="request">The complete queue request containing queue ID.</param>
        /// <returns>
        /// <c>200 OK</c> with completion details if successful;
        /// <c>400 Bad Request</c> if validation fails, queue not found, or medical record missing.
        /// </returns>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(ApiResponse<CompleteQueueResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CompleteQueueResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteQueue([FromBody] CompleteQueueRequest request)
        {
            var result = await _completeQueueService.Process(request);
            if (result.Data is null)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
