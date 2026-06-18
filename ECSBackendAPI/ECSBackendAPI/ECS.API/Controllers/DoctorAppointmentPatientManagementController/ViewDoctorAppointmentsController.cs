using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorAppointmentsServices;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Provides endpoints for doctors
    /// to view their appointments.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class ViewDoctorAppointmentsController : ControllerBase
    {
        private readonly IViewDoctorAppointmentsService _service;

        /// <summary>
        /// Initializes a new instance of the controller.
        /// </summary>
        /// <param name="service">
        /// The doctor appointment service.
        /// </param>
        public ViewDoctorAppointmentsController(
            IViewDoctorAppointmentsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Returns the doctor's appointment list
        /// with optional filtering and pagination.
        /// </summary>
        [HttpGet("{id:guid}/appointments")]
        public async Task<IActionResult> GetAppointments(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] AppointmentStatus? status = null,
            [FromQuery] DateOnly? date = null,
            [FromQuery] string? search = null)
        {
            var request = new ViewDoctorAppointmentsRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                Date = date,
                Search = search,
            };

            var result = await _service.Process(id, request);
            return Ok(result);
        }
    }
}
