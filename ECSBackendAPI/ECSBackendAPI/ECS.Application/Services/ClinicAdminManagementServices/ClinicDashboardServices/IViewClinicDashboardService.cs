using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicDashboardServices;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices
{
    /// <summary>
    /// Service contract defining operations for retrieving clinic dashboard data.
    /// </summary>
    public interface IViewClinicDashboardService
    {
        /// <summary>
        /// Executes the dashboard retrieval workflow process.
        /// </summary>
        /// <param name="request">
        /// The dashboard request criteria parameters.
        /// </param>
        /// <returns>
        /// A standardized API response containing clinic dashboard statistics.
        /// </returns>
        Task<ApiResponse<ViewClinicDashboardResponse>>
            Process(ViewClinicDashboardRequest request);
    }
}