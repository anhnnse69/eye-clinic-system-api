namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentHistoryServices
{
    /// <summary>
    /// Request object containing parameters for filtering and paginating appointment history records for the logged-in user.
    /// </summary>
    public class GetAppointmentHistoryRequest
    {
        /// <summary>
        /// Gets or sets the target text keyword match constraint utilized against ClinicName, DoctorName, or PatientName.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by Appointment Status (PENDING, CONFIRMED, COMPLETED, CANCELLED)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the sequential layout segment tracking index boundary parameter. Default configuration starts at index 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the upper bound limit size of elements inside a solitary segment page boundary. Default constraint is 10 rows.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
