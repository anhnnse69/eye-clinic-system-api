using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.EditDoctorScheduleServices
{
    /// <summary>
    /// Defines operations for editing doctor schedules.
    /// </summary>
    public interface IEditDoctorScheduleService
    {
        /// <summary>
        /// Updates a doctor's schedule.
        /// </summary>
        /// <param name="userId">Doctor user identifier.</param>
        /// <param name="scheduleId">Schedule identifier.</param>
        /// <param name="request">Updated schedule information.</param>
        /// <returns>The updated schedule.</returns>
        Task<ApiResponse<EditDoctorScheduleResponse>> Process(
            Guid userId,
            Guid scheduleId,
            EditDoctorScheduleRequest request);
    }
}
