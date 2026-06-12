using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices
{
    /// <summary>
    /// Update clinic service interface.
    /// </summary>
    public interface IUpdateClinicService
    {
        /// <summary>
        /// Processes the clinic update logic.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <param name="request">The clinic update payload.</param>
        /// <returns>An <see cref="ApiResponse{UpdateClinicResponse}"/> containing the execution result.</returns>
        Task<ApiResponse<UpdateClinicResponse>> Process(Guid id, UpdateClinicRequest request);
    }
}