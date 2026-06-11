using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.ForgotPasswordServices
{
    /// <summary>
    /// Forgot password service interface.
    /// </summary>
    public interface IForgotPasswordService
    {
        /// <summary>
        /// Processes forgot password request by sending OTP to email.
        /// </summary>
        /// <param name="request">Forgot password request with email.</param>
        /// <returns>API response indicating success or failure.</returns>
        Task<ApiResponse<object>> Process(ForgotPasswordRequest request);
    }
}
