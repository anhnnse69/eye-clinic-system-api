using ECS.Domain.Enums;

namespace ECS.Application.Services.PatientProfileManagementServices.ViewPatientProfileDetailServices
{
    /// <summary>
    /// Response object representing complete patient profile details available for viewing purposes.
    /// </summary>
    public class ViewPatientProfileDetailResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier associated with the patient profile record.
        /// </summary>
        public Guid PatientProfileId { get; set; }

        /// <summary>
        /// Gets or sets the full legal name belonging to the patient profile.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the gender classification assigned to the patient profile.
        /// </summary>
        public Gender Gender { get; set; }

        /// <summary>
        /// Gets or sets the birth date associated with the patient profile.
        /// </summary>
        public DateTime Dob { get; set; }

        /// <summary>
        /// Gets or sets the national identification number linked to the patient profile.
        /// </summary>
        public string? IdentityNumber { get; set; }

        /// <summary>
        /// Gets or sets the current residential address information.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the contact phone number belonging to the patient profile.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the health insurance card reference number.
        /// </summary>
        public string? BhytNumber { get; set; }

        /// <summary>
        /// Gets or sets the blood group classification information.
        /// </summary>
        public string? BloodType { get; set; }

        /// <summary>
        /// Gets or sets known allergy information associated with the patient.
        /// </summary>
        public string? Allergies { get; set; }

        /// <summary>
        /// Gets or sets historical medical condition records and disease information.
        /// </summary>
        public string? MedicalHistory { get; set; }

        /// <summary>
        /// Gets or sets the relationship descriptor between the authenticated user and patient profile.
        /// </summary>
        public string Relationship { get; set; } = "Bản thân";

        /// <summary>
        /// Gets or sets the profile creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the profile last modification timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
