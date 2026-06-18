using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Represents detailed patient information
    /// and appointment history.
    /// </summary>
    public class ViewPatientDetailResponse
    {
        public Guid PatientId { get; set; }
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
        public string? AvatarUrl { get; set; }
        public List<AppointmentHistoryItem> Appointments { get; set; } = [];
    }
}
