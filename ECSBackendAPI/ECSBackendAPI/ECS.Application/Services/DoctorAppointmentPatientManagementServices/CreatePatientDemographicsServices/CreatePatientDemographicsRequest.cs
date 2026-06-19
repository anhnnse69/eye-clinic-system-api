namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Request DTO for creating patient medical demographics by doctor.
    /// Based on UC36 - Create Patient Demographics
    /// This endpoint handles ONLY medical/ophthalmology information that doctors enter.
    /// Administrative info (DOB, Gender, Address, etc.) are handled separately by Patient/Receptionist.
    /// </summary>
    public class CreatePatientDemographicsRequest
    {
        // Required: Patient to create medical demographics for
        public string PatientProfileId { get; set; } = null!;
        
        // === Medical Background Section (from UC36) ===
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public string? LifestyleFactors { get; set; }
        
        // === Ophthalmology-specific fields (from UC36) ===
        public string? CurrentEyeMedications { get; set; }
        public string? PreviousEyeSurgery { get; set; }
        public string? EyeVisionHistory { get; set; }
    }
}
