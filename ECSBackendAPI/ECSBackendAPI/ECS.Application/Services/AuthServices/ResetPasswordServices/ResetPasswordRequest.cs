namespace ECS.Application.Services.AuthServices.ResetPasswordServices
{
    /// <summary>
    /// Request object for reset password.
    /// </summary>
    public class ResetPasswordRequest
    {
        public string ResetToken { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
