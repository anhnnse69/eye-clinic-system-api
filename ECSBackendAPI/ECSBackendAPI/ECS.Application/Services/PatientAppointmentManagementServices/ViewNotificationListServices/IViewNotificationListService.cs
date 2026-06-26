using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Defines operations for retrieving notifications.
    /// </summary
    public interface IViewNotificationListService
    {
        /// <summary>
        /// Retrieves a paginated list of notifications for the specified user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="request">Notification query parameters.</param>
        /// <returns>A paginated notification list.</returns>
        Task<ApiResponse<ViewNotificationListResponse>> Process(
            Guid userId,
            ViewNotificationListRequest request);
    }
}
