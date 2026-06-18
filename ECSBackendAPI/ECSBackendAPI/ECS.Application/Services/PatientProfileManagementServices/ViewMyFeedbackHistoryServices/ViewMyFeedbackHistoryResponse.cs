namespace ECS.Application.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices
{
    /// <summary>
    /// Represents feedback information visible to the authenticated patient.
    /// </summary>
    public class ViewMyFeedbackHistoryResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the feedback record.
        /// </summary>
        public string FeedbackId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unique identifier of the appointment associated with the feedback.
        /// </summary>
        public string AppointmentId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full name of the patient who submitted the feedback.
        /// </summary>
        public string PatientName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full name of the doctor evaluated in the feedback.
        /// </summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the clinic evaluated in the feedback.
        /// </summary>
        public string ClinicName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the rating score assigned to the doctor.
        /// </summary>
        public int RatingDoctor { get; set; }

        /// <summary>
        /// Gets or sets the rating score assigned to the clinic.
        /// </summary>
        public int RatingClinic { get; set; }

        /// <summary>
        /// Gets or sets the optional feedback comment provided by the patient.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the feedback is publicly visible.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the appointment date associated with the feedback.
        /// </summary>
        public string AppointmentDate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the date and time when the feedback was created.
        /// </summary>
        public string CreatedAt { get; set; } = null!;
    }
}