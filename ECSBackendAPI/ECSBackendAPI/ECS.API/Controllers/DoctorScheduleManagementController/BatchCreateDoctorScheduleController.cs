using System.Security.Claims;
using ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// Provides endpoints for receptionists to create doctor schedules in batch.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/doctors")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class BatchCreateDoctorScheduleController : ControllerBase
    {
        private readonly IBatchCreateDoctorScheduleService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchCreateDoctorScheduleController"/> class.
        /// </summary>
        /// <param name="service">
        /// Service responsible for validating and creating doctor schedules in batch.
        /// </param>
        public BatchCreateDoctorScheduleController(IBatchCreateDoctorScheduleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates schedules for multiple doctors, dates, and shifts.
        /// </summary>
        /// <param name="request">The schedule creation request.</param>
        /// <returns>The operation result.</returns>
        [HttpPost("schedule/batch")]
        public async Task<IActionResult> BatchCreateSchedule(
            [FromBody] BatchCreateDoctorScheduleRequest request)
        {
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var result = await _service.Process(receptionistUserId, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}