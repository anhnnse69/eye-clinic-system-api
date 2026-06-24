using ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// Provides endpoints for doctors to create personal schedules.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class CreateDoctorScheduleController : ControllerBase
    {
        private readonly ICreateDoctorScheduleService _service;

        /// <summary>
        /// Initializes a new instance of
        /// <see cref="CreateDoctorScheduleController"/>.
        /// </summary>
        public CreateDoctorScheduleController(
            ICreateDoctorScheduleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates one or more schedules for the specified doctor.
        /// </summary>
        /// <param name="id">
        /// Identifier of the doctor.
        /// </param>
        /// <param name="request">
        /// Schedule creation request.
        /// </param>
        /// <returns>
        /// A response containing created and skipped schedules.
        /// </returns>
        [HttpPost("{id:guid}/schedule")]
        public async Task<IActionResult> CreateSchedule(
            Guid id,
            [FromBody] CreateDoctorScheduleRequest request)
        {
            var result = await _service.Process(id, request);
            return Ok(result);
        }
    }
}
