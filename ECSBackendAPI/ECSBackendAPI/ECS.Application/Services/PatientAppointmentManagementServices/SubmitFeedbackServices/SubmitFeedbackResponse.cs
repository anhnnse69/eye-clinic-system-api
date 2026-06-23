namespace ECS.Application.Services.PatientAppointmentManagementServices.SubmitFeedbackServices
{
    /// <summary>
    /// Response object containing the submitted feedback details.
    /// </summary>
    public class SubmitFeedbackResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the feedback.
        /// </summary>
        public string FeedbackId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unique identifier of the appointment.
        /// </summary>
        public string AppointmentId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the rating for the doctor (1-5).
        /// </summary>
        public int RatingDoctor { get; set; }

        /// <summary>
        /// Gets or sets the rating for the clinic (1-5).
        /// </summary>
        public int RatingClinic { get; set; }

        /// <summary>
        /// Gets or sets the comment from the patient.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets whether the feedback is public.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp of the feedback.
        /// </summary>
        public string CreatedAt { get; set; } = null!;
    }
}
