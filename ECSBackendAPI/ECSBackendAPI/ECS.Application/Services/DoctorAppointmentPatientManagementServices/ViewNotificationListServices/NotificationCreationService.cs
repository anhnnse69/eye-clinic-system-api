using ECS.Application.Common.Response;
using ECS.Domain.Entities.Notifications;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Handles creating notifications for users. Used by other services
    /// (e.g. appointment confirmation/rejection) to notify patients.
    /// </summary>
    public class NotificationCreationService : INotificationCreationService
    {
        private readonly IRepositoryBaseAsync<Notification, Guid, AppDbContext>
            _notificationCommandRepo;

        /// <summary>
        /// Initializes a new instance of the service.
        /// </summary>
        /// <param name="notificationCommandRepo">Notification repository.</param>
        public NotificationCreationService(
            IRepositoryBaseAsync<Notification, Guid, AppDbContext> notificationCommandRepo)
        {
            _notificationCommandRepo = notificationCommandRepo;
        }

        /// <summary>
        /// Creates a notification for the specified user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="content">Notification content.</param>
        /// <returns>Operation result.</returns>
        public async Task<ApiResponse<string>> Process(
            Guid userId,
            string title,
            string content)
        {
            var notification = BuildNotification(
                userId,
                title,
                content);
            await CreateNotificationAsync(notification);
            return CreateSuccessResponse();
        }

        /// <summary>
        /// Creates a notification entity.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="content">Notification content.</param>
        /// <returns>Notification entity.</returns>
        private static Notification BuildNotification(
            Guid userId,
            string title,
            string content)
        {
            return new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Content = content,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Persists the notification.
        /// </summary>
        /// <param name="notification">Notification entity.</param>
        private async Task CreateNotificationAsync(
            Notification notification)
        {
            await _notificationCommandRepo.CreateAsync(notification);
            await _notificationCommandRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a successful response.
        /// </summary>
        /// <returns>Success response.</returns>
        private static ApiResponse<string> CreateSuccessResponse()
        {
            return ApiResponse<string>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                "OK");
        }
    }
}
