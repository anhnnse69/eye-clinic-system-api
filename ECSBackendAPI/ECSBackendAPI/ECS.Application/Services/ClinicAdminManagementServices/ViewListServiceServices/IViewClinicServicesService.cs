using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices
{
    /// <summary>
    /// Service contract defining workflow pipeline boundaries for retrieving listed clinic services.
    /// </summary>
    public interface IViewClinicServicesService
    {
        /// <summary>
        /// Processes internal queries to evaluate user context and load paged domain service details.
        /// </summary>
        /// <param name="request">The parameters holding search keywords and structural page configurations.</param>
        /// <returns>A standard unified envelope capturing response entries along with pagination markers.</returns>
        Task<ApiResponse<List<ViewClinicServiceResponse>>> Process(ViewClinicServicesRequest request);
    }
}