namespace ECS.Application.Services.DoctorScheduleManagementServices.GetActiveDoctorsServices
{
    /// <summary>
    /// Represents a simplified doctor profile data structure intended for selection lists or option dropdowns.
    /// </summary>
    public class DoctorOptionResponse
    {
        public Guid DoctorId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Specialty { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
    }
}
