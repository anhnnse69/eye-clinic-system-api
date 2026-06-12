using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.ForgotPasswordServices
{
    /// <summary>
    /// Interface for forgot password service.
    /// </summary>
    public interface IForgotPasswordService
    {
        /// <summary>
        /// Processes the forgot password request by sending OTP to user's email.
        /// </summary>
        /// <param name="request">Forgot password request containing email.</param>
        /// <returns>An <see cref="ApiResponse{ForgotPasswordResponse}"/> containing reset token.</returns>
        Task<ApiResponse<ForgotPasswordResponse>> Process(ForgotPasswordRequest request);
    }
}
