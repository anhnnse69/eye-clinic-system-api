using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.ViewAccountInfoServices
{
    /// <summary>
    /// View account info service interface
    /// </summary>
    public interface IViewAccountInfoService
    {
        /// <summary>
        /// View account info process
        /// </summary>
        /// <param name="viewAccountInfoRequest"></param>
        /// <returns></returns>
        Task<ApiResponse<ViewAccountInfoResponse>> Process(ViewAccountInfoRequest viewAccountInfoRequest);
    }
}
