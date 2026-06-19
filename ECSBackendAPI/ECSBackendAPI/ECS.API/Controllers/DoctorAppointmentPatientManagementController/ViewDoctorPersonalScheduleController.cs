using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorPersonalScheduleServices;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Controller for managing doctor's personal schedule.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class ViewDoctorPersonalScheduleController : ControllerBase
    {
        private readonly IViewDoctorPersonalScheduleService _service;

        public ViewDoctorPersonalScheduleController(
            IViewDoctorPersonalScheduleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDoctorPersonalScheduleController"/>.
        /// </summary>
        /// <param name="service">Service for retrieving doctor's schedule data.</param>
        [HttpGet("{id:guid}/schedule")]
        public async Task<IActionResult> GetPersonalSchedule(
            Guid id,
            [FromQuery] DateOnly workDate,
            [FromQuery] ShiftType? shiftType = null)
        {
            var request = new ViewDoctorPersonalScheduleRequest
            {
                WorkDate = workDate,
                ShiftType = shiftType,
            };
            var result = await _service.Process(id, request);
            return Ok(result);
        }
    }
}
