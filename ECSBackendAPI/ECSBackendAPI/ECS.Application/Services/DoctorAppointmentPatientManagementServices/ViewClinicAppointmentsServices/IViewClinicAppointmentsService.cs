using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewClinicAppointmentsServices
{
    /// <summary>
    /// Defines the contract for viewing all appointments
    /// (across every doctor) within a receptionist's clinic.
    /// </summary>
    public interface IViewClinicAppointmentsService
    {
        /// <summary>
        /// Returns the clinic's appointments across all doctors,
        /// with filtering and pagination support.
        /// </summary>
        Task<ApiResponse<ViewClinicAppointmentsResponse>> Process(
            Guid receptionistUserId,
            ViewClinicAppointmentsRequest request);
    }
}