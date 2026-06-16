namespace ECS.Application.Services.PatientMedicalRecordsServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// Response object representing patient demographics details.
    /// </summary>
    public class ViewPatientDemographicsResponse
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
        /// Gets or sets the total count of medical records for this patient.
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Gets or sets the collection of medical record summaries.
        /// </summary>
        public List<MedicalRecordSummaryItem> Records { get; set; } = new();
    }
}
