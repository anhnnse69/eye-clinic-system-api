using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorScheduleManagementServices.ClinicShiftServices
{
    /// <summary>
    /// Service interface for retrieving clinic shift ranges.
    /// </summary>
    public interface IClinicShiftService
    {
        /// <summary>
        /// Processes the request to get shift ranges for a receptionist.
        /// </summary>
        /// <param name="receptionistUserId">The ID of the receptionist user.</param>
        /// <returns>API response containing list of shift range items.</returns>
        Task<ApiResponse<List<ShiftRangeItem>>> Process(Guid receptionistUserId);
    }
}