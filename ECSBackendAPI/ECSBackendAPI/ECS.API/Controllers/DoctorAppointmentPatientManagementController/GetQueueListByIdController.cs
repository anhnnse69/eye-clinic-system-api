using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetQueueListByIdServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Controller for getting queue list by doctor ID.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR,CLINIC_ADMIN,RECEPTIONIST")]
    public class GetQueueListByIdController : ControllerBase
    {
        private readonly IGetQueueListByIdService _service;

        public GetQueueListByIdController(IGetQueueListByIdService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the queue list for a specific doctor by ID.
        /// </summary>
        /// <param name="doctorId">The doctor profile ID.</param>
        /// <param name="date">The date to get queue list (defaults to today).</param>
        /// <returns>Queue list with statistics.</returns>
        [HttpGet("{doctorId:guid}/queue")]
        public async Task<IActionResult> GetQueueListById(
            Guid doctorId,
            [FromQuery] DateOnly? date = null)
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.Now);
            var result = await _service.Process(doctorId, targetDate);
            return Ok(result);
        }
    }
}
