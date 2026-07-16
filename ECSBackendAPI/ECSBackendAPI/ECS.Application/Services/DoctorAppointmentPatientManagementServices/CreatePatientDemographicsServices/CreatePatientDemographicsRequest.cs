namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Request DTO for creating/editing patient medical demographics by doctor.
    /// Based on UC36 - Create Patient Demographics
    /// This endpoint handles both medical/ophthalmology AND editable administrative info.
    /// Administrative info (DOB, Gender, Address, etc.) can be pre-filled from PatientProfile
    /// and optionally edited by the doctor.
    /// </summary>
    public class CreatePatientDemographicsRequest
    {
        // Required: Patient to create medical demographics for
        public string PatientProfileId { get; set; } = null!;
        
        // === Administrative Info Section (Editable by Doctor) ===
        // These fields are pre-filled from PatientProfile but can be edited by doctor
        public string? FullName { get; set; }
        public string? DateOfBirth { get; set; }  // Format: yyyy-MM-dd
        public string? Gender { get; set; }  // MALE, FEMALE
        public string? PhoneNumber { get; set; }
        public string? IdentityNumber { get; set; }
        public string? BhytNumber { get; set; }
        public string? Address { get; set; }
        
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
