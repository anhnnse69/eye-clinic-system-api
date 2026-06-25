using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetMyQueueListServices
{
    /// <summary>
    /// Interface for getting current doctor's queue list from JWT token.
    /// </summary>
    public interface IGetMyQueueListService
    {
        /// <summary>
        /// Gets the queue list for the current authenticated doctor.
        /// Automatically resolves doctor profile from JWT token.
        /// </summary>
        /// <param name="date">The date to get queue list.</param>
        /// <returns>API response with queue list data.</returns>
        Task<ApiResponse<GetMyQueueListResponse>> Process(DateOnly date);
    }
}
