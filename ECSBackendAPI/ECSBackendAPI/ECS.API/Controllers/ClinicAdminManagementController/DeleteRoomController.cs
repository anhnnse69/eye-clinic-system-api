using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.DeleteRoom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Manages architectural spatial deletion and status alteration scopes within administration bounds.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/rooms")]
    public class DeleteRoomController : ControllerBase
    {
        private readonly IDeleteRoomService _deleteRoomService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicRoomDeletionController"/> class with functional contracts.
        /// </summary>
        /// <param name="deleteRoomService">The business pipeline contract managing room status state mutations.</param>
        public DeleteRoomController(IDeleteRoomService deleteRoomService)
        {
            _deleteRoomService = deleteRoomService;
        }

        /// <summary>
        /// Performs soft deletion or visibility status unlocking transformations onto an active facility room record.
        /// </summary>
        /// <param name="id">The unique target facility room tracker identifier extracted from route bounds.</param>
        /// <param name="request">The data packet containing configuration indicators mapping targeted states.</param>
        /// <returns>
        /// A <c>200 OK</c> success envelope detailing structural change values;
        /// A <c>400 Bad Request</c> error matrix payload if validation criteria rules crash.
        /// </returns>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<DeleteRoomResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DeleteRoomResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleRoomStatus([FromRoute] Guid id, [FromBody] DeleteRoomRequest request)
        {
            // Bind routing primary identifiers directly onto the execution criteria data packet
            request.RoomId = id;

            var result = await _deleteRoomService.Process(request);

            if (result.Data is null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
