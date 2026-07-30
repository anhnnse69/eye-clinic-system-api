using System.Security.Claims;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewClinicAppointmentsServices;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Provides endpoints for receptionists to view appointments
    /// across every doctor within their own clinic.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/appointments")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ViewClinicAppointmentsController : ControllerBase
    {
        private readonly IViewClinicAppointmentsService _service;

        /// <summary>
        /// Initializes a new instance of the controller.
        /// </summary>
        /// <param name="service">The clinic appointment service.</param>
        public ViewClinicAppointmentsController(
            IViewClinicAppointmentsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Returns the clinic's appointment list across all doctors,
        /// with optional filtering and pagination. The clinic is resolved
        /// from the authenticated receptionist's staff assignment.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAppointments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] AppointmentStatus? status = null,
            [FromQuery] DateOnly? date = null,
            [FromQuery] string? search = null,
            [FromQuery] Guid? doctorId = null)
        {
            // Extract the authenticated Receptionist's User ID from the JWT bearer token claims
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var request = new ViewClinicAppointmentsRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                Date = date,
                Search = search,
                DoctorId = doctorId,
            };

            var result = await _service.Process(receptionistUserId, request);
            return Ok(result);
        }
    }
}