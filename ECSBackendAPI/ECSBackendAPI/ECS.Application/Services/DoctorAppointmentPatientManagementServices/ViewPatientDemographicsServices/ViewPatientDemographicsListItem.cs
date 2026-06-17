namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// List item representing a patient demographics entry with associated medical record.
    /// </summary>
    public class ViewPatientDemographicsListItem
    {
        /// <summary>
        /// Gets or sets the stringified unique identification key for the patient profile.
        /// </summary>
        public string Id_PatientProfile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient full name.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient gender.
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient date of birth in string format.
        /// </summary>
        public string Dob { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient identity number.
        /// </summary>
        public string? IdentityNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient address.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the patient BHYT (health insurance) number.
        /// </summary>
        public string? BhytNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient blood type.
        /// </summary>
        public string? BloodType { get; set; }

        /// <summary>
        /// Gets or sets the patient known allergies.
        /// </summary>
        public string? Allergies { get; set; }

        /// <summary>
        /// Gets or sets the patient medical history summary.
        /// </summary>
        public string? MedicalHistory { get; set; }

        /// <summary>
        /// Gets or sets the stringified unique identification key for the medical record.
        /// </summary>
        public string Id_MedicalRecord { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the record type code (e.g., MS21_TRAUMA, MS22_ANTERIOR).
        /// </summary>
        public string RecordType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display label for the record type.
        /// </summary>
        public string RecordTypeLabel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the doctor full name who created this record.
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the appointment date.
        /// </summary>
        public string AppointmentDate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chief complaint.
        /// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// Gets or sets the main diagnosis.
        /// </summary>
        public string? DiagnosisMain { get; set; }

        /// <summary>
        /// Gets or sets whether the medical record is locked.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp of the medical record.
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;
    }
}
