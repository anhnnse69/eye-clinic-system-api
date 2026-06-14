namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices
{
    /// <summary>
    /// Represents a clinic profile response.
    /// </summary>
    public class ViewClinicProfileResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public decimal? RatingAvg { get; set; }
        public int? ReviewCount { get; set; }
        public List<ClinicDoctorItem> Doctors { get; set; } = [];
        public List<ClinicServiceItem> Services { get; set; } = [];
        public List<ClinicFeedbackItem> Feedbacks { get; set; } = [];
    }
}
