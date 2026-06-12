namespace ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices
{
    /// <summary>
    /// Clinic item returned in search results.
    /// </summary>
    public class ClinicSearchItem
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
    }
}
