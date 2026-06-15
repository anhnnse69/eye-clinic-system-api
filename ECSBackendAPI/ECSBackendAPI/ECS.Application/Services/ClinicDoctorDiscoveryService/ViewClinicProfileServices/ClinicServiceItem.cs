namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices
{
    /// <summary>
    /// Represents a service displayed in the clinic profile.
    /// </summary>
    public class ClinicServiceItem
    {
        public Guid Id { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal? Price { get; set; }
        public int DurationMinutes { get; set; }
    }
}
