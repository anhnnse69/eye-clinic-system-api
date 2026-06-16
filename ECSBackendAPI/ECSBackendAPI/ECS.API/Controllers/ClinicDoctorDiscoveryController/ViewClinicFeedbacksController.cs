using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicDoctorDiscoveryController
{
    /// <summary>
    /// Controller for retrieving clinic feedbacks and ratings.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinics")]
    public class ViewClinicFeedbacksController : ControllerBase
    {
        private readonly IViewClinicFeedbacksService _viewClinicFeedbacksService;

        public ViewClinicFeedbacksController(
            IViewClinicFeedbacksService viewClinicFeedbacksService)
        {
            _viewClinicFeedbacksService = viewClinicFeedbacksService;
        }

        /// <summary>
        /// Returns paginated public feedbacks and rating summary
        /// for a clinic.
        /// </summary>
        /// <param name="id">Clinic ID (GUID).</param>
        /// <param name="pageNumber">Page number (default 1).</param>
        /// <param name="pageSize">Page size (default 10).</param>
        [HttpGet("{id:guid}/feedbacks")]
        public async Task<IActionResult> GetClinicFeedbacks(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _viewClinicFeedbacksService.Process(
                id,
                new ViewClinicFeedbacksRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            return Ok(result);
        }
    }
}