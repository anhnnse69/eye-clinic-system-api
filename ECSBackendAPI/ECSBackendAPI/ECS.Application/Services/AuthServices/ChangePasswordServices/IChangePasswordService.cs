using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.ChangePasswordServices
{
    /// <summary>
    /// Change password service interface
    /// </summary>
    public interface IChangePasswordService
    {
        /// <summary>
        /// Change password process
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="changePasswordRequest"></param>
        /// <returns></returns>
        Task<ApiResponse<ChangePasswordResponse>> Process(Guid userId, ChangePasswordRequest changePasswordRequest);
    }
}
