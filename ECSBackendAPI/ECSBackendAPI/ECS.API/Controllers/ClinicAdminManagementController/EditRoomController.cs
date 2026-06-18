using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.EditRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles physical layout room configuration allocations for authorized clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/rooms")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class EditRoomController : ControllerBase
    {
        private readonly IEditRoomService _editRoomService;

        /// <summary>
        /// Initializes a new instance of <see cref="FacilityRoomController"/>.
        /// </summary>
        /// <param name="editRoomService">The orchestration service component managing facility rooms business logic.</param>
        public EditRoomController(IEditRoomService editRoomService)
        {
            _editRoomService = editRoomService;
        }

        /// <summary>
        /// Modifies details and configuration structures representing an existing facility room entry.
        /// </summary>
        /// <param name="request">The modification request containing payload target updates information variables.</param>
        /// <returns>
        /// A status code element representing <c>200 OK</c> upon success parameters mapping, 
        /// or <c>400 Bad Request</c> if validation metrics or identity tracking fails.
        /// </returns>
        [HttpPut("edit")]
        [ProducesResponseType(typeof(ApiResponse<EditRoomResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EditRoomResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditRoom([FromBody] EditRoomRequest request)
        {
            var result = await _editRoomService.Process(request);

            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
