namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices
{
    /// <summary>
    /// Represents a feedback displayed in the clinic profile.
    /// </summary>
    public class ClinicFeedbackItem
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = null!;
        public int RatingDoctor { get; set; }
        public int RatingClinic { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
