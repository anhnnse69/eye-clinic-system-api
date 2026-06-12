namespace ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices
{
    /// <summary>
    /// Response representation object containing clinic profile specific properties.
    /// </summary>
    public class GetClinicDetailsResponse
    {
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public decimal RatingAvg { get; set; }
        public int ReviewCount { get; set; }
    }
}
