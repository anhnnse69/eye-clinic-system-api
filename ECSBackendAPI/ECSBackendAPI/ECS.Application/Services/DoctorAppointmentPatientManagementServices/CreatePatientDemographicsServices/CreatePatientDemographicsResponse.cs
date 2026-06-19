namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Response DTO confirming patient medical demographics creation.
    /// Based on UC36 - Create Patient Demographics
    /// Contains only medical/ophthalmology information entered by the doctor.
    /// </summary>
    public class CreatePatientDemographicsResponse
    {
        public Guid PatientProfileId { get; set; }
        public string? PatientName { get; set; }
        
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
