using ECS.Application.Services.DoctorAppointmentPatientManagementServices.EditDoctorScheduleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Provides endpoints for doctors to edit their personal schedules.
    /// Allows updating work date and/or assigned room for a schedule.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class EditDoctorScheduleController : ControllerBase
    {
        private readonly IEditDoctorScheduleService _service;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="EditDoctorScheduleController"/> class.
        /// </summary>
        /// <param name="service">
        /// Service responsible for editing doctor schedules.
        /// </param>
        public EditDoctorScheduleController(IEditDoctorScheduleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Updates a doctor's schedule.
        /// </summary>
        /// <param name="id">Doctor user identifier.</param>
        /// <param name="scheduleId">Schedule identifier.</param>
        /// <param name="request">Updated schedule information.</param>
        /// <returns>The updated schedule.</returns>
        [HttpPut("{id:guid}/schedule/{scheduleId:guid}")]
        public async Task<IActionResult> EditSchedule(
            Guid id,
            Guid scheduleId,
            [FromBody] EditDoctorScheduleRequest request)
        {
            try
            {
                var result = await _service.Process(id, scheduleId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
