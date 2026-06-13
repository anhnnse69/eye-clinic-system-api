namespace ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices
{
    /// <summary>
    /// Request object for updating an existing clinic's details.
    /// </summary>
    public class UpdateClinicRequest
    {
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
    }
}