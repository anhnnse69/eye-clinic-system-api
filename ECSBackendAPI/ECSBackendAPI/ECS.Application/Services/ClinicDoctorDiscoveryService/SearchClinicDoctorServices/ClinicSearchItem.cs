namespace ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices
{
    /// <summary>
    /// Represents a clinic search result item.
    /// </summary>
    public class ClinicSearchItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public decimal? RatingAvg { get; set; }
        public int ReviewCount { get; set; }
    }
}
