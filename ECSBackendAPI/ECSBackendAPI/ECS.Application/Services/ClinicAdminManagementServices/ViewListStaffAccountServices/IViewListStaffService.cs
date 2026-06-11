using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices
{
    /// <summary>
    /// Service contract defining operations for viewing clinic staff account lists.
    /// </summary>
    public interface IViewListStaffService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve staff account profiles.
        /// </summary>
        /// <param name="request">The view staff data request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<List<StaffAccountResponse>>> Process(ViewListStaffRequest request);
    }
}