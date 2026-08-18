using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientProfileManagementServices.CheckSelfProfileServices
{
    /// <summary>
    /// Implements specific domain process pipelines to seamlessly check whether the authenticated user owns a self patient profile.
    /// </summary>
    public class CheckSelfProfileService : ICheckSelfProfileService
    {
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new operational instance of <see cref="CheckSelfProfileService"/> mapped with data engine references.
        /// </summary>
        /// <param name="patientProfileRepository">Repository boundary instance for querying physical patient profile records.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public CheckSelfProfileService(
            IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientProfileRepository = patientProfileRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Core orchestration handling transactional logic to verify self profile existence without any direct execution routing conditionals.
        /// </summary>
        /// <returns>An <see cref="ApiResponse{CheckSelfProfileResponse}"/> enclosing descriptive state payloads alongside metadata boundaries.</returns>
        public async Task<ApiResponse<CheckSelfProfileResponse>> Process()
        {
            // Initialize status tracking flags to allow complete process structural tracking flow without using block conditionals
            bool isCurrentUserValid = true;

            // Step 1: Extract Authenticated User ID from context via synchronous method
            var activeUserId = RetrieveAuthenticatedUserId(ref isCurrentUserValid);

            // Step 2: Check if authenticated user owns a self patient profile asynchronously
            var hasSelfProfile = await CheckSelfProfileExistence(activeUserId, isCurrentUserValid);

            // Step 3: Compile the execution state and return transactional tracking structure results
            return CreateResponse(isCurrentUserValid, hasSelfProfile);
        }

        /// <summary>
        /// Resolves claims structures inside requests wrappers onto concrete identification indicators safely.
        /// </summary>
        /// <param name="isCurrentUserValid">Output evaluation status updating to false if identification variables fail parsing matches.</param>
        /// <returns>The decoded user identity descriptor parameter identifier value.</returns>
        private Guid RetrieveAuthenticatedUserId(ref bool isCurrentUserValid)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(principalIdValue, out var parsedUserId))
            {
                isCurrentUserValid = false;
                return Guid.Empty;
            }
            return parsedUserId;
        }

        /// <summary>
        /// Queries persistence boundaries to determine whether a direct-ownership self patient profile exists under the user account.
        /// </summary>
        /// <param name="userId">The active authenticated user unique identifier value.</param>
        /// <param name="userState">Precondition evaluation flag indicating if user resolving succeeded; returns false immediately when not valid.</param>
        /// <returns>A boolean indicating whether the user already owns a self patient profile record.</returns>
        private async Task<bool> CheckSelfProfileExistence(Guid userId, bool userState)
        {
            if (!userState) return false;

            return await _patientProfileRepository
                .FindByCondition(x => x.UserId == userId)
                .AnyAsync();
        }

        /// <summary>
        /// Transforms processing contexts into appropriate application response payloads tracking logical runtime statuses.
        /// </summary>
        /// <param name="userState">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="hasSelfProfile">The boolean result indicating self profile ownership status.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<CheckSelfProfileResponse> CreateResponse(bool userState, bool hasSelfProfile)
        {
            var functionalErrorsEnvelope = FilterSystemicValidationFailures(userState);
            if (functionalErrorsEnvelope != null)
            {
                return functionalErrorsEnvelope;
            }

            return ApiResponse<CheckSelfProfileResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new CheckSelfProfileResponse { HasSelfProfile = hasSelfProfile });
        }

        /// <summary>
        /// Analyzes runtime flags status metrics to convert issues directly into clear system response error structures.
        /// </summary>
        /// <param name="userState">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<CheckSelfProfileResponse>? FilterSystemicValidationFailures(bool userState)
        {
            if (!userState)
            {
                return ApiResponse<CheckSelfProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4033.ToString()); // Authenticated user information is invalid
            }
            return null;
        }
    }
}