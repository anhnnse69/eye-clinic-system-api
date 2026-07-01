using ECS.Application.Services.DoctorScheduleManagementServices.GetActiveRoomsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// API Controller providing endpoints to retrieve active facility rooms within the receptionist's clinic.
    /// Secured explicitly for users holding the 'RECEPTIONIST' role.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class GetActiveRoomsController : ControllerBase
    {
        private readonly IGetActiveRoomsService _roomsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActiveRoomsController"/> class.
        /// </summary>
        /// <param name="roomsService">The application service processing active facility rooms retrieval.</param>
        public GetActiveRoomsController(
            IGetActiveRoomsService roomsService)
        {
            _roomsService = roomsService;
        }

        /// <summary>
        /// Retrieves a list of all active facility rooms associated with the operating receptionist's clinic boundary.
        /// </summary>
        /// <returns>An HTTP 200 OK status containing the collection of active room metadata details.</returns>
        [HttpGet("rooms")]
        public async Task<IActionResult> GetActiveRooms()
        {
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _roomsService.Process(receptionistUserId);
            return Ok(result);
        }
    }
}