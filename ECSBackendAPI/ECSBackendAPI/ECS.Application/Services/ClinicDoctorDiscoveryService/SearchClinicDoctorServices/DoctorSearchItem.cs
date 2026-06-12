namespace ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices
{
    /// <summary>
    /// Represents a doctor search result item.
    /// </summary>
    public class DoctorSearchItem
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Title { get; set; }
        public string? Specialty { get; set; }
        public string? ClinicName { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Bio { get; set; }
        public decimal? RatingAvg { get; set; }
        public int ReviewCount { get; set; }
    }
}
