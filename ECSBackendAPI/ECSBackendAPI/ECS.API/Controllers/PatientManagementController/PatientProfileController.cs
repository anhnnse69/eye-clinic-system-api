using ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// API Controller exposing backend secure access gates for patients to view their medical profiles and family profiles.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/profiles")]
    [Authorize(Roles = "PATIENT")]
    public class PatientProfileController : ControllerBase
    {
        private readonly IGetPatientProfilesService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientProfileController"/> class with injected profile processing workflows.
        /// </summary>
        /// <param name="service">The structural business application workflow process processor handling profiles retrievals.</param>
        public PatientProfileController(IGetPatientProfilesService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves a paginated envelope block of profile records belonging to or linked via relationships to the authenticated patient.
        /// </summary>
        /// <param name="request">The query parameter bindings capturing keyword entries, page numbers, and bounds.</param>
        /// <returns>An HTTP 200 OK action result layout surrounding the serialized standard business outcome packages.</returns>
        [HttpGet]

        public async Task<IActionResult> GetProfiles([FromQuery] GetPatientProfilesRequest request)
        {
            // Execute application workflows through asynchronous pipeline layers
            var result = await _service.Process(request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}
