using ECS.Domain.Enums;

namespace ECS.Application.Services.PatientProfileManagementServices.CreatePatientProfileServices
{
    /// <summary>
    /// Request data transfer object for creating a new patient profile.
    /// </summary>
    public class CreatePatientProfileRequest
    {
        public string FullName { get; set; } = null!;
        public Gender Gender { get; set; }
        public DateTime Dob { get; set; }
        public string? IdentityNumber { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BhytNumber { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
        public string Relationship { get; set; } = "Bản thân";
    }
}
