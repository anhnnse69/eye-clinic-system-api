using System.Security.Claims;
using ECS.Application.Services.DoctorScheduleManagementServices.EditDoctorScheduleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// Provides endpoints for receptionists to edit a doctor's schedule
    /// (the doctor must belong to the receptionist's own clinic).
    /// Allows updating work date and/or assigned room for a schedule.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/doctors")]
    [Authorize(Roles = "RECEPTIONIST")]
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
        /// <param name="id">Identifier of the DoctorProfile that owns the schedule.</param>
        /// <param name="scheduleId">Schedule identifier.</param>
        /// <param name="request">Updated schedule information.</param>
        /// <returns>The updated schedule.</returns>
        [HttpPut("{id:guid}/schedule/{scheduleId:guid}")]
        public async Task<IActionResult> EditSchedule(
            Guid id,
            Guid scheduleId,
            [FromBody] EditDoctorScheduleRequest request)
        {
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var result = await _service.Process(receptionistUserId, id, scheduleId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}