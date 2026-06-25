namespace ECS.Application.Services.PatientAppointmentManagementServices.SubmitFeedbackServices
{
    /// <summary>
    /// Request object containing feedback data for a completed appointment.
    /// </summary>
    public class SubmitFeedbackRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the appointment to rate.
        /// </summary>
        public Guid AppointmentId { get; set; }

        /// <summary>
        /// Gets or sets the rating for the doctor (1-5).
        /// </summary>
        public int RatingDoctor { get; set; }

        /// <summary>
        /// Gets or sets the rating for the clinic (1-5).
        /// </summary>
        public int RatingClinic { get; set; }

        /// <summary>
        /// Gets or sets the optional comment from the patient.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets whether the feedback is public. Default is true.
        /// </summary>
        public bool IsPublic { get; set; } = true;
    }
}
