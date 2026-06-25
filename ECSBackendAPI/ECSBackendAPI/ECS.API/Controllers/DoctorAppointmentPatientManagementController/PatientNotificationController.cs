using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewNotificationListServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Manage patient notifications, including viewing notification history
    /// and marking notifications as read.
    /// </summary>
    [ApiController]
    [Route("api/v1/patients")]
    [Authorize(Roles = "PATIENT")]
    public class PatientNotificationController : ControllerBase
    {
        private readonly IViewNotificationListService _listService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientNotificationController"/> class.
        /// </summary>
        /// <param name="listService">Service for retrieving patient notifications.</param>
        public PatientNotificationController(
            IViewNotificationListService listService)
        {
            _listService = listService;
        }

        /// <summary>
        /// Gets a paginated list of notifications for a patient.
        /// </summary>
        /// <param name="id">Patient ID.</param>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="isRead">Filter by read status.</param>
        [HttpGet("{id:guid}/notifications")]
        public async Task<IActionResult> GetNotifications(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isRead = null)
        {
            var request = new ViewNotificationListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                IsRead = isRead,
            };

            var result = await _listService.Process(id, request);
            return Ok(result);
        }
    }
}
