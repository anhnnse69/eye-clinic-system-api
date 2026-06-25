using ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
        /// <summary>
        /// API Controller exposing secure administrative endpoints for managing
        /// patient feedback records associated with the authenticated clinic.
        /// </summary>
        [ApiController]
        [Route("api/v1/clinic-admin/feedbacks")]
        [Authorize(Roles = "CLINIC_ADMIN")]
        public class ClinicDeleteClinicFeedbackController : ControllerBase
        {
            private readonly IDeleteClinicFeedbackService _service;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ClinicDeleteClinicFeedbackController"/> class.
    /// </summary>
    /// <param name="service">
    /// Business workflow service responsible for feedback deletion operations.
    /// </param>
    public ClinicDeleteClinicFeedbackController(
        IDeleteClinicFeedbackService service)
            {
                _service = service;
            }

            /// <summary>
            /// Performs a soft delete operation on a feedback record belonging
            /// to the authenticated clinic administrator's clinic context.
            /// </summary>
            /// <param name="feedbackId">
            /// Unique identifier of the feedback targeted for deletion.
            /// </param>
            /// <returns>
            /// An HTTP 200 OK response containing the standardized business
            /// operation result envelope.
            /// </returns>
            [HttpDelete("{feedbackId}")]
            public async Task<IActionResult> DeleteFeedback(
                [FromRoute] string feedbackId)
            {
                var request = new DeleteClinicFeedbackRequest
                {
                    FeedbackId = feedbackId
                };

                // Execute business workflow pipeline
                var result = await _service.Process(request);

                // Return standardized API response payload
                return Ok(result);
            }
        }
}

