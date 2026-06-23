using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListRoomServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Manages clinic infrastructure assets and room allocations for validated clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/rooms")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicViewClinicRoomController : ControllerBase
    {
        private readonly IGetClinicRoomsService _getClinicRoomsService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicRoomController"/> wrapping structural application workflows.
        /// </summary>
        /// <param name="getClinicRoomsService">The domain application engine parsing infrastructure queries.</param>
        public ClinicViewClinicRoomController(IGetClinicRoomsService getClinicRoomsService)
        {
            _getClinicRoomsService = getClinicRoomsService;
        }

        /// <summary>
        /// Fetches a filtered, queryable, and paginated collection listing room assets belonging directly to the manager's clinic.
        /// </summary>
        /// <param name="request">Optional pagination bounds and string filtering metrics container.</param>
        /// <returns>
        /// <c>200 OK</c> with matching dataset rows accompanied by pagination tracking elements;
        /// <c>400 Bad Request</c> if user parsing evaluations degrade or organization setups drop out of active boundaries.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<GetClinicRoomResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<GetClinicRoomResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRooms([FromQuery] GetClinicRoomsRequest request)
        {
            var result = await _getClinicRoomsService.Process(request);

            if (result.Data is null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
