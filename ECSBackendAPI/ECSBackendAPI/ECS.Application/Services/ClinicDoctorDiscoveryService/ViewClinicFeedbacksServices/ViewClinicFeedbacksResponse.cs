namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices
{
    /// <summary>
    /// Represents the paginated response of clinic feedbacks
    /// including rating summary.
    /// </summary>
    public class ViewClinicFeedbacksResponse
    {
        public decimal? RatingAvg { get; set; }
        public int? ReviewCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public List<ClinicFeedbackItem> Feedbacks { get; set; } = [];
    }
}