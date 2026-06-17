using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientProfileManagementServices.UpdatePatientProfileServices
{
    /// <summary>
    /// Implements specific domain process pipelines to seamlessly update patient profiles matching authorized user session.
    /// </summary>
    public class UpdatePatientProfileService : IUpdatePatientProfileService
    {
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new operational instance of <see cref="UpdatePatientProfileService"/> mapped with data engine references.
        /// </summary>
        /// <param name="patientProfileRepository">Repository boundary instance for updating physical patient profile records.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        /// /// <param name="dbContext">Database context instance enabling explicit entity state tracking and persistence synchronization.</param>
        public UpdatePatientProfileService(
            IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext dbContext)
        {
            _patientProfileRepository = patientProfileRepository;
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Core orchestration handling transactional logic to update valid patient profiles without any direct execution routing conditionals.
        /// </summary>
        /// <param name="profileId">The target profile unique identification key.</param>
        /// <param name="request">The parameters containing explicit data payload variables required to modify the record.</param>
        /// <returns>An <see cref="ApiResponse{UpdatePatientProfileResponse}"/> enclosing descriptive state payloads alongside metadata boundaries.</returns>
        public async Task<ApiResponse<UpdatePatientProfileResponse>> Process(Guid profileId, UpdatePatientProfileRequest request)
        {
            // Initialize status tracking flags to allow complete process structural tracking flow without using block conditionals
            bool isCurrentUserValid = true;
            bool isProfileExisted = true;
            bool isUserAuthorized = true;
            bool isIdentityUnique = true;

            // Step 1: Extract Authenticated User ID from context via synchronous method
            var activeUserId = RetrieveAuthenticatedUserId(ref isCurrentUserValid);

            // Step 2: Retrieve existing profile graph asynchronously
            var existingProfile = await FetchActivePatientProfile(profileId, isCurrentUserValid);

            // Step 2.5: Validate profile existence synchronously to safely update tracker flags without ref async errors
            ValidateProfileExistence(existingProfile, isCurrentUserValid, ref isProfileExisted);

            // Step 3: Verify security mapping permissions between session context and tracking nodes
            var existingLink = VerifyOwnershipRelation(existingProfile, activeUserId, isCurrentUserValid, isProfileExisted, ref isUserAuthorized);

            // Step 4: Verify unique availability properties across standard identity number attributes asynchronously
            isIdentityUnique = await CheckIdentityNumberUniqueness(profileId, request.IdentityNumber, isCurrentUserValid, isProfileExisted, isUserAuthorized);

            // Step 5: Mutate domain structural model states inside system boundaries safely
            var updatedProfileTree = ApplyDataMutations(existingProfile, existingLink, request, activeUserId, isCurrentUserValid, isProfileExisted, isUserAuthorized, isIdentityUnique);

            // Step 6: Persist the modified structural data graph down onto physical repositories utilizing a value Tuple pattern
            var (committedUserPatientNode, isExecutionSuccess) = await PersistUpdatedProfileGraph(updatedProfileTree, existingLink, isCurrentUserValid, isProfileExisted, isUserAuthorized, isIdentityUnique);

            // Step 7: Compile the execution state and return transactional tracking structure results
            return CreateResponse(updatedProfileTree, committedUserPatientNode, isCurrentUserValid, isProfileExisted, isUserAuthorized, isIdentityUnique, isExecutionSuccess);
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
        /// Retrieves an existing patient profile graph including relationship mappings from persistence boundaries.
        /// </summary>
        /// <param name="profileId">The unique identifier sequence assigned onto the target patient profile node.</param>
        /// <param name="userState">Precondition evaluation flag indicating whether authenticated user extraction succeeded.</param>
        /// <returns>A structural patient profile hierarchy loaded together with relational association collections.</returns>
        private async Task<PatientProfile?> FetchActivePatientProfile(Guid profileId, bool userState)
        {
            if (!userState) return null;

            // Load including the UserPatients relational tree boundaries 
            return await _patientProfileRepository.GetByIdAsync(profileId, x => x.UserPatients!);
        }

        /// <summary>
        /// Evaluates whether a requested patient profile entity physically exists inside current persistence scopes.
        /// </summary>
        /// <param name="profile">The retrieved profile instance extracted from repository execution contexts.</param>
        /// <param name="userState">Precondition evaluation flag indicating authenticated session validity.</param>
        /// <param name="isProfileExisted">Tracking indicator updated to false when no matching record is located.</param>
        private void ValidateProfileExistence(PatientProfile? profile, bool userState, ref bool isProfileExisted)
        {
            if (!userState) return;

            if (profile == null)
            {
                isProfileExisted = false;
            }
        }

        /// <summary>
        /// Validates ownership associations between authenticated users and targeted patient profile structures.
        /// </summary>
        /// <param name="profile">The loaded patient profile graph containing relationship allocation collections.</param>
        /// <param name="userId">The active authenticated user unique identifier value.</param>
        /// <param name="userState">Guard execution flag evaluating authentication context integrity.</param>
        /// <param name="profileState">Guard execution flag evaluating profile existence verification.</param>
        /// <param name="isUserAuthorized">Tracking indicator updated to false when ownership validation fails.</param>
        /// <returns>The resolved relationship allocation node linking user and patient profile contexts.</returns>
        private UserPatient? VerifyOwnershipRelation(PatientProfile? profile, Guid userId, bool userState, bool profileState, ref bool isUserAuthorized)
        {
            if (!userState || !profileState || profile == null || profile.UserPatients == null) return null;

            var connectionLink = profile.UserPatients.FirstOrDefault(up => up.UserId == userId);
            if (connectionLink == null)
            {
                isUserAuthorized = false;
            }
            return connectionLink;
        }

        /// <summary>
        /// Determines whether targeted identity numbers remain uniquely allocated across patient profile records.
        /// </summary>
        /// <param name="profileId">The identifier of the profile currently being modified inside transactional scopes.</param>
        /// <param name="identityNumber">The identity tracking sequence submitted for uniqueness validation.</param>
        /// <param name="userState">Precondition validation flag evaluating authentication pipeline integrity.</param>
        /// <param name="profileState">Precondition validation flag evaluating profile existence conditions.</param>
        /// <param name="authState">Precondition validation flag evaluating ownership authorization constraints.</param>
        /// <returns>A status flag indicating whether identity number allocation conflicts are absent systemwide.</returns>
        private async Task<bool> CheckIdentityNumberUniqueness(Guid profileId, string? identityNumber, bool userState, bool profileState, bool authState)
        {
            if (!userState || !profileState || !authState || string.IsNullOrWhiteSpace(identityNumber))
            {
                return true;
            }

            var cleanIdentityNumber = identityNumber.Trim();

            var recordConflictExists = await _patientProfileRepository
                .FindByCondition(profile => profile.IdentityNumber != null
                                         && profile.IdentityNumber.Trim() == cleanIdentityNumber
                                         && profile.Id != profileId)
                .AnyAsync();

            return !recordConflictExists;
        }

        /// <summary>
        /// Applies incoming request payload values onto existing patient profile domain entities safely.
        /// </summary>
        /// <param name="target">The persistent patient profile graph targeted for mutation operations.</param>
        /// <param name="link">The relationship allocation node connecting users with patient profiles.</param>
        /// <param name="source">The request payload carrying updated profile attributes definitions.</param>
        /// <param name="activeUserId">The active authenticated user identifier extracted from execution context.</param>
        /// <param name="userState">Guard execution flag evaluating authentication validation criteria.</param>
        /// <param name="profileState">Guard execution flag evaluating profile existence constraints.</param>
        /// <param name="authState">Guard execution flag evaluating ownership authorization requirements.</param>
        /// <param name="identityState">Guard execution flag evaluating identity uniqueness validation status.</param>
        /// <returns>The modified patient profile graph reflecting updated domain state definitions.</returns>
        private PatientProfile? ApplyDataMutations(PatientProfile? target, UserPatient? link, UpdatePatientProfileRequest source, Guid activeUserId, bool userState, bool profileState, bool authState, bool identityState)
        {
            if (!userState || !profileState || !authState || !identityState || target == null || link == null)
            {
                return null;
            }

            bool isSelf = source.Relationship.Trim().Equals("Bản thân", StringComparison.OrdinalIgnoreCase);

            target.UserId = isSelf ? activeUserId : null;
            target.FullName = source.FullName.Trim();
            target.Gender = source.Gender;
            target.Dob = source.Dob;

            target.IdentityNumber = string.IsNullOrWhiteSpace(source.IdentityNumber) ? null : source.IdentityNumber.Trim();
            target.Address = string.IsNullOrWhiteSpace(source.Address) ? null : source.Address.Trim();
            target.PhoneNumber = string.IsNullOrWhiteSpace(source.PhoneNumber) ? null : source.PhoneNumber.Trim();
            target.BhytNumber = string.IsNullOrWhiteSpace(source.BhytNumber) ? null : source.BhytNumber.Trim();
            target.BloodType = string.IsNullOrWhiteSpace(source.BloodType) ? null : source.BloodType.Trim();
            target.Allergies = string.IsNullOrWhiteSpace(source.Allergies) ? null : source.Allergies.Trim();
            target.MedicalHistory = string.IsNullOrWhiteSpace(source.MedicalHistory) ? null : source.MedicalHistory.Trim();
            target.UpdatedAt = DateTime.UtcNow;

            // Mutate connection metrics fields
            link.Relationship = source.Relationship.Trim();

            return target;
        }

        /// <summary>
        /// Handles isolated persistence commands responsible for committing updated profile graphs transactionally.
        /// </summary>
        /// <param name="profileGraph">The modified patient profile hierarchy awaiting storage synchronization.</param>
        /// <param name="relationLink">The relationship allocation node requiring persistence updates.</param>
        /// <param name="userState">Precondition validation flag evaluating authenticated execution integrity.</param>
        /// <param name="profileState">Precondition validation flag evaluating profile existence verification.</param>
        /// <param name="authState">Precondition validation flag evaluating ownership authorization validity.</param>
        /// <param name="identityState">Precondition validation flag evaluating uniqueness constraint compliance.</param>
        /// <returns>A tuple pairing committed relationship nodes alongside execution outcome confirmation states.</returns>
        private async Task<(UserPatient? UserPatientNode, bool IsSuccess)> PersistUpdatedProfileGraph(PatientProfile? profileGraph, UserPatient? relationLink, bool userState, bool profileState, bool authState, bool identityState)
        {
            if (profileGraph == null || relationLink == null || !userState || !profileState || !authState || !identityState)
            {
                return (null, false);
            }

            using var transactionalScope = await _patientProfileRepository.BeginTransactionAsync();
            try
            {
                await _patientProfileRepository.UpdateAsync(profileGraph);
                _dbContext.Entry(relationLink).State = EntityState.Modified;
                await _patientProfileRepository.SaveChangesAsync();
                await transactionalScope.CommitAsync();

                return (relationLink, true);
            }
            catch (Exception)
            {
                await transactionalScope.RollbackAsync();
                return (null, false);
            }
        }

        /// <summary>
        /// Transforms processing contexts into standardized response payload structures reflecting runtime outcomes.
        /// </summary>
        /// <param name="coreProfile">The updated persistent profile graph extracted from transactional boundaries.</param>
        /// <param name="operationalNode">The relationship allocation node linked to the modified profile.</param>
        /// <param name="userState">Indicates whether authentication extraction succeeded successfully.</param>
        /// <param name="profileState">Indicates whether target profile existence validation passed.</param>
        /// <param name="authState">Indicates whether ownership authorization requirements were satisfied.</param>
        /// <param name="identityState">Indicates whether identity uniqueness validation passed successfully.</param>
        /// <param name="successState">Indicates whether persistence execution committed successfully.</param>
        /// <returns>A standardized application payload container prepared for transport serialization layers.</returns>
        private ApiResponse<UpdatePatientProfileResponse> CreateResponse(
            PatientProfile? coreProfile,
            UserPatient? operationalNode,
            bool userState,
            bool profileState,
            bool authState,
            bool identityState,
            bool successState)
        {
            var functionalErrorsEnvelope = FilterSystemicValidationFailures(userState, profileState, authState, identityState, successState);
            if (functionalErrorsEnvelope != null)
            {
                return functionalErrorsEnvelope;
            }

            return ApiResponse<UpdatePatientProfileResponse>.Success(
                GeneralCode.APP_MESSAGE_2006.ToString(),
                MapToResponse(coreProfile!, operationalNode!));
        }

        /// <summary>
        /// Analyzes runtime validation flags and converts failed conditions into standardized error payloads.
        /// </summary>
        /// <param name="userState">The structural state verifying authenticated user extraction validity.</param>
        /// <param name="profileState">The structural state verifying target profile existence status.</param>
        /// <param name="authState">The structural state verifying ownership authorization constraints.</param>
        /// <param name="identityState">The structural state verifying identity uniqueness requirements.</param>
        /// <param name="successState">The structural state verifying transactional persistence success.</param>
        /// <returns>A failed API response package if validation rules fail; otherwise null.</returns>
        private ApiResponse<UpdatePatientProfileResponse>? FilterSystemicValidationFailures(bool userState, bool profileState, bool authState, bool identityState, bool successState)
        {
            if (!userState)
            {
                return ApiResponse<UpdatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4033.ToString());
            }
            if (!profileState)
            {
                return ApiResponse<UpdatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4010.ToString());
            }
            if (!authState)
            {
                return ApiResponse<UpdatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4014.ToString());
            }
            if (!identityState)
            {
                return ApiResponse<UpdatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4043.ToString());
            }
            if (!successState)
            {
                return ApiResponse<UpdatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
            }
            return null;
        }

        /// <summary>
        /// Performs projection mappings from domain entities into response transfer payload schemas.
        /// </summary>
        /// <param name="profileSource">The updated patient profile entity retrieved from operational layers.</param>
        /// <param name="allocationLink">The relationship allocation mapping associated with profile ownership.</param>
        /// <returns>A structured response payload containing updated profile confirmation details.</returns>
        private UpdatePatientProfileResponse MapToResponse(PatientProfile profileSource, UserPatient allocationLink)
        {
            return new UpdatePatientProfileResponse
            {
                PatientProfileId = profileSource.Id,
                FullName = profileSource.FullName,
                Relationship = allocationLink.Relationship ?? "N/A",
                UpdatedAt = profileSource.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
            };
        }
    }
}