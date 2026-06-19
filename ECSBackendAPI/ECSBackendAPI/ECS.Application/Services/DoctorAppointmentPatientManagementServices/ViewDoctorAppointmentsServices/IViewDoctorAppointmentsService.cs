using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorAppointmentsServices
{
    /// <summary>
    /// Defines the contract for viewing
    /// a doctor's appointment list.
    /// </summary>
    public interface IViewDoctorAppointmentsService
    {
        /// <summary>
        /// Returns the doctor's appointments
        /// with filtering and pagination support.
        /// </summary>
        Task<ApiResponse<ViewDoctorAppointmentsResponse>> Process(
            Guid userId,
            ViewDoctorAppointmentsRequest request);
    }
}
