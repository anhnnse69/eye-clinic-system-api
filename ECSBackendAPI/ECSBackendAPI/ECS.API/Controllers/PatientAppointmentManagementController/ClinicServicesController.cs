using ECS.Application.Services.PatientAppointmentManagementServices.GetClinicServicesForBookingServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// Exposes distributed application network endpoint routing boundaries to manage service options bound to physical clinic entities.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinics")]
    public class ClinicServicesController : ControllerBase
    {
        private readonly IGetClinicServicesForBookingService _servicesService;

        /// <summary>
        /// Initializes a new operational controller boundary instance with injected orchestration handler dependencies.
        /// </summary>
        /// <param name="servicesService">The abstract application boundary contract execution instance handling operational services extraction.</param>
        public ClinicServicesController(IGetClinicServicesForBookingService servicesService)
        {
            _servicesService = servicesService;
        }

        /// <summary>
        /// Resolves active healthcare service datasets configured directly within the specified physical clinic primary reference coordinate boundaries.
        /// </summary>
        /// <param name="id">The unique identifier tracking mapping matrices of the underlying targeted clinic entity.</param>
        /// <returns>An asynchronous task executing network results containing HTTP 200 state payload structural definitions.</returns>
        [HttpGet("{id:guid}/services")]
        public async Task<IActionResult> GetServices(Guid id) => Ok(await _servicesService.Process(id));
    }
}