using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices
{
    /// <summary>
    /// Interface for the service managing reception counter walk-in ticket creation pipelines.
    /// </summary>
    public interface IReceptionistCreateWalkinAppointmentService
    {
        /// <summary>
        /// Processes the receptionist request payload to register on-site appointments.
        /// </summary>
        /// <param name="request">The request parameter configurations gathered from the counter form context.</param>
        /// <returns>An <see cref="ApiResponse{ReceptionistCreateWalkinAppointmentResponse}"/> envelope containing finalized queue rows.</returns>
        Task<ApiResponse<ReceptionistCreateWalkinAppointmentResponse>> Process(ReceptionistCreateWalkinAppointmentRequest request);
    }
}