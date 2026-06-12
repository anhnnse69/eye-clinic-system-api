namespace ECS.Application.Services.AuthServices.ForgotPasswordServices
{
    /// <summary>
    /// Request object for forgot password.
    /// </summary>
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
