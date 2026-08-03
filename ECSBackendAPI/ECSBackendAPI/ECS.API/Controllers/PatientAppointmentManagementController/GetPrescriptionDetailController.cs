using ECS.Application.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// Controller exposing backend endpoints for UC 22: View Prescription.
    /// Allows patients to view complete prescription details associated with an appointment.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient/appointments")]
    [Authorize(Roles = "PATIENT")]
    public class GetPrescriptionDetailController : ControllerBase
    {
        private readonly IGetPrescriptionDetailService _service;

        public GetPrescriptionDetailController(IGetPrescriptionDetailService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves prescription details for a specific appointment ID.
        /// </summary>
        /// <param name="appointmentId">The unique identifier of the appointment.</param>
        /// <returns>An ActionResult containing the prescription details.</returns>
        [HttpGet("{appointmentId}/prescription")]
        public async Task<IActionResult> GetPrescriptionDetail([FromRoute] Guid appointmentId)
        {
            var request = new GetPrescriptionDetailRequest
            {
                AppointmentId = appointmentId
            };

            var result = await _service.Process(request);
            return Ok(result);
        }
    }
}
