namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices
{
    /// <summary>
    /// Response object containing detailed profile info for a clinic.
    /// </summary>
    public class ViewClinicResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public decimal? RatingAvg { get; set; }
        public int? ReviewCount { get; set; }
    }
}