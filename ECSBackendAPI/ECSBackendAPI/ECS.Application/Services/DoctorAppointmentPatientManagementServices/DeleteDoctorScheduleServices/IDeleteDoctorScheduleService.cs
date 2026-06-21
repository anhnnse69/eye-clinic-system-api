using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.DeleteDoctorScheduleServices
{
    /// <summary>
    /// Defines operations for deleting a doctor's personal schedule.
    /// </summary
    public interface IDeleteDoctorScheduleService
    {
        /// <summary>
        /// Deletes a doctor's schedule by its identifier.
        /// </summary>
        /// <param name="userId">
        /// The identifier of the authenticated user performing the deletion.
        /// </param>
        /// <param name="scheduleId">
        /// The identifier of the schedule to be deleted.
        /// </param>
        /// <returns>
        /// An <see cref="ApiResponse{T}"/> containing details of the deleted schedule.
        /// </returns>
        Task<ApiResponse<DeleteDoctorScheduleResponse>> Process(
            Guid userId,
            Guid scheduleId);
    }
}
