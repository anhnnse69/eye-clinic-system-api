using ECS.Application.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// API Controller exposing secure access endpoints for patients to retrieve their feedback history records.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/feedbacks")]
    [Authorize(Roles = "PATIENT")]
    public class ViewMyFeedbackHistoryController : ControllerBase
    {
        private readonly IViewMyFeedbackHistoryService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackController"/> class with injected feedback retrieval workflows.
        /// </summary>
        /// <param name="service">The business workflow processor responsible for feedback history retrieval operations.</param>
        public ViewMyFeedbackHistoryController(
            IViewMyFeedbackHistoryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves a paginated collection of feedback records belonging to patient profiles accessible by the authenticated user.
        /// </summary>
        /// <param name="request">
        /// The query parameter container capturing keyword filters and pagination configuration values.
        /// </param>
        /// <returns>
        /// An HTTP 200 response wrapping the standardized feedback history payload structure.
        /// </returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetFeedbackHistory(
            [FromQuery] ViewMyFeedbackHistoryRequest request)
        {
            // Execute application workflow processing pipelines asynchronously
            var result = await _service.Process(request);

            // Return standardized response payload envelope
            return Ok(result);
        }
    }
}
