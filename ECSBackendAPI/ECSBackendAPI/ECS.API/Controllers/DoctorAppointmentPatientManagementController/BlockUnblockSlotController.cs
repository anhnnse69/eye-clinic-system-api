using ECS.Application.Services.DoctorAppointmentPatientManagementServices.EditDoctorScheduleServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Manages doctor slot blocking operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class BlockUnblockSlotController : ControllerBase
    {
        private readonly IBlockUnblockSlotService _service;

        /// <summary>
        /// Initializes the controller.
        /// </summary>
        public BlockUnblockSlotController(IBlockUnblockSlotService service)
        {
            _service = service;
        }

        /// <summary>
        /// Blocks or unblocks a doctor's time slot.
        /// </summary>
        /// <param name="id">Doctor user identifier.</param>
        /// <param name="slotId">Time slot identifier.</param>
        /// <param name="request">Block/unblock request.</param>
        /// <returns>The updated slot status.</returns>
        [HttpPatch("{id:guid}/schedule/slots/{slotId:guid}/block")]
        public async Task<IActionResult> ToggleBlock(
            Guid id,
            Guid slotId,
            [FromBody] BlockUnblockSlotRequest request)
        {
            try
            {
                var result = await _service.Process(id, slotId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
