namespace ECS.Application.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices
{
    /// <summary>
    /// Request object containing filtering and paging parameters for retrieving feedback history.
    /// </summary>
    public class ViewMyFeedbackHistoryRequest
    {
        /// <summary>
        /// Search keyword matching patient name, doctor name, clinic name or comment.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Current page index.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of records per page.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
