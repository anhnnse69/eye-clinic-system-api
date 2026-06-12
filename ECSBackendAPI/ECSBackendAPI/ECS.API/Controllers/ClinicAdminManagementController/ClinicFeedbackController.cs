using ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// API Controller exposing backend secure access gates to manage or audit customer feedback logs within the administrative context.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/feedbacks")]
    public class ClinicFeedbackController : ControllerBase
    {
        private readonly IGetClinicFeedbacksService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicFeedbackController"/> class with injected core process services.
        /// </summary>
        /// <param name="service">The structural business application workflow process processor handling feedback retrievals.</param>
        public ClinicFeedbackController(
            IGetClinicFeedbacksService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves a paginated envelope block of filtered clinic execution feedbacks securely tied to the authorized operator's clinic context.
        /// </summary>
        /// <param name="request">The query parameter bindings capturing keyword entries, page indexes, and specific numerical rating boundaries.</param>
        /// <returns>An HTTP 200 OK action result layout surrounding the serialized standard business outcome packages.</returns>
        [HttpGet]
        [Authorize(Roles = "CLINIC_ADMIN")]
        public async Task<IActionResult> GetFeedbacks(
            [FromQuery] GetClinicFeedbacksRequest request)
        {
            // Execute application workflows through asynchronous pipeline layers
            var result = await _service.Process(request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}