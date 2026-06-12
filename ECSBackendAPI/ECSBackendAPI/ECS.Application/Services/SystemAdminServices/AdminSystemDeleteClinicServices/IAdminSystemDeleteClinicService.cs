using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices
{
    /// <summary>
    /// Toggle clinic status service interface.
    /// </summary>
    public interface IAdminSystemDeleteClinicService
    {
        /// <summary>
        /// Processes the clinic status toggle (soft-delete) logic.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <returns>An <see cref="ApiResponse{AdminSystemDeleteClinicResponse}"/> containing the execution result.</returns>
        Task<ApiResponse<AdminSystemDeleteClinicResponse>> Process(Guid id);
    }
}
