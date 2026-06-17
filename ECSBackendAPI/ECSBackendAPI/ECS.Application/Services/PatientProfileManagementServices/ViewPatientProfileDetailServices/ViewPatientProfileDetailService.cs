using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientProfileManagementServices.ViewPatientProfileDetailServices
{
    /// <summary>
    /// Handles retrieval of a detailed patient profile accessible by the authenticated patient.
    /// </summary>
    public class ViewPatientProfileDetailService : IViewPatientProfileDetailService
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>
        _patientProfileRepository;

        private readonly AppDbContext _context;

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewPatientProfileDetailService"/> class with required repositories,
        /// database contexts, and authentication claim accessors.
        /// </summary>
        /// <param name="patientProfileRepository">
        /// Repository boundary instance responsible for querying patient profile entities.
        /// </param>
        /// <param name="context">
        /// Underlying persistence database context managing relational entity state mappings.
        /// </param>
        /// <param name="httpContextAccessor">
        /// Accessor used to safely retrieve authentication claims from the current HTTP request pipeline.
        /// </param>
        public ViewPatientProfileDetailService(
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientProfileRepository = patientProfileRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal workflow pipeline to validate access permissions,
        /// retrieve profile information, and construct a detailed response payload.
        /// </summary>
        /// <param name="patientProfileId">
        /// The unique identifier associated with the target patient profile.
        /// </param>
        /// <returns>
        /// An <see cref="ApiResponse{ViewPatientProfileDetailResponse}"/> containing
        /// detailed patient profile information or an appropriate validation failure response.
        /// </returns>
        public async Task<ApiResponse<ViewPatientProfileDetailResponse>> Process(Guid patientProfileId)
        {
            bool isUserValid = true;
            bool isProfileExist = true;
            bool hasPermission = true;

            var userId = RetrieveUserId(ref isUserValid);

            var profile = await RetrievePatientProfile(patientProfileId, isUserValid);

            ValidateProfile(profile, isUserValid, ref isProfileExist);

            ValidatePermission(profile, userId, isProfileExist, ref hasPermission);

            var response = await BuildResponse(profile, userId, isProfileExist, hasPermission);

            return CreateResponse(response, isUserValid, isProfileExist, hasPermission);
        }

        /// <summary>
        /// Resolves the authenticated user identifier from the current security claims context.
        /// </summary>
        /// <param name="isUserValid">
        /// Validation flag updated when authentication claim extraction fails.
        /// </param>
        /// <returns>
        /// The parsed user identifier value if available; otherwise an empty GUID.
        /// </returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Retrieves the target patient profile entity from the underlying persistence layer.
        /// </summary>
        /// <param name="patientProfileId">
        /// The identifier associated with the requested patient profile.
        /// </param>
        /// <param name="isUserValid">
        /// Validation controller flag preventing repository access when authentication fails.
        /// </param>
        /// <returns>
        /// A patient profile entity instance if found; otherwise null.
        /// </returns>
        private async Task<PatientProfile?> RetrievePatientProfile(
            Guid patientProfileId,
            bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            return await _patientProfileRepository
                .FindByCondition(
                    x => x.Id == patientProfileId,
                    trackChanges: false)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Validates whether the requested patient profile exists within the system.
        /// </summary>
        /// <param name="profile">
        /// The retrieved patient profile entity instance.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether authentication validation succeeded.
        /// </param>
        /// <param name="isProfileExist">
        /// Output tracking flag updated when the profile cannot be located.
        /// </param>
        private void ValidateProfile(
            PatientProfile? profile,
            bool isUserValid,
            ref bool isProfileExist)
        {
            if (!isUserValid)
            {
                return;
            }

            if (profile == null)
            {
                isProfileExist = false;
            }
        }

        /// <summary>
        /// Evaluates whether the authenticated user has permission to access the requested patient profile.
        /// </summary>
        /// <param name="profile">
        /// The patient profile entity under evaluation.
        /// </param>
        /// <param name="userId">
        /// The authenticated user identifier.
        /// </param>
        /// <param name="isProfileExist">
        /// Indicates whether the target profile exists.
        /// </param>
        /// <param name="hasPermission">
        /// Permission tracking flag updated according to ownership and relationship rules.
        /// </param>
        private void ValidatePermission(
            PatientProfile? profile,
            Guid userId,
            bool isProfileExist,
            ref bool hasPermission)
        {
            if (!isProfileExist || profile == null)
            {
                return;
            }

            bool isOwner = profile.UserId == userId;

            bool isLinked = _context.Set<UserPatient>()
                .AsNoTracking()
                .Any(x =>
                    x.UserId == userId &&
                    x.PatientId == profile.Id);

            hasPermission = isOwner || isLinked;
        }

        /// <summary>
        /// Constructs the response data transfer object after successful validation checks.
        /// </summary>
        /// <param name="profile">
        /// The retrieved patient profile entity.
        /// </param>
        /// <param name="currentUserId">
        /// The authenticated user identifier.
        /// </param>
        /// <param name="isProfileExist">
        /// Indicates profile existence validation state.
        /// </param>
        /// <param name="hasPermission">
        /// Indicates authorization validation state.
        /// </param>
        /// <returns>
        /// A populated response DTO or null when validation requirements fail.
        /// </returns>
        private async Task<ViewPatientProfileDetailResponse?> BuildResponse(
            PatientProfile? profile,
            Guid currentUserId,
            bool isProfileExist,
            bool hasPermission)
        {
            if (!isProfileExist ||
                !hasPermission ||
                profile == null)
            {
                return null;
            }

            return await MapToResponseDto(
                profile,
                currentUserId);
        }

        /// <summary>
        /// Maps internal patient profile domain entities onto response transfer objects.
        /// </summary>
        /// <param name="profile">
        /// The patient profile domain entity instance.
        /// </param>
        /// <param name="currentUserId">
        /// The authenticated user identifier used for relationship resolution.
        /// </param>
        /// <returns>
        /// A fully populated patient profile detail response object.
        /// </returns>
        private async Task<ViewPatientProfileDetailResponse> MapToResponseDto(
            PatientProfile profile,
            Guid currentUserId)
        {
            var relationship = await RetrieveRelationship(
                currentUserId,
                profile.Id);

            return new ViewPatientProfileDetailResponse
            {
                PatientProfileId = profile.Id,
                FullName = profile.FullName,
                Gender = profile.Gender,
                Dob = profile.Dob,
                IdentityNumber = profile.IdentityNumber,
                Address = profile.Address,
                PhoneNumber = profile.PhoneNumber,
                BhytNumber = profile.BhytNumber,
                BloodType = profile.BloodType,
                Allergies = profile.Allergies,
                MedicalHistory = profile.MedicalHistory,
                Relationship = relationship,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }

        /// <summary>
        /// Retrieves the relationship descriptor between the authenticated user and the target patient profile.
        /// </summary>
        /// <param name="userId">
        /// The authenticated user identifier.
        /// </param>
        /// <param name="patientProfileId">
        /// The target patient profile identifier.
        /// </param>
        /// <returns>
        /// The resolved relationship descriptor string; defaults to "Bản thân" when unavailable.
        /// </returns>
        private async Task<string> RetrieveRelationship(
            Guid userId,
            Guid patientProfileId)
        {
            var relationship = await _context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.PatientId == patientProfileId)
                .Select(x => x.Relationship)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(relationship)
                ? "Bản thân"
                : relationship;
        }

        /// <summary>
        /// Packages workflow execution results into standardized success or failure response envelopes.
        /// </summary>
        /// <param name="response">
        /// The generated response payload.
        /// </param>
        /// <param name="isUserValid">
        /// Authentication validation state.
        /// </param>
        /// <param name="isProfileExist">
        /// Profile existence validation state.
        /// </param>
        /// <param name="hasPermission">
        /// Authorization validation state.
        /// </param>
        /// <returns>
        /// A standardized application response structure.
        /// </returns>
        private ApiResponse<ViewPatientProfileDetailResponse> CreateResponse(
            ViewPatientProfileDetailResponse? response,
            bool isUserValid,
            bool isProfileExist,
            bool hasPermission)
        {
            var errorResponse = CreateErrorResponse(
                isUserValid,
                isProfileExist,
                hasPermission);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<ViewPatientProfileDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response!);
        }

        /// <summary>
        /// Evaluates workflow validation states and generates standardized error responses when required.
        /// </summary>
        /// <param name="isUserValid">
        /// Authentication validation state indicator.
        /// </param>
        /// <param name="isProfileExist">
        /// Profile existence validation state indicator.
        /// </param>
        /// <param name="hasPermission">
        /// Authorization validation state indicator.
        /// </param>
        /// <returns>
        /// A failed API response when validation rules fail; otherwise null.
        /// </returns>
        private ApiResponse<ViewPatientProfileDetailResponse>? CreateErrorResponse(
            bool isUserValid,
            bool isProfileExist,
            bool hasPermission)
        {
            if (!isUserValid)
            {
                return ApiResponse<ViewPatientProfileDetailResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4033.ToString());
            }

            if (!isProfileExist)
            {
                return ApiResponse<ViewPatientProfileDetailResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4010.ToString());
            }

            if (!hasPermission)
            {
                return ApiResponse<ViewPatientProfileDetailResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4014.ToString());
            }

            return null;
        }
    }

}
