using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorScheduleManagementServices.ViewDoctorPersonalScheduleServices
{
    /// <summary>
    /// Service interface for retrieving a doctor's personal schedule.
    /// </summary>
    public interface IViewDoctorPersonalScheduleService
    {
        /// <summary>
        /// Retrieves the personal schedule of a doctor for a specific date,
        /// optionally filtered by shift type.
        /// </summary>
        /// <param name="userId">The ID of the doctor (user) whose schedule is being retrieved.</param>
        /// <param name="request">The request containing work date and optional shift filter.</param>
        /// <returns>
        /// An <see cref="ApiResponse{T}"/> containing the doctor's schedule response data.
        /// </returns>
        Task<ApiResponse<ViewDoctorPersonalScheduleResponse>> Process(
            Guid userId,
            ViewDoctorPersonalScheduleRequest request);
    }
}
