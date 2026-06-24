using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Defines operations for marking notifications as read.
    /// </summary>
    public interface IMarkNotificationReadService
    {
        /// <summary>
        /// Marks a notification as read for the specified user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <returns>Operation result message.</returns>
        Task<ApiResponse<string>> Process(Guid userId, Guid notificationId);
    }
}
