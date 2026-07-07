using ECS.Application.Services.DoctorScheduleManagementServices.ClinicShiftServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// Provides the actual shift time ranges (computed from the clinic's
    /// OpenTime/CloseTime) for the receptionist's clinic, so the frontend
    /// can display accurate labels instead of hardcoded hours.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/clinic")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ClinicShiftController : ControllerBase
    {
        private readonly IClinicShiftService _clinicShiftService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicShiftController"/> class.
        /// </summary>
        /// <param name="clinicShiftService">The clinic shift service.</param>
        public ClinicShiftController(IClinicShiftService clinicShiftService)
        {
            _clinicShiftService = clinicShiftService;
        }

        /// <summary>
        /// Returns Morning/Afternoon/Evening time ranges for the
        /// receptionist's clinic.
        /// </summary>
        [HttpGet("shift-ranges")]
        public async Task<IActionResult> GetShiftRanges()
        {
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _clinicShiftService
                .Process(receptionistUserId);
            return Ok(result);
        }
    }
}