using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.ViewPersonalProfileServices
{
    /// <summary>
    /// Handles the business logic for retrieving and mapping personal user profile profiles.
    /// </summary>
    public class GetPersonalProfileService : IGetPersonalProfileService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GetPersonalProfileService"/> with repository infrastructure.
        /// </summary>
        /// <param name="userQueryRepo">The repository query base context for User aggregate.</param>
        public GetPersonalProfileService(IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepo)
        {
            _userQueryRepo = userQueryRepo;
        }

        /// <summary>
        /// Orchestrates the retrieval and mapping of the targeted personal profile.
        /// </summary>
        /// <param name="currentUserId">The unique identifier of the user record to process.</param>
        /// <returns>An <see cref="ApiResponse{GetPersonalProfileResponse}"/> wrapping the compiled profile payload.</returns>
        public async Task<ApiResponse<GetPersonalProfileResponse>> Process(Guid currentUserId)
        {
            // Step 1: Eager load profile nodes utilizing tracking infrastructure filters
            var userAggregate = await FetchUserWithProfilesOrThrow(currentUserId);
            // Step 2: Map raw database entities securely to matching frontend payload contracts
            var responseDto = MapToProfileResponse(userAggregate);
            // Step 3: Bundle and emit outputs wrapped cleanly in unified system responses
            return CreateApiResponse(responseDto);
        }

        /// <summary>
        /// Fetches the user aggregate with related profiles or throws an exception if not found.
        /// </summary>
        /// <param name="userId">The targeted user identifier.</param>
        /// <returns>The fully loaded <see cref="User"/> entity.</returns>
        /// <throws cref="KeyNotFoundException">Thrown when no matching user is located.</throws>
        private async Task<User> FetchUserWithProfilesOrThrow(Guid userId)
        {
            var user = await _userQueryRepo.FindAll(trackChanges: false)
                .Include(u => u.StaffClinics!).ThenInclude(sc => sc.Clinic)
                .Include(u => u.DoctorProfiles!).ThenInclude(dp => dp.Clinic)
                .Include(u => u.DoctorProfiles!).ThenInclude(dp => dp.Specialty)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException("APP_MESSAGE_404_USER_NOT_FOUND");
            }
            return user;
        }

        /// <summary>
        /// Maps the raw user aggregate structure data to a structured profile response payload.
        /// </summary>
        /// <param name="user">The user entity context containing profile information.</param>
        /// <returns>A configured <see cref="GetPersonalProfileResponse"/> instance.</returns>
        private GetPersonalProfileResponse MapToProfileResponse(User user)
        {
            var response = new GetPersonalProfileResponse
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl,
                Clinic = new ClinicInfoNested { Name = "Chưa phân bổ", Address = "Chưa cập nhật" }
            };
            ExtractClinicAndProfessionalDetails(user, response);
            return response;
        }

        /// <summary>
        /// Extracts dynamic clinic and professional sub-details based on user role classification.
        /// </summary>
        /// <param name="user">The user data entity core structure.</param>
        /// <param name="response">The current response payload data transfer object being built.</param>
        private void ExtractClinicAndProfessionalDetails(User user, GetPersonalProfileResponse response)
        {
            if (user.Role == UserRole.DOCTOR)
            {
                var doctorProfile = user.DoctorProfiles?.FirstOrDefault(dp => dp.IsActive);
                if (doctorProfile != null)
                {
                    response.Clinic = new ClinicInfoNested
                    {
                        Name = doctorProfile.Clinic?.Name ?? user.StaffClinics?.FirstOrDefault()?.Clinic?.Name ?? "Chưa phân bổ",
                        Address = doctorProfile.Clinic?.Address ?? user.StaffClinics?.FirstOrDefault()?.Clinic?.Address ?? "Chưa cập nhật"
                    };
                    response.DoctorProfile = new DoctorProfileNested
                    {
                        Title = doctorProfile.Title ?? "Bác sĩ",
                        ExperienceYears = doctorProfile.ExperienceYears,
                        Bio = doctorProfile.Bio ?? "Thông tin giới thiệu bác sĩ chưa được cập nhật.",
                        SpecialtyName = doctorProfile.Specialty?.Name ?? "Mắt tổng quát"
                    };
                }
                else
                {
                    var staffClinic = user.StaffClinics?.FirstOrDefault(sc => sc.IsActive);
                    response.Clinic = new ClinicInfoNested
                    {
                        Name = staffClinic?.Clinic?.Name ?? "Chưa phân bổ",
                        Address = staffClinic?.Clinic?.Address ?? "Chưa cập nhật"
                    };
                    response.DoctorProfile = new DoctorProfileNested
                    {
                        Title = "Bác sĩ",
                        ExperienceYears = 0,
                        Bio = "Thông tin giới thiệu bác sĩ chưa được cập nhật.",
                        SpecialtyName = "Mắt tổng quát"
                    };
                }
            }
            else if (user.Role == UserRole.RECEPTIONIST)
            {
                var staffClinic = user.StaffClinics?.FirstOrDefault(sc => sc.IsActive);
                if (staffClinic != null)
                {
                    response.Clinic = new ClinicInfoNested
                    {
                        Name = staffClinic.Clinic.Name,
                        Address = staffClinic.Clinic.Address
                    };
                }
            }
        }

        /// <summary>
        /// Wraps the compiled profile payload into a success API response.
        /// </summary>
        /// <param name="data">The compiled personal profile response instance.</param>
        /// <returns>A configured <see cref="ApiResponse{GetPersonalProfileResponse}"/>.</returns>
        private ApiResponse<GetPersonalProfileResponse> CreateApiResponse(GetPersonalProfileResponse data)
        {
            return ApiResponse<GetPersonalProfileResponse>.Success("APP_MESSAGE_2000", data);
        }
    }
}