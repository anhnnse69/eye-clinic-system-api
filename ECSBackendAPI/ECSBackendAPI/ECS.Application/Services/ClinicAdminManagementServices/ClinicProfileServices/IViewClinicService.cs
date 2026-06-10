using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices
{
    /// <summary>
    /// Service contract defining operations for viewing clinic profile data.
    /// </summary>
    public interface IViewClinicService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve data profile definitions.
        /// </summary>
        /// <param name="request">The view clinic data request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<ViewClinicResponse>> Process(ViewClinicRequest request);
    }
}