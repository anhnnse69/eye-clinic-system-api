using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorClinicRoomsServices
{
    /// <summary>
    /// Defines operations for retrieving clinic rooms
    /// available to the authenticated doctor.
    /// </summary>
    public interface IViewDoctorClinicRoomsService
    {
        /// <summary>
        /// Retrieves all active rooms belonging to the doctor's clinic.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <returns>
        /// A response containing the list of active clinic rooms.
        /// </returns>
        Task<ApiResponse<List<ClinicRoomResponse>>> Process(Guid userId);
    }
}
