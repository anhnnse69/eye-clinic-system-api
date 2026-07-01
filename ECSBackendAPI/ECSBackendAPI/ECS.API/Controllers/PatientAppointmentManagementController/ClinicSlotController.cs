using ECS.Application.Services.PatientAppointmentManagementServices.ClinicSlotServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientAppointmentManagementController
{
    /// <summary>
    /// Exposes distributed application network endpoint routing boundaries to manage clinic time slot options bound to physical clinic entities.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinics")]
    [Authorize(Roles = "PATIENT")]
    public class ClinicSlotController : ControllerBase
    {
        private readonly IClinicSlotService _clinicSlotService;

        /// <summary>
        /// Initializes a new operational controller boundary instance with injected orchestration handler dependencies.
        /// </summary>
        /// <param name="clinicSlotService">The abstract application boundary contract execution instance handling clinic slot extraction.</param>
        public ClinicSlotController(IClinicSlotService clinicSlotService)
        {
            _clinicSlotService = clinicSlotService;
        }

        /// <summary>
        /// Resolves available time slot datasets linked directly within the specified physical clinic primary reference coordinate boundaries.
        /// </summary>
        /// <param name="clinicId">The unique identifier tracking mapping matrices of the underlying targeted clinic entity.</param>
        /// <param name="date">The date to check availability for (format: yyyy-MM-dd).</param>
        /// <param name="serviceId">Optional service identifier to filter slots.</param>
        /// <returns>An asynchronous task executing network results containing HTTP 200 state payload structural definitions.</returns>
        [HttpGet("{clinicId}/available-slots")]
        public async Task<IActionResult> GetAvailableSlots(
            Guid clinicId,
            [FromQuery] string date,
            [FromQuery] Guid? serviceId = null)
        {
            var result = await _clinicSlotService.Process(clinicId, date, serviceId);
            return Ok(result);
        }
    }
}