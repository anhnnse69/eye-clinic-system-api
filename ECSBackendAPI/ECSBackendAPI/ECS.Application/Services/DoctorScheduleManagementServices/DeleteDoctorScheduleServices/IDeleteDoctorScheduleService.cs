using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorScheduleManagementServices.DeleteDoctorScheduleServices
{
    /// <summary>
    /// Defines operations for deleting a doctor's personal schedule.
    /// </summary
    public interface IDeleteDoctorScheduleService
    {
        /// <summary>
        /// Soft-deletes a doctor's schedule.
        /// </summary>
        /// <param name="receptionistUserId">
        /// Identifier of the receptionist performing the deletion.
        /// </param>
        /// <param name="doctorId">
        /// Identifier of the DoctorProfile that owns the schedule.
        /// </param>
        /// <param name="scheduleId">
        /// Identifier of the schedule to delete.
        /// </param>
        Task<ApiResponse<DeleteDoctorScheduleResponse>> Process(
            Guid receptionistUserId,
            Guid doctorId,
            Guid scheduleId);
    }
}
