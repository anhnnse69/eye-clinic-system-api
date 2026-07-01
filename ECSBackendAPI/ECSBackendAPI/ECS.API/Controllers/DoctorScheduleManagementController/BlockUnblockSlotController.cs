using ECS.Application.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// Manages doctor slot blocking operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/doctors")]
    [Authorize(Roles = "RECEPTIONIST")]
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
        public async Task<IActionResult> ToggleSlotBlock(
            Guid id,
            Guid slotId,
            [FromBody] BlockUnblockSlotRequest request)
        {
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var result = await _service.Process(receptionistUserId, id, slotId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
