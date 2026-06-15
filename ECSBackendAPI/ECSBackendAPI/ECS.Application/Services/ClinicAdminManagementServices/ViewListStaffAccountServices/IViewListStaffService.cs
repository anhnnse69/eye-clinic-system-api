using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices
{
    /// <summary>
    /// Service contract defining operations for viewing paginated, filtered, and searched staff profiles.
    /// </summary>
    public interface IViewListStaffService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve data profile definitions.
        /// </summary>
        /// <param name="request">The search filter criteria request parameter attributes.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<List<StaffAccountResponse>>> Process(ViewListStaffRequest request);
    }
}