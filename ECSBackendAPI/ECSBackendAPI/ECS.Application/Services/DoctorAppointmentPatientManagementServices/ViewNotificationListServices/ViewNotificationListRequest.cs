namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewNotificationListServices
{
    /// <summary>
    /// Request model for retrieving notifications.
    /// </summary>
    public class ViewNotificationListRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool? IsRead { get; set; }
    }
}
