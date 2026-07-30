using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentsServices;
using ECS.Application.Services.ReceptionistAppointmentManagementServices.ConfirmRejectAppointmentsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Provides an endpoint for receptionists to confirm or reject
    /// appointments belonging to any doctor within their own clinic.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/appointments")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistConfirmRejectAppointmentController : ControllerBase
    {
        private readonly IReceptionistConfirmRejectAppointmentService _service;

        /// <summary>
        /// Initializes a new instance of the controller.
        /// </summary>
        /// <param name="service">The receptionist confirm/reject service.</param>
        public ReceptionistConfirmRejectAppointmentController(
            IReceptionistConfirmRejectAppointmentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Confirms or rejects an appointment on behalf of the
        /// authenticated receptionist's clinic.
        /// </summary>
        [HttpPatch("{appointmentId:guid}/decision")]
        public async Task<IActionResult> Decide(
            Guid appointmentId,
            [FromBody] ConfirmRejectAppointmentRequest request)
        {
            // Extract the authenticated Receptionist's User ID from the JWT bearer token claims
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _service.Process(receptionistUserId, appointmentId, request);
            return Ok(result);
        }
    }
}