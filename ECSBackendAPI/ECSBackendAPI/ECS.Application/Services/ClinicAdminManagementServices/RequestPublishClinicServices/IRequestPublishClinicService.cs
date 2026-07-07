using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.RequestPublishClinicServices
{
    /// <summary>
    /// Service contract defining operations for requesting clinic publication.
    /// </summary>
    public interface IRequestPublishClinicService
    {
        /// <summary>
        /// Executes the application workflow process to request clinic publication.
        /// </summary>
        /// <param name="request">
        /// The request payload containing the clinic identifier.
        /// </param>
        /// <returns>
        /// A unified standard envelope containing execution status information.
        /// </returns>
        Task<ApiResponse<bool>> Process(RequestPublishClinicRequest request);
    }
}
