using System.Security.Claims;
using ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// API Controller providing endpoints for receptionists to manage and create doctor shifts.
    /// Secured explicitly to users with the 'RECEPTIONIST' role.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/doctors")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class CreateDoctorScheduleController : ControllerBase
    {
        private readonly ICreateDoctorScheduleService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDoctorScheduleController"/> class.
        /// </summary>
        /// <param name="service">The application service processing doctor schedule creation.</param>
        public CreateDoctorScheduleController(ICreateDoctorScheduleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates a new work schedule shift for a specific doctor.
        /// </summary>
        /// <param name="id">The unique identifier (GUID) of the target Doctor Profile.</param>
        /// <param name="request">The request body payload containing metadata like shift types, target room, and work dates.</param>
        /// <returns>An HTTP 200 OK status containing the newly generated schedule response metadata.</returns>
        [HttpPost("{id:guid}/schedule")]
        public async Task<IActionResult> CreateSchedule(
            Guid id,
            [FromBody] CreateDoctorScheduleRequest request)
        {
            // Extract the authenticated Receptionist's User ID from the JWT bearer token claims
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            // Forward the orchestration logic execution context down to the application service layer
            var result = await _service.Process(receptionistUserId, id, request);
            return Ok(result);
        }
    }
}