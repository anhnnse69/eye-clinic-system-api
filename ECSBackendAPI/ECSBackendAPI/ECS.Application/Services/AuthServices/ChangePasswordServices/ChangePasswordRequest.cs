namespace ECS.Application.Services.AuthServices.ChangePasswordServices
{
    /// <summary>
    /// Request object for changing the user's password.
    /// </summary>
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
