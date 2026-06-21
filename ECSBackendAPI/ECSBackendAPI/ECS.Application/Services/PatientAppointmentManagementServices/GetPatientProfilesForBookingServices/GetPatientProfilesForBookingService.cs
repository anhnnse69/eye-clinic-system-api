using System.Security.Claims;
using ECS.Application.Common.Helpers;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetPatientProfilesForBookingServices
{
    /// <summary>
    /// Handles the business logic for retrieving accessible patient profiles formatted specifically for booking options.
    /// </summary>
    public class GetPatientProfilesForBookingService : IGetPatientProfilesForBookingService
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPatientProfilesForBookingService"/> class with required repositories and pipelines.
        /// </summary>
        /// <param name="patientProfileRepository">Repository boundary instance for querying physical patient profile records.</param>
        /// <param name="context">The underlying persistence database context unit mapping multi-entity relational state models.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public GetPatientProfilesForBookingService(
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientProfileRepository = patientProfileRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to evaluate identity scopes and return selectable profile options.
        /// </summary>
        /// <returns>An <see cref="ApiResponse{List{PatientProfileOption}}"/> enclosing descriptive state payloads for the selection UI.</returns>
        public async Task<ApiResponse<List<PatientProfileOption>>> Process()
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isDataScopeExist = true;

            // Step 1: Extract identity information parameter metrics from the active security claim session context
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Search operational relational databases to identify profiles bound tightly to the active account
            var accessibleProfileIds = await RetrieveAccessibleProfileIds(userId, isUserValid);

            // Step 3: Track context validation parameters safely before hitting core query pipelines
            ValidateDataContext(accessibleProfileIds, isUserValid, ref isDataScopeExist);

            // Step 4: Fetch relationship links between the user and active profile scopes
            var userPatientLinks = await FetchUserPatientRelationships(userId, isUserValid);

            // Step 5: Query underlying target physical data storage blocks executing evaluation algorithms
            var profiles = await ExecuteProfilesQuery(accessibleProfileIds, isDataScopeExist);

            // Step 6: Map internal domain state model attributes onto decoupled serialized response data schemas
            var result = MapToResponseDto(profiles, userPatientLinks);

            // Step 7: Evaluate processing parameters and package state structures dynamically to handle execution outcomes
            return CreateResponse(result, isUserValid, isDataScopeExist);
        }

        /// <summary>
        /// Resolves the logged-in user signature coordinates using synchronous token parsing streams.
        /// </summary>
        /// <param name="isUserValid">Output evaluation status updating to false if identification variables fail parsing matches.</param>
        /// <returns>The decoded user identity descriptor parameter identifier value.</returns>
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
        /// Leverages specialized access helpers to isolate unique profile identifier sequences linked directly to the target account.
        /// </summary>
        /// <param name="userId">The system identity identifier tracking target metrics variables.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>A collection matrix containing unique tracking primary keys linked directly onto the active context.</returns>
        private async Task<List<Guid>> RetrieveAccessibleProfileIds(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return new List<Guid>();

            return await PatientProfileAccessHelper.GetAccessibleProfileIdsAsync(
                userId, _patientProfileRepository, _context);
        }

        /// <summary>
        /// Validates whether the resolved identity boundary maps onto an accessible slice of target operational data structures.
        /// </summary>
        /// <param name="accessibleProfileIds">The allocated matrix holding allowed primary keys sequence strings.</param>
        /// <param name="isUserValid">Precondition evaluation flag indicating if user resolving succeeded.</param>
        /// <param name="isDataScopeExist">Flag updated to <c>false</c> if verification matches fail system benchmarks.</param>
        private void ValidateDataContext(List<Guid> accessibleProfileIds, bool isUserValid, ref bool isDataScopeExist)
        {
            if (!isUserValid) return;

            if (accessibleProfileIds == null || accessibleProfileIds.Count == 0)
            {
                isDataScopeExist = false;
            }
        }

        /// <summary>
        /// Extracts real-time structural metadata tracking parameters bounding multi-user connection records.
        /// </summary>
        /// <param name="userId">The logged-in user signature coordinate tracking identifier parameters.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>A structured list representing mapping context links between active users and physical patients data structures.</returns>
        private async Task<List<UserPatient>> FetchUserPatientRelationships(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return new List<UserPatient>();

            return await _context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Executes physical data store evaluations using structural sorting configurations.
        /// </summary>
        /// <param name="accessibleProfileIds">The context parameter tracking structural identifiers keys vector maps.</param>
        /// <param name="isDataScopeExist">Guard condition preventing database load pipelines on validation failure states.</param>
        /// <returns>A structured list of physical patient profile records matching core expressions.</returns>
        private async Task<List<PatientProfile>> ExecuteProfilesQuery(List<Guid> accessibleProfileIds, bool isDataScopeExist)
        {
            if (!isDataScopeExist) return new List<PatientProfile>();

            return await _patientProfileRepository
                .FindByCondition(x => accessibleProfileIds.Contains(x.Id), trackChanges: false)
                .OrderBy(x => x.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="data">The physical domain core model list array returned directly out of backend operational layers.</param>
        /// <param name="userPatientLinks">The allocated layout dictionary tracking interpersonal relationship definitions metadata.</param>
        /// <returns>A structured target presentation data payload collection instance context.</returns>
        private List<PatientProfileOption> MapToResponseDto(List<PatientProfile> data, List<UserPatient> userPatientLinks)
        {
            return data.Select(profile =>
            {
                var link = userPatientLinks
                    .FirstOrDefault(x => x.PatientId == profile.Id);

                string relationship = link?.Relationship ?? "Bản thân";

                return new PatientProfileOption
                {
                    Id = profile.Id,
                    FullName = profile.FullName,
                    Gender = profile.Gender.ToString(),
                    Dob = profile.Dob.ToString("dd/MM/yyyy"),
                    Relationship = relationship
                };
            })
            .ToList();
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="result">The structured data response payload list projected from database layers.</param>
        /// <param name="isUserValid">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="isDataScopeExist">Indicates data layer tracking context presence attributes.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<List<PatientProfileOption>> CreateResponse(
            List<PatientProfileOption> result,
            bool isUserValid,
            bool isDataScopeExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isDataScopeExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<PatientProfileOption>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }

        /// <summary>
        /// Evaluates structural tracking conditions matrix variables to issue systemized application error entries.
        /// </summary>
        /// <param name="isUserValid">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <param name="isDataScopeExist">The state checking indicator capturing contextual environment existence benchmarks.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<List<PatientProfileOption>>? CreateErrorResponse(bool isUserValid, bool isDataScopeExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<PatientProfileOption>>.Fail(
                    GeneralCode.APP_MESSAGE_4033.ToString()); // Unauthenticated/Invalid User
            }

            if (!isDataScopeExist)
            {
                return ApiResponse<List<PatientProfileOption>>.Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(),
                    new List<PatientProfileOption>());
            }

            return null;
        }
    }
}