namespace ECS.Application.Services.AuthServices.ViewAccountInfoServices
{
    /// <summary>
    /// Response object containing account information.
    /// </summary>
    public class ViewAccountInfoResponse
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
