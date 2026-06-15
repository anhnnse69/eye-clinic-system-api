namespace ECS.Application.Services.AuthServices.ViewPersonalProfileServices
{
    /// <summary>
    /// Response object containing aggregated personal profile details and sub-nested profiles.
    /// </summary>
    public class GetPersonalProfileResponse
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
        public ClinicInfoNested Clinic { get; set; } = null!;
        public DoctorProfileNested? DoctorProfile { get; set; }
    }

    /// <summary>
    /// Data transfer object for nested clinic structural tracking data.
    /// </summary>
    public class ClinicInfoNested
    {
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
    }

    /// <summary>
    /// Data transfer object for nested professional doctor specific metadata.
    /// </summary>
    public class DoctorProfileNested
    {
        public string? Title { get; set; }
        public int ExperienceYears { get; set; }
        public string? Bio { get; set; }
        public string? SpecialtyName { get; set; }
    }
}