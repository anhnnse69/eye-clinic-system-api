using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCancelAppointmentsServices
{
    /// <summary>
    /// Interface for the service orchestrating receptionist-driven appointment cancellation pipelines.
    /// </summary>
    public interface IReceptionistCancelAppointmentsService
    {
        /// <summary>
        /// Processes the cancellation flow for a specific medical appointment tracking registration.
        /// </summary>
        /// <param name="request">The cancellation parameters and operational justification.</param>
        /// <returns>An <see cref="ApiResponse{ReceptionistCancelAppointmentsResponse}"/> wrapping the operation execution outcome.</returns>
        Task<ApiResponse<ReceptionistCancelAppointmentsResponse>> Process(ReceptionistCancelAppointmentsRequest request);
    }
}
