using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorScheduleManagementServices.BatchCreateDoctorScheduleServices;

namespace ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService
{
    /// <summary>
    /// Defines operations for creating doctor schedules in batch.
    /// </summary>
    public interface IBatchCreateDoctorScheduleService
    {
        /// <summary>
        /// Creates doctor schedules in batch.
        /// </summary>
        /// <param name="receptionistUserId">The receptionist user ID.</param>
        /// <param name="request">The batch schedule creation request.</param>
        /// <returns>The batch schedule creation result.</returns>
        Task<ApiResponse<BatchCreateDoctorScheduleResponse>> Process(
            Guid receptionistUserId,
            BatchCreateDoctorScheduleRequest request);
    }
}