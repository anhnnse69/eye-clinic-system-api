using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorScheduleManagementServices.GetActiveDoctorsServices
{
    /// <summary>
    /// Defines the contract for a service that retrieves active doctor profiles within a receptionist's clinic boundary.
    /// </summary>
    public interface IGetActiveDoctorsService
    {
        /// <summary>
        /// Returns the list of active doctors belonging to the same clinic
        /// as the requesting receptionist.
        /// </summary>
        Task<ApiResponse<List<DoctorOptionResponse>>> Process(Guid receptionistUserId);
    }
}