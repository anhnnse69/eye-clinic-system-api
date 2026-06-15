using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices
{
    /// <summary>
    /// Handles the business logic for retrieving, filtering, and paging profiles tied directly to the authorized patient account.
    /// </summary>
    public class GetPatientProfilesService : IGetPatientProfilesService
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPatientProfilesService"/> class with required repositories and claims pipelines.
        /// </summary>
        /// <param name="patientProfileRepository">Repository boundary instance for querying physical patient profile records.</param>
        /// <param name="context">The underlying persistence database context unit mapping multi-entity relational state models.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public GetPatientProfilesService(
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientProfileRepository = patientProfileRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to parse filters, evaluate identity scopes, and return data.
        /// </summary>
        /// <param name="request">The parameters containing data filters, search terms, and explicit pagination criteria details.</param>
        /// <returns>An <see cref="ApiResponse{List{GetPatientProfileResponse}}"/> enclosing descriptive state payloads alongside metadata boundaries.</returns>
        public async Task<ApiResponse<List<GetPatientProfileResponse>>> Process(GetPatientProfilesRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isDataScopeExist = true;

            // Step 1: Extract identity information parameter metrics from the active security claim session context
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Search operational relational databases to identify profiles bound tightly to the active account
            var accessibleProfileIds = await RetrieveLinkedProfileIds(userId, isUserValid);

            // Step 3: Track context validation parameters safely before hitting core query pipelines
            ValidateDataContext(accessibleProfileIds, isUserValid, ref isDataScopeExist);

            // Step 4: Synthesize a flexible criteria lambda matrix expression using dynamic criteria logic mapping
            var filterExpression = BuildFilterExpression(accessibleProfileIds, request, isDataScopeExist);

            // Step 5: Query underlying target physical data storage blocks executing paged evaluation algorithms
            var (profiles, totalRecords) = await ExecutePagedQuery(filterExpression, request, isDataScopeExist);

            // Step 6: Map internal domain state model attributes onto decoupled serialized response data schemas
            var userPatientLinks = await FetchUserPatientRelationships(userId, isUserValid);
            var result = MapToResponseDto(profiles, userId, userPatientLinks);

            // Step 7: Package contextual index layout tracker parameters to formulate pagination tracking wrappers
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Evaluate processing parameters and package state structures dynamically to handle execution outcomes
            return CreateResponse(result, meta, isUserValid, isDataScopeExist);
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
        /// Probes multiple table associations to isolate unique profile identifier sequences linked directly to the target account.
        /// </summary>
        /// <param name="userId">The system identity identifier tracking target metrics variables.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>A collection matrix containing unique tracking primary keys linked directly onto the active context.</returns>
        private async Task<List<Guid>> RetrieveLinkedProfileIds(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return new List<Guid>();

            var directProfileIds = await _patientProfileRepository
                .FindByCondition(x => x.UserId == userId, trackChanges: false)
                .Select(x => x.Id)
                .ToListAsync();

            var linkedProfileIds = await _context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.PatientId)
                .ToListAsync();

            return directProfileIds.Union(linkedProfileIds).Distinct().ToList();
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

            if (accessibleProfileIds == null || !accessibleProfileIds.Any())
            {
                isDataScopeExist = false;
            }
        }

        /// <summary>
        /// Assembles dynamic lambda filter structures mapping request parameters against entity state conditions.
        /// </summary>
        /// <param name="accessibleProfileIds">The context parameter tracking structural identifiers keys vector maps.</param>
        /// <param name="request">The structural entity containing filtering keys and generic search parameters.</param>
        /// <param name="isDataScopeExist">Guard execution flag assessing baseline tracking criteria states.</param>
        /// <returns>A composite queryable logic filter expression block.</returns>
        private Expression<Func<PatientProfile, bool>> BuildFilterExpression(
            List<Guid> accessibleProfileIds,
            GetPatientProfilesRequest request,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist || accessibleProfileIds == null || !accessibleProfileIds.Any())
            {
                return x => false; // Fallback expression returning zero database row hits safely
            }

            var searchTerm = request.SearchTerm?.Trim().ToLower();

            return x =>
                accessibleProfileIds.Contains(x.Id)
                && (string.IsNullOrEmpty(searchTerm)
                    || x.FullName.ToLower().Contains(searchTerm)
                    || (x.IdentityNumber != null && x.IdentityNumber.ToLower().Contains(searchTerm))
                    || (x.PhoneNumber != null && x.PhoneNumber.ToLower().Contains(searchTerm)));
        }

        /// <summary>
        /// Executes physical data store evaluations using explicit transactional tracking skips and page index limit controls.
        /// </summary>
        /// <param name="filterExpression">The structured operational logic filter condition matrix maps.</param>
        /// <param name="request">The data container tracking layout configuration bounds.</param>
        /// <param name="isDataScopeExist">Guard condition preventing database load pipelines on validation failure states.</param>
        /// <returns>A tuple pairing structural result detail item list rows with overall record aggregate bounds integers.</returns>
        private async Task<(List<PatientProfile> Profiles, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<PatientProfile, bool>> filterExpression,
            GetPatientProfilesRequest request,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist)
            {
                return (new List<PatientProfile>(), 0);
            }

            IQueryable<PatientProfile> query = _patientProfileRepository
                .FindByCondition(filterExpression, trackChanges: false);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalRecords);
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
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="data">The physical domain core model list array returned directly out of backend operational layers.</param>
        /// <param name="currentUserId">The unique tracker reference coordinates mapping active context identifiers tokens.</param>
        /// <param name="userPatientLinks">The allocated layout dictionary tracking interpersonal relationship definitions metadata.</param>
        /// <returns>A structured target presentation data payload collection instance context.</returns>
        private List<GetPatientProfileResponse> MapToResponseDto(
            List<PatientProfile> data,
            Guid currentUserId,
            List<UserPatient> userPatientLinks)
        {
            return data.Select(profile =>
            {
                var link = userPatientLinks
                    .FirstOrDefault(x => x.PatientId == profile.Id);

                string relationship = link?.Relationship ?? "Bản thân";

                return new GetPatientProfileResponse
                {
                    Id_patientProfile = profile.Id.ToString(),
                    FullName = profile.FullName,
                    Gender = profile.Gender.ToString(),
                    Dob = profile.Dob.ToString("dd/MM/yyyy"),
                    IdentityNumber = string.IsNullOrWhiteSpace(profile.IdentityNumber) ? "N/A" : profile.IdentityNumber,
                    PhoneNumber = string.IsNullOrWhiteSpace(profile.PhoneNumber) ? "N/A" : profile.PhoneNumber,
                    Relationship = relationship,
                    CreatedAt = profile.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                };
            })
            .ToList();
        }

        /// <summary>
        /// Assembles pagination envelope metrics parameters using custom constructor signatures.
        /// </summary>
        /// <param name="request">The tracking item block mapping target layout page configurations.</param>
        /// <param name="totalRecords">The quantified data baseline row count metrics context.</param>
        /// <returns>A populated metadata envelope serialization module component instance.</returns>
        private MetaResponse BuildPaginationMeta(GetPatientProfilesRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="result">The structured data response payload list projected from database layers.</param>
        /// <param name="meta">The pagination metadata structure layout configuration parameters.</param>
        /// <param name="isUserValid">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="isDataScopeExist">Indicates data layer tracking context presence attributes.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<List<GetPatientProfileResponse>> CreateResponse(
            List<GetPatientProfileResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isDataScopeExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isDataScopeExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<GetPatientProfileResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Evaluates structural tracking conditions matrix variables to issue systemized application error entries.
        /// </summary>
        /// <param name="isUserValid">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <param name="isDataScopeExist">The state checking indicator capturing contextual environment existence benchmarks.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<List<GetPatientProfileResponse>>? CreateErrorResponse(bool isUserValid, bool isDataScopeExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<GetPatientProfileResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString()); // Unauthenticated/Invalid User
            }

            if (!isDataScopeExist)
            {
                return ApiResponse<List<GetPatientProfileResponse>>.Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(),
                    new List<GetPatientProfileResponse>(),
                    new MetaResponse(1, 10, 0));
            }

            return null;
        }
    }
}