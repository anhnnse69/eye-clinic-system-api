namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Response DTO confirming patient medical demographics creation.
    /// Based on UC36 - Create Patient Demographics
    /// Contains both medical/ophthalmology AND updated administrative info.
    /// </summary>
    public class CreatePatientDemographicsResponse
    {
        public Guid PatientProfileId { get; set; }
        public string? PatientName { get; set; }
        
        // === Administrative Info (updated by doctor) ===
        public string? FullName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdentityNumber { get; set; }
        public string? BhytNumber { get; set; }
        public string? Address { get; set; }
        
        // === Medical Background Section ===
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public string? LifestyleFactors { get; set; }
        
        // === Ophthalmology-specific fields ===
        public string? CurrentEyeMedications { get; set; }
        public string? PreviousEyeSurgery { get; set; }
        public string? EyeVisionHistory { get; set; }
        
        public string CreatedAt { get; set; } = null!;
        public bool IsSuccess { get; set; }
    }
}
