using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices
{
    /// <summary>
    /// Service contract defining operations for viewing and filtering clinic feedback lists.
    /// </summary>
    public interface IGetClinicFeedbacksService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve and filter patient feedback records.
        /// </summary>
        /// <param name="request">The view feedback request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<List<GetClinicFeedbackResponse>>> Process(
            GetClinicFeedbacksRequest request);
    }
}