using ECS.Application.Services.PatientAppointmentManagementServices.GetClinicBookingOptionsServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// Exposes distributed application network endpoint routing boundaries to manage doctor options bound to physical clinic entities.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinics")]
    public class ClinicDoctorsController : ControllerBase
    {
        private readonly IGetClinicDoctorsForBookingService _doctorsService;

        /// <summary>
        /// Initializes a new operational controller boundary instance with injected orchestration handler dependencies.
        /// </summary>
        /// <param name="doctorsService">The abstract application boundary contract execution instance handling active doctor profiles extraction.</param>
        public ClinicDoctorsController(IGetClinicDoctorsForBookingService doctorsService)
        {
            _doctorsService = doctorsService;
        }

        /// <summary>
        /// Resolves active doctor profile datasets linked directly within the specified physical clinic primary reference coordinate boundaries.
        /// </summary>
        /// <param name="id">The unique identifier tracking mapping matrices of the underlying targeted clinic entity.</param>
        /// <returns>An asynchronous task executing network results containing HTTP 200 state payload structural definitions.</returns>
        [HttpGet("{id:guid}/doctors")]
        public async Task<IActionResult> GetDoctors(Guid id) => Ok(await _doctorsService.Process(id));
    }
}