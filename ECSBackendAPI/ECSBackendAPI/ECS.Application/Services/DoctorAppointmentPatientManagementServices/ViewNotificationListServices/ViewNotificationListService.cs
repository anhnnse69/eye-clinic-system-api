using ECS.Application.Common.Response;
using ECS.Domain.Entities.Notifications;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Retrieves the paginated notification list for a patient,
    /// optionally filtered by read status.
    /// </summary>
    public class ViewNotificationListService : IViewNotificationListService
    {
        private readonly IRepositoryQueryBase<Notification, Guid, AppDbContext>
            _notificationRepo;

        /// <summary>
        /// Initializes a new instance of the service.
        /// </summary>
        /// <param name="notificationRepo">Notification repository.</param>
        public ViewNotificationListService(
            IRepositoryQueryBase<Notification, Guid, AppDbContext> notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        /// <summary>
        /// Retrieves a paginated list of notifications for the given user.
        /// </summary>
        /// <param name="userId">Identifier of the authenticated user.</param>
        /// <param name="request">Pagination and read-status filter.</param>
        /// <returns>A successful response containing the notification list.</returns>
        public async Task<ApiResponse<ViewNotificationListResponse>> Process(
            Guid userId,
            ViewNotificationListRequest request)
        {
            var (pageNumber, pageSize) = NormalizePaging(request);

            var totalRecords = await CountNotificationsAsync(userId, request.IsRead);
            var totalPages = CalculateTotalPages(totalRecords, pageSize);
            var unreadCount = await CountUnreadAsync(userId);
            var notifications = await FetchNotificationsAsync(
                userId, request.IsRead, pageNumber, pageSize);
            var response = BuildResponse(
                notifications, pageNumber, pageSize, totalPages, totalRecords, unreadCount);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Normalizes paging parameters.
        /// </summary>
        private static (int PageNumber, int PageSize) NormalizePaging(
            ViewNotificationListRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            return (pageNumber, pageSize);
        }

        /// <summary>
        /// Counts notifications matching the filter.
        ///</summary>
        private async Task<int> CountNotificationsAsync(Guid userId, bool? isRead)
        {
            return await _notificationRepo
                .FindByCondition(n =>
                    n.UserId == userId &&
                    (isRead == null || n.IsRead == isRead))
                .CountAsync();
        }

        /// <summary>
        /// Counts unread notifications.
        /// </summary>
        private async Task<int> CountUnreadAsync(Guid userId)
        {
            return await _notificationRepo
                .FindByCondition(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// Calculates the total number of pages.
        /// </summary>
        private static int CalculateTotalPages(int totalRecords, int pageSize)
        {
            return pageSize == 0
                ? 0
                : (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        /// <summary>
        /// Retrieves notification items for the current page.
        /// </summary>
        private async Task<List<NotificationItem>> FetchNotificationsAsync(
            Guid userId,
            bool? isRead,
            int pageNumber,
            int pageSize)
        {
            return await _notificationRepo
                .FindByCondition(n =>
                    n.UserId == userId &&
                    (isRead == null || n.IsRead == isRead))
                .OrderByDescending(n => n.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationItem
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    IsRead = n.IsRead,
                    SentAt = DateTime.SpecifyKind(n.SentAt, DateTimeKind.Utc),
                })
                .ToListAsync();
        }

        /// <summary>
        /// Builds the notification list response.
        /// </summary>
        private static ViewNotificationListResponse BuildResponse(
            List<NotificationItem> notifications,
            int pageNumber,
            int pageSize,
            int totalPages,
            int totalRecords,
            int unreadCount)
        {
            return new ViewNotificationListResponse
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                UnreadCount = unreadCount,
                Notifications = notifications,
            };
        }

        /// <summary>
        /// Creates a successful response.
        /// </summary>
        private static ApiResponse<ViewNotificationListResponse> CreateSuccessResponse(
            ViewNotificationListResponse response)
        {
            return ApiResponse<ViewNotificationListResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}
