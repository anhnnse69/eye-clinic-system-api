using ECS.Application.Services.DoctorScheduleManagementServices.DeleteDoctorScheduleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// API Controller handling operations to soft-delete or remove doctor schedules.
    /// Access is restricted to authorized users holding the 'RECEPTIONIST' role.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/doctors")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class DeleteDoctorScheduleController : ControllerBase
    {
        private readonly IDeleteDoctorScheduleService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteDoctorScheduleController"/> class.
        /// </summary>
        /// <param name="service">The application service responsible for validating and deleting doctor schedules.</param>
        public DeleteDoctorScheduleController(IDeleteDoctorScheduleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Deletes a specific schedule assigned to a doctor after verifying business rule compliance.
        /// </summary>
        /// <param name="id">The unique identifier (GUID) of the target Doctor Profile.</param>
        /// <param name="scheduleId">The unique identifier (GUID) of the specific schedule slot structure to delete.</param>
        /// <returns>An HTTP Action result handling both successful payloads and business rule violations mapping.</returns>
        [HttpDelete("{id:guid}/schedule/{scheduleId:guid}")]
        public async Task<IActionResult> DeleteSchedule(Guid id, Guid scheduleId)
        {
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var result = await _service.Process(receptionistUserId, id, scheduleId);
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