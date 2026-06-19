using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentServices
{
    /// <summary>
    /// Interface for confirming or rejecting appointments.
    /// </summary>
    public interface IConfirmRejectAppointmentService
    {
        /// <summary>
        /// Confirms or rejects an appointment.
        /// </summary>
        /// <param name="userId">Authenticated doctor ID.</param>
        /// <param name="appointmentId">Appointment ID.</param>
        /// <param name="request">Confirm/reject request.</param>
        /// <returns>Updated appointment response.</returns>
        Task<ApiResponse<ConfirmRejectAppointmentResponse>> Process(
            Guid userId,
            Guid appointmentId,
            ConfirmRejectAppointmentRequest request);
    }
}