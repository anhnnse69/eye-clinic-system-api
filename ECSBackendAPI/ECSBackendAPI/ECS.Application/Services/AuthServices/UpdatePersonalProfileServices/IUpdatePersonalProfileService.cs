using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.UpdatePersonalProfileServices
{
    /// <summary>
    /// Update personal profile service interface.
    /// </summary>
    public interface IUpdatePersonalProfileService
    {
        /// <summary>
        /// Processes the personal profile update logic.
        /// </summary>
        /// <param name="currentUserId">The unique identifier of the user.</param>
        /// <param name="request">The personal profile update payload.</param>
        /// <returns>An <see cref="ApiResponse{UpdatePersonalProfileResponse}"/> containing the execution result.</returns>
        Task<ApiResponse<UpdatePersonalProfileResponse>> Process(Guid currentUserId, UpdatePersonalProfileRequest request);
    }
}
