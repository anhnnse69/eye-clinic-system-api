using ECS.Domain.Entities.Notifications;
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

        /// Creates a notification for the specified user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="content">Notification content.</param>
        public async Task CreateAsync(Guid userId, string title, string content)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Content = content,
                IsRead = false,
                SentAt = DateTime.UtcNow,
            };
            await _notificationCommandRepo.CreateAsync(notification);
            await _notificationCommandRepo.SaveChangesAsync();
        }
    }
}
