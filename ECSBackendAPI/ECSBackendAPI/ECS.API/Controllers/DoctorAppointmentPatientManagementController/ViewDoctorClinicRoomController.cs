using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorClinicRoomsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Provides endpoints for retrieving clinic rooms
    /// available to the authenticated doctor.
    /// </summary>
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class ViewDoctorClinicRoomController : ControllerBase
    {
        private readonly IViewDoctorClinicRoomsService _service;

        /// <summary>
        /// Initializes a new instance of
        /// <see cref="ViewDoctorClinicRoomController"/>.
        /// </summary>
        public ViewDoctorClinicRoomController(
            IViewDoctorClinicRoomsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves all active rooms belonging to the doctor's clinic.
        /// </summary>
        /// <param name="id">
        /// Identifier of the doctor.
        /// </param>
        /// <returns>
        /// A response containing the list of active clinic rooms.
        /// </returns>
        [HttpGet("{id:guid}/rooms")]
        public async Task<IActionResult> GetRooms(Guid id)
        {
            var result = await _service.Process(id);
            return Ok(result);
        }
    }
}
