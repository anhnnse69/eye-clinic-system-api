using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices
{
    /// <summary>
    /// Interface for the service orchestrating daily appointments retrieval, live tracking telemetry computations, and strict clinic boundary isolation.
    /// </summary>
    public interface IReceptionistGetDailyAppointmentsService
    {
        /// <summary>
        /// Processes, secures, and extracts paginated data rows and status counters matching requested structural frameworks.
        /// </summary>
        /// <param name="request">The search filter packages and receptionist context arguments mapped from the token layer.</param>
        /// <returns>An <see cref="ApiResponse{List{ReceptionistGetDailyAppointmentsResponse}}"/> wrapping final data arrays and tracking structures.</returns>
        Task<ApiResponse<List<ReceptionistGetDailyAppointmentsResponse>>> Process(ReceptionistGetDailyAppointmentsRequest request);
    }
}