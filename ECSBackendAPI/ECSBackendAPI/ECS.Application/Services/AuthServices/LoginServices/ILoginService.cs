using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.LoginServices
{
    /// <summary>
    /// Login service interface
    /// </summary>
    public interface ILoginService
    {
        /// <summary>
        /// Login process
        /// </summary>
        /// <param name="loginRequest"></param>
        /// <returns></returns>
        Task<ApiResponse<LoginResponse>> Proccess(LoginRequest loginRequest);
    }
}
