using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices
{
    /// <summary>
    /// Interface for CompleteQueueService.
    /// </summary>
    public interface ICompleteQueueService
    {
        /// <summary>
        /// Processes the request to mark a queue item as completed.
        /// </summary>
        /// <param name="request">The complete queue request.</param>
        /// <returns>An ApiResponse containing the completion result.</returns>
        Task<ApiResponse<CompleteQueueResponse>> Process(CompleteQueueRequest request);
    }
}
