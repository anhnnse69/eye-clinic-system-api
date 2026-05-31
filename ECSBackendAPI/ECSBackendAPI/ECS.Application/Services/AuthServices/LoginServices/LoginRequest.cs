namespace ECS.Application.Services.AuthServices.LoginServices
{
    /// <summary>
    /// Request object for user login.
    /// </summary>
    public class LoginRequest
    {
        public string EmailAddress { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}