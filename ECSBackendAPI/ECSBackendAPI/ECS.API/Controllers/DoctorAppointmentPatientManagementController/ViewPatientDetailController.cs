using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Provides endpoints for doctors
    /// to view patient details.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class ViewPatientDetailController : ControllerBase
    {
        private readonly IViewPatientDetailService _service;

        public ViewPatientDetailController(
            IViewPatientDetailService service)
        {
            _service = service;
        }

        /// <summary>
        /// Returns detailed information
        /// about a patient and their appointment history.
        /// </summary>
        [HttpGet("{id:guid}/patients/{patientId:guid}")]
        public async Task<IActionResult> GetPatientDetail(
            Guid id,
            Guid patientId)
        {
            var result = await _service.Process(id, patientId);
            return Ok(result);
        }
    }
}
