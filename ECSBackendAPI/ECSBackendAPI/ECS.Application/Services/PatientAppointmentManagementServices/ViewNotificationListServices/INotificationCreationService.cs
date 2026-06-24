using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Defines operations for creating notifications.
    /// </summary>
    public interface INotificationCreationService
    {
        /// <summary>
        /// Creates a notification for the specified user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="content">Notification content.</param>
        Task<ApiResponse<string>> Process(Guid userId, string title, string content);
    }
}