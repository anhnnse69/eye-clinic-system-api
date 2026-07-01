using System.Security.Claims;
using ECS.Application.Services.DoctorScheduleManagementServices.GetActiveDoctorsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorScheduleManagementController
{
    /// <summary>
    /// API Controller providing endpoints to retrieve active doctors within the receptionist's clinic.
    /// Secured explicitly for users holding the 'RECEPTIONIST' role.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class GetActiveDoctorsController : ControllerBase
    {
        private readonly IGetActiveDoctorsService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActiveDoctorsController"/> class.
        /// </summary>
        /// <param name="service">The application service processing active doctor retrieval.</param>
        public GetActiveDoctorsController(IGetActiveDoctorsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves a list of all active doctors associated with the operating receptionist's clinic.
        /// </summary>
        /// <returns>An HTTP 200 OK status containing the collection of active doctor profiles.</returns>
        [HttpGet("doctors")]
        public async Task<IActionResult> GetActiveDoctors()
        {
            var receptionistUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.Process(receptionistUserId);
            return Ok(result);
        }
    }
}