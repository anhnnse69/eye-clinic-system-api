using ECS.Application.Services.ClinicDoctorDiscoveryService.GetActiveSpecialtiesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicDoctorDiscoveryController
{
    /// <summary>
    /// Handles requests related to medical specialties configuration and lookups.
    /// </summary>
    [ApiController]
    [Route("api/v1/specialties")]
    [Authorize(Roles = "DOCTOR,RECEPTIONIST")]
    public class GetActiveSpecialtiesController : ControllerBase
    {
        private readonly IGetActiveSpecialtiesService _specialtiesService;

        /// <summary>
        /// Initializes a new instance of <see cref="GetActiveSpecialtiesController"/> with required dependencies.
        /// </summary>
        /// <param name="specialtiesService">The service handling active specialties retrieval.</param>
        public GetActiveSpecialtiesController(IGetActiveSpecialtiesService specialtiesService)
        {
            _specialtiesService = specialtiesService;
        }

        /// <summary>
        /// Retrieves a comprehensive list of all active medical specialties in the system.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with the collection of active specialties;
        /// <c>401 Unauthorized</c> if the user context is not validated.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetActiveSpecialties()
        {
            // Execute business logic process flow
            var result = await _specialtiesService.Process();
            return Ok(result);
        }
    }
}