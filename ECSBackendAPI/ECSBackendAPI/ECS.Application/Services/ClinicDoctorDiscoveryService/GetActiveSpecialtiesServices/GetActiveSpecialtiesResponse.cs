namespace ECS.Application.Services.ClinicDoctorDiscoveryService.GetActiveSpecialtiesServices
{
    /// <summary>
    /// Response model returning data summary post execution.
    /// </summary>
    public class GetActiveSpecialtiesResponse
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
