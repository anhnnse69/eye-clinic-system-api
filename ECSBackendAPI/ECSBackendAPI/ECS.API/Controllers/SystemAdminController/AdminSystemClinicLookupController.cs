using ECS.Application.Common.Response;
using ECS.Application.Services.SystemAdminServices.GetClinicLookupServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles administrative system global lookup metrics endpoints for platform operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/clinics")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class ClinicLookupController : ControllerBase
    {
        private readonly IGetClinicLookupService _clinicLookupService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicLookupController"/>.
        /// </summary>
        /// <param name="clinicLookupService">The service handling structural lookups mapping global active clinics.</param>
        public ClinicLookupController(IGetClinicLookupService clinicLookupService)
        {
            _clinicLookupService = clinicLookupService;
        }

        /// <summary>
        /// Retrieves a minimized metadata identity lookup array pairing names with unique IDs across active system clinics.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with lightweight minimal metadata details lookup collection wrapper array data payloads.
        /// </returns>
        [HttpGet("lookup")]
        [ProducesResponseType(typeof(ApiResponse<List<GetClinicLookupResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClinicLookup()
        {
            var result = await _clinicLookupService.Process(new GetClinicLookupRequest());
            return Ok(result);
        }
    }
}
