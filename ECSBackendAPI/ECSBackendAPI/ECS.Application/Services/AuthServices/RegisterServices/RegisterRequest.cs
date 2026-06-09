namespace ECS.Application.Services.AuthServices.RegisterServices
{
    /// <summary>
    /// Request model for user self-registration.
    /// All fields are required. Email must be a valid format.
    /// Password must be at least 6 characters.
    /// ConfirmPassword must match Password (validated in service layer).
    /// </summary>
    public class RegisterRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}