using ECS.Application.Services.PatientAppointmentManagementServices.ViewNotificationListServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Notifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Test.MockData
{
    public static class ViewNotificationListServiceMockData
    {
        public static readonly Guid ValidUserId = Guid.NewGuid();
        public static readonly Guid OtherUserId = Guid.NewGuid();
        public static readonly Guid NotificationId1 = Guid.NewGuid();
        public static readonly Guid NotificationId2 = Guid.NewGuid();

        public static User CreateUser(Guid? id = null)
        {
            return new User
            {
                Id = id ?? ValidUserId,
                Phone = "0901234567",
                Email = "user@test.com",
                FullName = "Test User",
                PasswordHash = "hash"
            };
        }

        public static Notification CreateNotification(
            Guid? id = null,
            Guid? userId = null,
            string title = "Notification Title",
            string content = "Notification Content",
            bool isRead = false,
            DateTime? sentAt = null)
        {
            var uId = userId ?? ValidUserId;
            return new Notification
            {
                Id = id ?? Guid.NewGuid(),
                UserId = uId,
                Title = title,
                Content = content,
                IsRead = isRead,
                SentAt = sentAt ?? DateTime.UtcNow,
                User = CreateUser(uId)
            };
        }

        public static List<Notification> GetSampleNotifications(Guid userId)
        {
            return new List<Notification>
    {
        CreateNotification(
            id: NotificationId1,
            userId: userId,
            title: "Title 1",
            content: "Content 1",
            isRead: false, // Sử dụng dấu hai chấm (:) cho Named Argument
            sentAt: new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc)),
        CreateNotification(
            id: NotificationId2,
            userId: userId,
            title: "Title 2",
            content: "Content 2",
            isRead: true, // Sử dụng dấu hai chấm (:) cho Named Argument
            sentAt: new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc))
    };
        }

        public static ViewNotificationListRequest CreateRequest(
            int pageNumber = 1,
            int pageSize = 10,
            bool? isRead = null)
        {
            return new ViewNotificationListRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                IsRead = isRead
            };
        }
    }
}
