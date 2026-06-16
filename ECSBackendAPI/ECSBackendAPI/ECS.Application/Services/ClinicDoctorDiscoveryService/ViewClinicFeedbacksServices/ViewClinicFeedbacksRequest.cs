namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices
{
    /// <summary>
    /// Represents the request for paginated clinic feedbacks.
    /// </summary>
    public class ViewClinicFeedbacksRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}