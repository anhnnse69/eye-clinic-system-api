using ECS.Application.Common.Response;
using ECS.Domain.Entities.Notifications;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Handles notification read operations.
    /// </summary
    public class MarkNotificationReadService : IMarkNotificationReadService
    {
        private readonly IRepositoryQueryBase<Notification, Guid, AppDbContext>
            _notificationQueryRepo;
        private readonly IRepositoryBaseAsync<Notification, Guid, AppDbContext>
            _notificationCommandRepo;

        /// <summary>
        /// Initializes a new instance of the service.
        /// </summary>
        /// <param name="notificationQueryRepo">Notification query repository.</param>
        /// <param name="notificationCommandRepo">Notification command repository.</param>
        public MarkNotificationReadService(
            IRepositoryQueryBase<Notification, Guid, AppDbContext> notificationQueryRepo,
            IRepositoryBaseAsync<Notification, Guid, AppDbContext> notificationCommandRepo)
        {
            _notificationQueryRepo = notificationQueryRepo;
            _notificationCommandRepo = notificationCommandRepo;
        }

        /// <summary>
        /// Marks the specified notification as read.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <returns>Operation result.</returns>
        public async Task<ApiResponse<string>> Process(
            Guid userId,
            Guid notificationId)
        {
            var notification = await ResolveOwnedNotificationAsync(
                userId,
                notificationId);
            await MarkAsReadAsync(notification);
            return CreateSuccessResponse();
        }

        /// <summary>
        /// Updates the notification read status.
        /// </summary>
        /// <param name="notification">Notification entity.</param>
        private async Task MarkAsReadAsync(Notification notification)
        {
            notification.IsRead = true;

            await _notificationCommandRepo.UpdateAsync(notification);
            await _notificationCommandRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a notification owned by the specified user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <returns>The notification entity.</returns>
        private async Task<Notification> ResolveOwnedNotificationAsync(
            Guid userId,
            Guid notificationId)
        {
            var notification = await _notificationQueryRepo
                .FindByCondition(n =>
                    n.Id == notificationId &&
                    n.UserId == userId)
                .FirstOrDefaultAsync();
            if (notification is null)
            {
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4004.ToString());
            }
            return notification;
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
