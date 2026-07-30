namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentDetailServices
{
    /// <summary>
    /// Response object containing comprehensive appointment details for the patient.
    /// </summary>
    public class GetAppointmentDetailResponse
    {
        // Basic Info
        /// <summary>
        /// Gets or sets the stringified unique identification key mapping the appointment entity.
        /// </summary>
        public string Id_appointment { get; set; } = null!;

        /// <summary>
        /// Gets or sets the serialized text identifier tracking specific state lifecycle nodes of an appointment.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Gets or sets the creation timestamp of the appointment.
        /// </summary>
        public string CreatedAt { get; set; } = null!;

        // Patient Info
        /// <summary>
        /// Gets or sets the full name of the patient.
        /// </summary>
        public string PatientName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the phone number of the patient.
        /// </summary>
        public string PatientPhone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the email of the patient.
        /// </summary>
        public string PatientEmail { get; set; } = null!;

        /// <summary>
        /// Gets or sets the date of birth of the patient.
        /// </summary>
        public string PatientDob { get; set; } = null!;

        /// <summary>
        /// Gets or sets the gender of the patient.
        /// </summary>
        public string PatientGender { get; set; } = null!;

        // Appointment Info
        /// <summary>
        /// Gets or sets the name of the clinic.
        /// </summary>
        public string ClinicName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the address of the clinic.
        /// </summary>
        public string ClinicAddress { get; set; } = null!;

        /// <summary>
        /// Gets or sets the phone number of the clinic.
        /// </summary>
        public string ClinicPhone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full name of the doctor.
        /// </summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the title of the doctor.
        /// </summary>
        public string DoctorTitle { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the formatted appointment date.
        /// </summary>
        public string AppointmentDate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the formatted time slot.
        /// </summary>
        public string TimeSlot { get; set; } = null!;

        /// <summary>
        /// Gets or sets the symptoms of the patient.
        /// </summary>
        public string? Symptoms { get; set; }

        /// <summary>
        /// Gets or sets the note/reason for the appointment.
        /// </summary>
        public string? NoteReason { get; set; }

        // Feedback (optional)
        /// <summary>
        /// Gets or sets the feedback details for the appointment.
        /// </summary>
        public FeedbackDetail? Feedback { get; set; }

        // Prescription (optional)
        /// <summary>
        /// Gets or sets the prescription details if doctor prescribed medicines for this appointment.
        /// </summary>
        public PatientPrescriptionDto? Prescription { get; set; }
    }

    /// <summary>
    /// Prescription details associated with an appointment medical record.
    /// </summary>
    public class PatientPrescriptionDto
    {
        public string DiagnosisMain { get; set; } = string.Empty;
        public string? DiagnosisComorbid { get; set; }
        public string? DoctorNotes { get; set; }
        public List<PatientPrescriptionItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Individual medicine item in a prescription.
    /// </summary>
    public class PatientPrescriptionItemDto
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string DurationDays { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Instruction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Feedback details for the appointment.
    /// </summary>
    public class FeedbackDetail
    {
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
