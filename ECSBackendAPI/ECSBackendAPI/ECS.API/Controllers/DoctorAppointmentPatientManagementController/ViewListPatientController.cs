using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Controller for doctor's patient list operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctors")]
    [Authorize(Roles = "DOCTOR")]
    public class ViewListPatientController : ControllerBase
    {
        private readonly IViewListPatientService _viewPatientListService;

        public ViewListPatientController(
            IViewListPatientService viewPatientListService)
        {
            _viewPatientListService = viewPatientListService;
        }

        /// <summary>
        /// Returns the paginated list of patients
        /// who booked appointments with the doctor
        /// identified by the related user account id.
        /// </summary>
        /// <param name="id">User ID (GUID) linked to the doctor profile.</param>
        /// <param name="pageNumber">Page number (default 1).</param>
        /// <param name="pageSize">Page size (default 10).</param>
        /// <param name="status">
        /// Optional appointment status filter.
        /// </param>
        [HttpGet("{id:guid}/patients")]
        public async Task<IActionResult> GetPatientList(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] AppointmentStatus? status = null)
        {
            var result = await _viewPatientListService.Process(
                id,
                new ViewListPatientRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Status = status
                });

            return Ok(result);
        }
    }
}
