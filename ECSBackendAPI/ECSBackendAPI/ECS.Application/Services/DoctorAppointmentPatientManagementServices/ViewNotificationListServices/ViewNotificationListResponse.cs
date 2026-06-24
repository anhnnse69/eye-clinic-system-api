namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Response model for notification list retrieval.
    /// </summary>
    public class ViewNotificationListResponse
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int UnreadCount { get; set; }
        public List<NotificationItem> Notifications { get; set; } = [];
    }

    /// <summary>
    /// Notification information.
    /// </summary>
    public class NotificationItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
    }
}
