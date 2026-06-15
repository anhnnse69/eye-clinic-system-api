using ECS.Application.Common.Response;

namespace ECS.Application.Services.AuthServices.ViewPersonalProfileServices
{
    /// <summary>
    /// Interface for the personal profile retrieval service.
    /// </summary>
    public interface IGetPersonalProfileService
    {
        /// <summary>
        /// Processes profile data construction pipelines based on target tracking identifier.
        /// </summary>
        /// <param name="currentUserId">The unique identifier of the current user context.</param>
        /// <returns>An <see cref="ApiResponse{GetPersonalProfileResponse}"/> containing the profile details.</returns>
        Task<ApiResponse<GetPersonalProfileResponse>> Process(Guid currentUserId);
    }
}