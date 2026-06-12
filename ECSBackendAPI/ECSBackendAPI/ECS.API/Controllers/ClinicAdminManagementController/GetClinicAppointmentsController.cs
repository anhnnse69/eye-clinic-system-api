using ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles appointment management endpoints for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/appointments")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class GetClinicAppointmentsController : ControllerBase
    {
        private readonly IGetClinicAppointmentsService _appointmentService;

        public GetClinicAppointmentsController(IGetClinicAppointmentsService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        /// <summary>
        /// Retrieves a paginated and filtered list of appointments for the clinic.
        /// </summary>
        /// <param name="request">The query parameters including clinicId, filtering, searching, and pagination metrics.</param>
        /// <returns>
        /// <c>200 OK</c> with the list of appointments and pagination metadata.
        /// </c>401 Unauthorized</c> if token missing or invalid.
        /// <c>403 Forbidden</c> if role is not Clinic Admin.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetClinicAppointments([FromQuery] GetClinicAppointmentsRequest request)
        {
            var result = await _appointmentService.Process(request);
            return Ok(result);
        }
    }
}
