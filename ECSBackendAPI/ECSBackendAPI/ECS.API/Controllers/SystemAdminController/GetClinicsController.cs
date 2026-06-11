using ECS.Application.Services.SystemAdminServices.ClinicManagementServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles clinic management endpoints for system administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/clinics")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class GetClinicsController : ControllerBase
    {
        private readonly IGetClinicsService _clinicsService;

        /// <summary>
        /// Initializes a new instance of <see cref="GetClinicsController"/>.
        /// </summary>
        /// <param name="clinicsService">The service handling clinic retrieval and pagination business logic.</param>
        public GetClinicsController(IGetClinicsService clinicsService)
        {
            _clinicsService = clinicsService;
        }

        /// <summary>
        /// Retrieves a paginated and filtered list of all registered clinics.
        /// </summary>
        /// <param name="request">The query parameters including filtering, searching, and pagination metrics.</param>
        /// <returns>
        /// <c>200 OK</c> with the list of clinics and pagination metadata;
        /// <c>401 Unauthorized</c> if the request lacks valid authentication credentials;
        /// <c>403 Forbidden</c> if the authenticated user is not a system administrator.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllClinics([FromQuery] GetClinicsRequest request)
        {
            var result = await _clinicsService.Process(request);
            return Ok(result);
        }
    }
}