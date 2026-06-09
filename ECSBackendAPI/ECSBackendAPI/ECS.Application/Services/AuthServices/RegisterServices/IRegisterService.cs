using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.RegisterServices
{
    /// <summary>
    /// Defines the contract for the user registration service.
    /// </summary>
    public interface IRegisterService
    {
        /// <summary>
        /// Processes a user registration request.
        /// </summary>
        /// <param name="request">The registration data submitted by the user.</param>
        /// <returns>A response indicating whether the registration was successful.</returns>
        Task<ApiResponse<bool>> Process(RegisterRequest request);
    }
}