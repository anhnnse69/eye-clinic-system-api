using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.ResetPasswordServices
{
    /// <summary>
    /// Reset password service interface.
    /// </summary>
    public interface IResetPasswordService
    {
        /// <summary>
        /// Processes reset password request with OTP verification.
        /// </summary>
        /// <param name="request">Reset password request with email, OTP, and new password.</param>
        /// <returns>API response indicating success or failure.</returns>
        Task<ApiResponse<object>> Process(ResetPasswordRequest request);
    }
}
