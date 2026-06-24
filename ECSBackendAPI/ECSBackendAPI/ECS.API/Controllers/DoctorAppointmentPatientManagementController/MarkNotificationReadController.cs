using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewNotificationListServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Manage notification read status.
    /// </summary>
    [ApiController]
    [Route("api/v1/patients")]
    [Authorize(Roles = "PATIENT")]
    public class MarkNotificationReadController : ControllerBase
    {
        private readonly IMarkNotificationReadService _markReadService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkNotificationReadController"/> class.
        /// </summary>
        /// <param name="markReadService">Service for marking notifications as read.</param>
        public MarkNotificationReadController(
            IMarkNotificationReadService markReadService)
        {
            _markReadService = markReadService;
        }

        /// <summary>
        /// Marks a notification as read.
        /// </summary>
        /// <param name="id">Patient ID.</param>
        /// <param name="notificationId">Notification ID.</param>
        [HttpPatch("{id:guid}/notifications/{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(
            Guid id,
            Guid notificationId)
        {
            var result = await _markReadService.Process(id, notificationId);
            return Ok(result);
        }
    }
}
