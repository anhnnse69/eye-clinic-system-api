using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.CreateRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles physical infrastructure operations and room management routes for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/rooms")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class CreateRoomController : ControllerBase
    {
        private readonly ICreateRoomService _createRoomService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityRoomController"/> class with application workflow services.
        /// </summary>
        /// <param name="createRoomService">The workflow orchestration interface engine managing execution blocks.</param>
        public CreateRoomController(ICreateRoomService createRoomService)
        {
            _createRoomService = createRoomService;
        }

        /// <summary>
        /// Registers a new clinical room resource bound to the authenticated corporate administrator context.
        /// </summary>
        /// <param name="request">The data transport envelope details processing payload parameters.</param>
        /// <returns>
        /// A <c>200 OK</c> carrying success metadata properties along structural resource details;
        /// A <c>400 Bad Request</c> if administrative claims mapping or naming duplication validation workflows fail.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateRoomResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateRoomResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateRoomRequest request)
        {
            var result = await _createRoomService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
