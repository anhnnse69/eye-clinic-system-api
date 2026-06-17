using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientProfileManagementServices.CreatePatientProfileServices
{
    /// <summary>
    /// Implements specific domain process pipelines to seamlessly create patient profiles matching authorized user session.
    /// </summary>
    public class CreatePatientProfileService : ICreatePatientProfileService
    {
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new operational instance of <see cref="CreatePatientProfileService"/> mapped with data engine references.
        /// </summary>
        /// <param name="patientProfileRepository">Repository boundary instance for writing physical patient profile records.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public CreatePatientProfileService(
            IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientProfileRepository = patientProfileRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Core orchestration handling transactional logic to provision valid unique patient profiles without any direct execution routing conditionals.
        /// </summary>
        /// <param name="request">The parameters containing explicit data payload variables required to formulate a new record.</param>
        /// <returns>An <see cref="ApiResponse{CreatePatientProfileResponse}"/> enclosing descriptive state payloads alongside metadata boundaries.</returns>
        public async Task<ApiResponse<CreatePatientProfileResponse>> Process(CreatePatientProfileRequest request)
        {
            // Initialize status tracking flags to allow complete process structural tracking flow without using block conditionals
            bool isCurrentUserValid = true;
            bool isIdentityUnique = true;

            // Step 1: Extract Authenticated User ID from context via synchronous method
            var activeUserId = RetrieveAuthenticatedUserId(ref isCurrentUserValid);

            // Step 2: Verify unique availability properties across standard identity number attributes asynchronously
            isIdentityUnique = await CheckIdentityNumberUniqueness(request.IdentityNumber, isCurrentUserValid);

            // Step 3: Build physical model state definitions containing structural entities inside context limits
            var targetPatientProfileTree = ConstructPatientProfileTree(request, activeUserId, isCurrentUserValid, isIdentityUnique);

            // Step 4: Persist the combined structural data graph down onto physical repositories utilizing a value Tuple pattern
            var (committedUserPatientNode, isExecutionSuccess) = await PersistPatientProfileGraph(targetPatientProfileTree, isCurrentUserValid, isIdentityUnique);

            // Step 5: Compile the execution state and return transactional tracking structure results
            return CreateResponse(targetPatientProfileTree, committedUserPatientNode, isCurrentUserValid, isIdentityUnique, isExecutionSuccess);
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
        /// Determines whether targeted customer identity numbers are unmapped inside persistence frameworks systemwide.
        /// </summary>
        /// <param name="identityNumber">The explicit tracking sequence string assigned onto identification markers.</param>
        /// <param name="isCurrentUserValid">Precondition evaluation flag indicating if user resolving succeeded.</param>
        /// <returns>A status flag checking boolean indicating the absence of structural state data duplication.</returns>
        private async Task<bool> CheckIdentityNumberUniqueness(string? identityNumber, bool isCurrentUserValid)
        {
            if (!isCurrentUserValid || string.IsNullOrWhiteSpace(identityNumber))
            {
                return true;
            }

            var recordConflictExists = await _patientProfileRepository
                .FindByCondition(profile => profile.IdentityNumber == identityNumber.Trim())
                .AnyAsync();

            return !recordConflictExists;
        }

        /// <summary>
        /// Maps domain logic configurations to build structured storage model hierarchies cleanly.
        /// </summary>
        /// <param name="dataInput">The structural entity containing baseline record details and generic parameters.</param>
        /// <param name="activeUserId">The unique tracker reference coordinates mapping active context identifiers tokens.</param>
        /// <param name="userState">Guard execution flag assessing baseline tracking criteria states.</param>
        /// <param name="identityState">Guard execution flag assessing uniqueness constraints variables.</param>
        /// <returns>A comprehensive domains graph structure hierarchy mapped safely into operational frameworks context.</returns>
        private PatientProfile? ConstructPatientProfileTree(CreatePatientProfileRequest dataInput, Guid activeUserId, bool userState, bool identityState)
        {
            if (!userState || !identityState)
            {
                return null;
            }

            bool isSelf = dataInput.Relationship.Trim().Equals("Bản thân", StringComparison.OrdinalIgnoreCase);

            var createdProfile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = isSelf ? activeUserId : null,
                FullName = dataInput.FullName.Trim(),
                Gender = dataInput.Gender,
                Dob = dataInput.Dob,
                IdentityNumber = dataInput.IdentityNumber?.Trim(),
                Address = dataInput.Address?.Trim(),
                PhoneNumber = dataInput.PhoneNumber?.Trim(),
                BhytNumber = dataInput.BhytNumber?.Trim(),
                BloodType = dataInput.BloodType?.Trim(),
                Allergies = dataInput.Allergies?.Trim(),
                MedicalHistory = dataInput.MedicalHistory?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            createdProfile.UserPatients = new List<UserPatient>
            {
                new UserPatient
                {
                    UserId = activeUserId,
                    PatientId = createdProfile.Id,
                    Relationship = dataInput.Relationship.Trim(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            return createdProfile;
        }

        /// <summary>
        /// Handles isolated low-level persistent storage commands wrapping mutations into explicit unit blocks safely.
        /// </summary>
        /// <param name="profileGraph">The composite persistent entity context node mapping layout multi-table bounds.</param>
        /// <param name="userState">Precondition validation flag evaluating authenticated pipeline integrity maps.</param>
        /// <param name="identityState">Precondition validation flag evaluating unique constraint context indicators.</param>
        /// <returns>A tuple pairing structural relational linking nodes alongside execution outcome confirmation states.</returns>
        private async Task<(UserPatient? UserPatientNode, bool IsSuccess)> PersistPatientProfileGraph(PatientProfile? profileGraph, bool userState, bool identityState)
        {
            if (profileGraph == null || !userState || !identityState)
            {
                return (null, false);
            }

            using var transactionalScope = await _patientProfileRepository.BeginTransactionAsync();
            try
            {
                await _patientProfileRepository.CreateAsync(profileGraph);
                await _patientProfileRepository.SaveChangesAsync();
                await transactionalScope.CommitAsync();

                return (profileGraph.UserPatients.First(), true);
            }
            catch (Exception)
            {
                await transactionalScope.RollbackAsync();
                return (null, false);
            }
        }

        /// <summary>
        /// Transforms processing contexts into appropriate application response payloads tracking logical runtime statuses.
        /// </summary>
        /// <param name="coreProfile">The operational persistent model graph instance extracted out of system scopes.</param>
        /// <param name="operationalNode">The detailed structural context link mapping unique multi-user dependencies metadata.</param>
        /// <param name="userState">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="identityState">Indicates unique identity validation tracking presence attributes.</param>
        /// <param name="successState">Indicates physical operational pipeline execution commit outcomes flags.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<CreatePatientProfileResponse> CreateResponse(
            PatientProfile? coreProfile,
            UserPatient? operationalNode,
            bool userState,
            bool identityState,
            bool successState)
        {
            var functionalErrorsEnvelope = FilterSystemicValidationFailures(userState, identityState, successState);
            if (functionalErrorsEnvelope != null)
            {
                return functionalErrorsEnvelope;
            }

            return ApiResponse<CreatePatientProfileResponse>.Success(
                GeneralCode.APP_MESSAGE_2005.ToString(),
                MapToResponse(coreProfile!, operationalNode!));
        }

        /// <summary>
        /// Analyzes runtime flags status metrics to convert issues directly into clear system response error structures.
        /// </summary>
        /// <param name="userState">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <param name="identityState">The state checking indicator capturing data integrity validation conflicts.</param>
        /// <param name="successState">The tracking metrics confirming database operation transactional outcomes flags.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<CreatePatientProfileResponse>? FilterSystemicValidationFailures(bool userState, bool identityState, bool successState)
        {
            if (!userState)
            {
                return ApiResponse<CreatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4033.ToString()); // Authenticated user information is invalid
            }
            if (!identityState)
            {
                return ApiResponse<CreatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_4042.ToString()); // Identity number already exists in the system.
            }
            if (!successState)
            {
                return ApiResponse<CreatePatientProfileResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString()); // Database operation failed
            }
            return null;
        }

        /// <summary>
        /// Performs isolation transforms mapping physical database fields structures down to serialization schemas safely.
        /// </summary>
        /// <param name="profileSource">The physical domain core model instance returned out of backend operational layers.</param>
        /// <param name="allocationLink">The allocated layout dictionary tracking interpersonal relationship definitions metadata.</param>
        /// <returns>A structured target presentation data payload container instance context.</returns>
        private CreatePatientProfileResponse MapToResponse(PatientProfile profileSource, UserPatient allocationLink)
        {
            return new CreatePatientProfileResponse
            {
                PatientProfileId = profileSource.Id,
                FullName = profileSource.FullName,
                Relationship = allocationLink.Relationship ?? "N/A",
                CreatedAt = profileSource.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            };
        }
    }
}