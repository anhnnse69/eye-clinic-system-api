using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentsServices;

namespace ECS.Application.Services.ReceptionistAppointmentManagementServices.ConfirmRejectAppointmentsServices
{
    /// <summary>
    /// Defines the contract for a receptionist confirming or rejecting
    /// an appointment on behalf of any doctor within their own clinic.
    /// Reuses the same request/response contract as the doctor-facing
    /// decision endpoint since the decision semantics are identical.
    /// </summary>
    public interface IReceptionistConfirmRejectAppointmentService
    {
        /// <summary>
        /// Confirms or rejects an appointment belonging to a doctor
        /// within the receptionist's clinic.
        /// </summary>
        Task<ApiResponse<ConfirmRejectAppointmentResponse>> Process(
            Guid receptionistUserId,
            Guid appointmentId,
            ConfirmRejectAppointmentRequest request);
    }
}