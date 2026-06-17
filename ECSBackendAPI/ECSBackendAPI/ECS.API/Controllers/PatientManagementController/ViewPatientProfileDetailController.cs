using ECS.Application.Services.PatientProfileManagementServices.ViewPatientProfileDetailServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// API Controller exposing secure endpoints for viewing detailed patient profile information.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient-profiles")]
    [Authorize]
    public class ViewPatientProfileDetailController : ControllerBase
    {
        private readonly IViewPatientProfileDetailService
            _viewPatientProfileDetailService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewPatientProfileDetailController"/> class with injected profile detail workflow services.
        /// </summary>
        /// <param name="viewPatientProfileDetailService">
        /// The application service responsible for retrieving detailed patient profile information.
        /// </param>
        public ViewPatientProfileDetailController(
            IViewPatientProfileDetailService viewPatientProfileDetailService)
        {
            _viewPatientProfileDetailService =
                viewPatientProfileDetailService;
        }

        /// <summary>
        /// Retrieves complete detail information for a specific patient profile accessible to the authenticated user.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the target patient profile record.
        /// </param>
        /// <returns>
        /// An HTTP 200 OK action result containing a standardized application response payload.
        /// </returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            // Execute application workflow pipeline to retrieve detailed profile information
            var result =
                await _viewPatientProfileDetailService.Process(id);

            // Return standardized response payload to API consumers
            return Ok(result);
        }
    }
}
