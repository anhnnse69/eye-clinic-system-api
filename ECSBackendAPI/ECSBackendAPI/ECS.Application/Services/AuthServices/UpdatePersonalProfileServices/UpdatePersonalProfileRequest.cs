namespace ECS.Application.Services.AuthServices.UpdatePersonalProfileServices
{
    /// <summary>
    /// Request object for updating an existing user's profile details.
    /// </summary>
    public class UpdatePersonalProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Title { get; set; }
        public int ExperienceYears { get; set; }
        public string? Bio { get; set; }
        public Guid? SpecialtyId { get; set; }
    }
}