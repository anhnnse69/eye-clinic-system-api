namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices
{
    /// <summary>
    /// Represents a doctor displayed in the clinic profile.
    /// </summary>
    public class ClinicDoctorItem
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? Title { get; set; }
        public string? Specialty { get; set; }
        public int ExperienceYears { get; set; }
        public decimal? RatingAvg { get; set; }
        public int? ReviewCount { get; set; }
    }
}
