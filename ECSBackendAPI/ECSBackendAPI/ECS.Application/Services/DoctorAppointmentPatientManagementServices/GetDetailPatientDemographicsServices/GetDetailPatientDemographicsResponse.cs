namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices
{
    /// <summary>
    /// Response object containing detailed patient demographics information.
    /// </summary>
    public class GetDetailPatientDemographicsResponse
    {
        /// <summary>
        /// Gets or sets the patient full name.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient date of birth in string format (yyyy-MM-dd).
        /// </summary>
        public string Dob { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient gender.
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient identity number (CCCD/CMND).
        /// </summary>
        public string? IdentityNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient health insurance code (BHYT).
        /// </summary>
        public string? BhytNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient address.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the patient blood type.
        /// </summary>
        public string? BloodType { get; set; }

        /// <summary>
        /// Gets or sets the patient known allergies.
        /// </summary>
        public string? Allergies { get; set; }

        /// <summary>
        /// Gets or sets the patient medical history.
        /// </summary>
        public string? MedicalHistory { get; set; }
    }
}
