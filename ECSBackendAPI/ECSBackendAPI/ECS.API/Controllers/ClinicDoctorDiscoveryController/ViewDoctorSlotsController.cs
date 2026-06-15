using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicDoctorDiscoveryController
{
    /// <summary>
    /// Controller for retrieving doctor available slots.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    public class ViewDoctorSlotsController : ControllerBase
    {
        private readonly IViewDoctorSlotsService _viewDoctorSlotsService;

        public ViewDoctorSlotsController(
            IViewDoctorSlotsService viewDoctorSlotsService)
        {
            _viewDoctorSlotsService = viewDoctorSlotsService;
        }

        /// <summary>
        /// Returns doctor info and available time slots
        /// for the next 30 days.
        /// </summary>
        /// <param name="id">Doctor profile ID (GUID).</param>
        [HttpGet("{id:guid}/slots")]
        public async Task<IActionResult> GetDoctorSlots(Guid id)
        {
            var result = await _viewDoctorSlotsService.Process(id);
            return Ok(result);
        }
    }
}
