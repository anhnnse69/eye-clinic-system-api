using System.Text;
using ECS.Application.Common.OTP;

namespace ECS.Application.Services.AuthServices.ForgotPasswordServices
{
    /// <summary>
    /// Response object for forgot password containing reset token.
    /// </summary>
    public class ForgotPasswordResponse
    {
        public string ResetToken { get; set; } = string.Empty;
    }
}
