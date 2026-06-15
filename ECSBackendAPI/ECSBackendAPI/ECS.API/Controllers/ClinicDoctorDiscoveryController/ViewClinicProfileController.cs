using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicDoctorDiscoveryController
{
    /// <summary>
    /// Controller for retrieving clinic profiles.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinics")]
    public class ViewClinicProfileController : ControllerBase
    {
        private readonly IViewClinicProfileService _viewClinicProfileService;

        public ViewClinicProfileController(
            IViewClinicProfileService viewClinicProfileService)
        {
            _viewClinicProfileService = viewClinicProfileService;
        }

        /// <summary>
        /// Returns the full public profile of a clinic by its ID.
        /// </summary>
        /// <param name="id">Clinic ID (GUID).</param>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetClinicProfile(Guid id)
        {
            var result = await _viewClinicProfileService.Process(id);
            return Ok(result);
        }
    }
}
