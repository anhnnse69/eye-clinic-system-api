using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService
{
    /// <summary>
    /// Defines operations for creating doctor schedules.
    /// </summary>
    public interface ICreateDoctorScheduleService
    {
        /// <summary>
        /// Creates schedules for the specified doctor across one or more
        /// work dates and shifts.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the doctor user creating schedules.
        /// </param>
        /// <param name="request">
        /// Schedule creation request containing work dates,
        /// shifts, and room information.
        /// </param>
        /// <returns>
        /// A response containing created schedules and skipped schedules.
        /// </returns>
        Task<ApiResponse<CreateDoctorScheduleResponse>> Process(
            Guid userId,
            CreateDoctorScheduleRequest request);
    }
}
