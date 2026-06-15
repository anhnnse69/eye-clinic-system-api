using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices
{
    /// <summary>
    /// Defines the contract for viewing paginated clinic feedbacks.
    /// </summary>
    public interface IViewClinicFeedbacksService
    {
        /// <summary>
        /// Returns the paginated feedbacks and rating summary
        /// of a clinic by its ID.
        /// </summary>
        Task<ApiResponse<ViewClinicFeedbacksResponse>> Process(
            Guid clinicId,
            ViewClinicFeedbacksRequest request);
    }
}