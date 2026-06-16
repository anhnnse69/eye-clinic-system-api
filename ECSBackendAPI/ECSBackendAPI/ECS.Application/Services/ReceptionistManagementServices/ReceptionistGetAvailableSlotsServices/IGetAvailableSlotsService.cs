using ECS.Application.Common.Response;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices
{
    /// <summary>
    /// Interface for the service handling available time slots and schedule matrices retrieval.
    /// </summary>
    public interface IGetAvailableSlotsService
    {
        /// <summary>
        /// Processes the available slots request to compile doctor schedule matrices.
        /// </summary>
        /// <param name="request">The scheduler request filters and receptionist context.</param>
        /// <returns>An <see cref="ApiResponse{List{GetAvailableSlotsResponse}}"/> wrapping the list of matching schedules.</returns>
        Task<ApiResponse<List<GetAvailableSlotsResponse>>> Process(GetAvailableSlotsRequest request);
    }
}
