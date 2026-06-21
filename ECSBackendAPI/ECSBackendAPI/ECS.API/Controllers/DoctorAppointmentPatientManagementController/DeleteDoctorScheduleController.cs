using ECS.Application.Services.DoctorAppointmentPatientManagementServices.DeleteDoctorScheduleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Provides endpoints for managing doctor personal schedules.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class DeleteDoctorScheduleController : ControllerBase
    {
        private readonly IDeleteDoctorScheduleService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteDoctorScheduleController"/> class.
        /// </summary>
        /// <param name="service">
        /// Service responsible for deleting doctor schedules.
        /// </param>
        public DeleteDoctorScheduleController(IDeleteDoctorScheduleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Deletes a doctor schedule.
        /// </summary>
        /// <param name="id">Doctor identifier.</param>
        /// <param name="scheduleId">Schedule identifier.</param>
        /// <returns>Deleted schedule information.</returns>
        [HttpDelete("{id:guid}/schedule/{scheduleId:guid}")]
        public async Task<IActionResult> DeleteSchedule(
            Guid id,
            Guid scheduleId)
        {
            var result = await _service.Process(id, scheduleId);
            return Ok(result);
        }
    }
}
