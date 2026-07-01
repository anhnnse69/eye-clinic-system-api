using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorScheduleManagementServices.GetActiveRoomsServices
{
    /// <summary>
    /// Defines the contract for a service that retrieves active facility rooms within a receptionist's clinic boundary.
    /// </summary>
    public interface IGetActiveRoomsService
    {
        /// <summary>
        /// Returns the list of active facility rooms belonging to the
        /// same clinic as the requesting receptionist.
        /// </summary>
        Task<ApiResponse<List<ClinicRoomResponse>>> Process(Guid receptionistUserId);
    }
}
