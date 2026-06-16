using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.UpdatePersonalProfileServices
{
    /// <summary>
    /// Handles the business logic for updating personal profile information.
    /// </summary>
    public class UpdatePersonalProfileService : IUpdatePersonalProfileService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdatePersonalProfileService"/> with required dependencies.
        /// </summary>
        /// <param name="userRepository">Repository for data persistence operations.</param>
        public UpdatePersonalProfileService(IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Processes the profile update by fetching, mutating, saving the entity, and returning the result.
        /// </summary>
        /// <param name="currentUserId">The unique identifier of the user.</param>
        /// <param name="request">The personal profile update data payload.</param>
        /// <returns>An <see cref="ApiResponse{UpdatePersonalProfileResponse}"/> indicating success.</returns>
        public async Task<ApiResponse<UpdatePersonalProfileResponse>> Process(Guid currentUserId, UpdatePersonalProfileRequest request)
        {
            // Step 1: Retrieve the existing user entity with related profiles or throw an exception if missing
            var existingUser = await FetchUserWithRelatedProfilesOrThrow(currentUserId);
            // Step 2: Map and mutate fields with incoming request data
            var mutatedUser = MapAndMutateProperties(existingUser, request);
            // Step 3: Persist data modifications to the database
            await SaveDataChanges(mutatedUser);
            // Step 4: Construct the response DTO wrapper
            var responseDto = BuildResponseDto(mutatedUser);
            return CreateApiResponse(responseDto);
        }

        /// <summary>
        /// Queries the repository for an existing user entity matching the given ID including related doctor profiles.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The resolved <see cref="User"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no matching entity is found.</exception>
        private async Task<User> FetchUserWithRelatedProfilesOrThrow(Guid userId)
        {
            var user = await _userRepository.FindAll(trackChanges: true)
                .Include(u => u.DoctorProfiles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            return user ?? throw new KeyNotFoundException("APP_MESSAGE_404_USER_NOT_FOUND");
        }

        /// <summary>
        /// Maps incoming request payloads to mutate properties of the original entity.
        /// </summary>
        /// <param name="user">The original domain entity instance.</param>
        /// <param name="request">The request payload data to apply.</param>
        /// <returns>The mutated <see cref="User"/> instance.</returns>
        private User MapAndMutateProperties(User user, UpdatePersonalProfileRequest request)
        {
            user.FullName = request.FullName.Trim();
            user.Phone = request.Phone.Trim();
            user.Email = request.Email?.Trim();
            user.AvatarUrl = request.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            MutateProfessionalProfileIfDoctor(user, request);
            return user;
        }

        /// <summary>
        /// Conditionally updates specific doctor profile fields if the user has a doctor role.
        /// </summary>
        /// <param name="user">The original domain entity instance.</param>
        /// <param name="request">The request payload data to apply.</param>
        private void MutateProfessionalProfileIfDoctor(User user, UpdatePersonalProfileRequest request)
        {
            if (user.Role == UserRole.DOCTOR)
            {
                var activeDoctorProfile = user.DoctorProfiles?.FirstOrDefault(dp => dp.IsActive);
                if (activeDoctorProfile != null)
                {
                    activeDoctorProfile.Title = request.Title?.Trim();
                    activeDoctorProfile.ExperienceYears = request.ExperienceYears;
                    activeDoctorProfile.Bio = request.Bio?.Trim();
                    activeDoctorProfile.SpecialtyId = request.SpecialtyId;
                    activeDoctorProfile.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Commits the modified entity states asynchronously into database context.
        /// </summary>
        /// <param name="user">The updated user entity instance.</param>
        private async Task SaveDataChanges(User user)
        {
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Builds a summary data payload from the updated entity properties.
        /// </summary>
        /// <param name="user">The updated user domain entity.</param>
        /// <returns>A configured <see cref="UpdatePersonalProfileResponse"/> data payload.</returns>
        private UpdatePersonalProfileResponse BuildResponseDto(User user)
        {
            return new UpdatePersonalProfileResponse
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Role = user.Role.ToString(),
                UpdatedAt = user.UpdatedAt.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        /// <summary>
        /// Wraps the processed response model into a structured API response payload.
        /// </summary>
        /// <param name="data">The built transaction response data.</param>
        /// <returns>A successful <see cref="ApiResponse{UpdatePersonalProfileResponse}"/> variant wrapper.</returns>
        private ApiResponse<UpdatePersonalProfileResponse> CreateApiResponse(UpdatePersonalProfileResponse data)
        {
            return ApiResponse<UpdatePersonalProfileResponse>.Success("APP_MESSAGE_2000", data);
        }
    }
}