namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices
{
    /// <summary>
    /// Represents a feedback item belonging to a clinic.
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