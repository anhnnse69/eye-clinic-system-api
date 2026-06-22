using ECS.Application.Common.Response;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentHistoryServices
{
    /// <summary>
    /// Service contract defining operations for viewing cross-clinic appointment histories associated with the logged-in user.
    /// </summary>
    public interface IGetAppointmentHistoryService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve, filter, and map historical clinic appointments bound to the active session.
        /// </summary>
        /// <param name="request">The view appointments history request criteria parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<List<GetAppointmentHistoryResponse>>> Process(GetAppointmentHistoryRequest request);
    }
}
