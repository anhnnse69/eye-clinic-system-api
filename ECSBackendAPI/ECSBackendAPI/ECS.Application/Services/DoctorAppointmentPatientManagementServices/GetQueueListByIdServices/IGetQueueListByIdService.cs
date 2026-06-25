using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetQueueListByIdServices
{
    /// <summary>
    /// Interface for getting queue list by doctor ID.
    /// </summary>
    public interface IGetQueueListByIdService
    {
        /// <summary>
        /// Gets the queue list for a specific doctor on a given date.
        /// </summary>
        /// <param name="doctorId">The doctor profile ID.</param>
        /// <param name="date">The date to get queue list.</param>
        /// <returns>API response with queue list data.</returns>
        Task<ApiResponse<GetQueueListByIdResponse>> Process(Guid doctorId, DateOnly date);
    }
}
