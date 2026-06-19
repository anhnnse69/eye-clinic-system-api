using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Controller for doctors to confirm or reject appointments.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class ConfirmRejectAppointmentController : ControllerBase
    {
        private readonly IConfirmRejectAppointmentService _confirmRejectService;

        public ConfirmRejectAppointmentController(
            IConfirmRejectAppointmentService confirmRejectService)
        {
            _confirmRejectService = confirmRejectService;
        }

        /// <summary>
        /// Confirms or rejects an appointment.
        /// </summary>
        [HttpPatch("{id:guid}/appointments/{appointmentId:guid}/decision")]
        public async Task<IActionResult> Decide(
            Guid id,
            Guid appointmentId,
            [FromBody] ConfirmRejectAppointmentRequest request)
        {
            var result = await _confirmRejectService.Process(
                id, appointmentId, request);
            return Ok(result);
        }
    }
}
