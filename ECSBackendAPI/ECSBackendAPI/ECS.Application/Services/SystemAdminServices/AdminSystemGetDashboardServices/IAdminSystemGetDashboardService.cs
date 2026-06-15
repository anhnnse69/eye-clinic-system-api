using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices
{
    /// <summary>
    /// Interface for the system administrator dashboard service.
    /// </summary>
    public interface IAdminSystemGetDashboardService
    {
        /// <summary>
        /// Processes the dashboard request to compile system metrics.
        /// </summary>
        /// <param name="request">The dashboard filters including clinic ID and date intervals.</param>
        /// <returns>An <see cref="ApiResponse{AdminSystemGetDashboardResponse}"/> containing compiled dashboard data.</returns>
        Task<ApiResponse<AdminSystemGetDashboardResponse>> Process(AdminSystemGetDashboardRequest request);
    }
}