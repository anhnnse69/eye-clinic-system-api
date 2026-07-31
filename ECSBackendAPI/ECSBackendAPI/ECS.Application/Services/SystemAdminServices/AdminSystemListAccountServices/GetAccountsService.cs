using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemListAccountServices
{
    /// <summary>
    /// Handles system administrator contextual query pipeline operations to list, search, and map physical database core users.
    /// </summary>
    public class GetAccountsService : IGetAccountsService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="GetAccountsService"/> with mandatory infrastructure abstraction layers.
        /// </summary>
        /// <param name="userRepository">Repository layout boundary interface querying user entity collections.</param>
        /// <param name="httpContextAccessor">Accessor layer retrieving authentication identity parameters out of HTTP security claims contexts.</param>
        public GetAccountsService(
            IRepositoryQueryBase<User, Guid, AppDbContext> userRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to parse filters, evaluate operational bounds, and return paginated data collections.
        /// </summary>
        /// <param name="request">The parameters containing data filters, keywords, and explicit pagination criteria details.</param>
        /// <returns>An <see cref="ApiResponse{List{GetAccountResponse}}"/> enclosing descriptive state payloads alongside metadata boundaries.</returns>
        public async Task<ApiResponse<List<GetAccountResponse>>> Process(GetAccountsRequest request)
        {
            // Step 1: Initialize status tracking flags guarding active workflow tracking configurations
            bool isCurrentAdminValid = true;

            // Step 2: Extract identity context coordination metrics parameters out of current security token collections
            var currentUserId = RetrieveAdminUserId(out isCurrentAdminValid);

            // Step 3: Validate authorization contexts verifying active user holds permission privileges
            isCurrentAdminValid = ValidateAdminAuthorization(currentUserId, isCurrentAdminValid);

            // Step 4: Synthesize dynamic criteria query lambda expressions matching constraint arguments
            var filterExpression = BuildFilterExpression(request);

            // Step 5: Query storage layout collections executing sequential data paging tracking bounds algorithms
            var (users, totalRecords) = await ExecutePagedQuery(filterExpression, request);

            // Step 6: Map internal physical domain model attributes securely onto business representation response schemas
            var result = MapToResponseDto(users);

            // Step 7: Formulate tracking index page segment pagination wrappers leveraging default metadata parameters
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Package contextual envelope outcome payload structures dynamically to conclude processing pipelines
            return CreateResponse(result, meta, isCurrentAdminValid);
        }

        /// <summary>
        /// Decodes token claims streams to retrieve the running operator unique identifier parameter context.
        /// </summary>
        /// <param name="isCurrentAdminValid">Output execution validation state flag tracking authorization validity.</param>
        /// <returns>The decoded operator unique user signature tracking identifier.</returns>
        private Guid RetrieveAdminUserId(out bool isCurrentAdminValid)
        {
            isCurrentAdminValid = true;
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid adminId))
            {
                isCurrentAdminValid = false;
                return Guid.Empty;
            }
            return adminId;
        }

        /// <summary>
        /// Validates if the operation identity tracking parameters possess required administrative system authorization capabilities.
        /// </summary>
        /// <param name="adminId">The extracted unique structural user reference key identifier.</param>
        /// <param name="isCurrentAdminValid">Current flag metric indicating preceding parsing stage outcome states.</param>
        /// <returns>An updated status metric evaluating authorization validity boundaries.</returns>
        private bool ValidateAdminAuthorization(Guid adminId, bool isCurrentAdminValid)
        {
            if (!isCurrentAdminValid)
            {
                return false;
            }

            var roleClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.Role)?
                .Value;

            if (roleClaim != UserRole.SYSTEM_ADMIN.ToString())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Assembles flexible database filter condition maps according to input keywords and parameter options.
        /// </summary>
        /// <param name="request">The data block structure packing filtering criteria conditions keys.</param>
        /// <returns>A structured target expression lambda condition constraint mapping rule block.</returns>
        private Expression<Func<User, bool>> BuildFilterExpression(GetAccountsRequest request)
        {
            var searchTerm = request.SearchTerm?.Trim().ToLower();

            return x =>
                x.Role != UserRole.SYSTEM_ADMIN
                && (!request.Role.HasValue || x.Role == request.Role.Value)
                && (!request.IsActive.HasValue || x.IsActive == request.IsActive.Value)
                && (!request.ClinicId.HasValue || (x.StaffClinics != null && x.StaffClinics.Any(sc => sc.ClinicId == request.ClinicId.Value)))
                && (string.IsNullOrEmpty(searchTerm)
                    || x.FullName.ToLower().Contains(searchTerm)
                    || x.Phone.ToLower().Contains(searchTerm));
        }

        /// <summary>
        /// Interacts against underlying technical persistence adapters explicitly enforcing type bindings for query optimizations.
        /// </summary>
        /// <param name="filterExpression">The operational logic search matrix constraint condition.</param>
        /// <param name="request">The structural pagination setup boundary tracking configuration settings.</param>
        /// <returns>A composite tuple value wrapping matched entity instance arrays alongside absolute total row bounds metrics.</returns>
        private async Task<(List<User> Users, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<User, bool>> filterExpression,
            GetAccountsRequest request)
        {
            var query = _userRepository
                .FindByCondition(filterExpression, trackChanges: false);

            var totalRecords = await query.CountAsync<User>();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync<User>();

            return (items, totalRecords);
        }

        /// <summary>
        /// Projects physical persistence collection structures directly onto target representation contract records.
        /// </summary>
        /// <param name="data">The physical entity domain core model array array rows source object.</param>
        /// <returns>A decoupled collection array representing presentation tracking response structures.</returns>
        private List<GetAccountResponse> MapToResponseDto(List<User> data)
        {
            return data.Select(user => new GetAccountResponse
            {
                // Mapping physical record primary system registration key onto string representations
                Id = user.Id.ToString(),
                // Transporting contact phone configuration properties details directly
                Phone = user.Phone,
                // Assigning optional messaging address details securely
                Email = user.Email,
                // Populating actual naming identifier strings context parameters
                FullName = user.FullName,
                // Converting system categorization role enumeration states safely onto string formats
                Role = user.Role.ToString(),
                // Transporting lifecycle status parameters directly
                IsActive = user.IsActive,
                // Projecting optional avatar asset link targets location entries
                AvatarUrl = user.AvatarUrl,
                // Formatting persistent calendar establishment dates parameters using specific presentation masks
                CreatedAt = user.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();
        }

        /// <summary>
        /// Instantiates metadata tracking component configurations using provided constructor definitions.
        /// </summary>
        /// <param name="request">The container packing target layout query segmentation constraints attributes.</param>
        /// <param name="totalRecords">The absolute record volume bounds count resolved by system queries.</param>
        /// <returns>A populated structural pagination tracking component configuration mapping reference context.</returns>
        private MetaResponse BuildPaginationMeta(GetAccountsRequest request, int totalRecords)
        {
            return new MetaResponse(
                request.PageNumber,
                request.PageSize,
                totalRecords);
        }

        /// <summary>
        /// Assesses systemized application indicator flags to construct failed tracking capsules or successful transfer metadata packages.
        /// </summary>
        /// <param name="result">The projected presentation data object list mapped out of database rows.</param>
        /// <param name="meta">The pagination tracking metadata allocation component model context parameters.</param>
        /// <param name="isCurrentAdminValid">Validation monitor parameter mapping administrative access privileges check status.</param>
        /// <returns>A generic application response payload configuration container initialized appropriately.</returns>
        private ApiResponse<List<GetAccountResponse>> CreateResponse(
            List<GetAccountResponse> result,
            MetaResponse meta,
            bool isCurrentAdminValid)
        {
            var errorResponse = CreateErrorResponse(isCurrentAdminValid);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<GetAccountResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Assesses error condition flags to return systemic application failure schemas.
        /// </summary>
        /// <param name="isCurrentAdminValid">Indicates if the administrative operational authorization remains verified intact.</param>
        /// <returns>A failed API payload package if constraint checks fail; otherwise null.</returns>
        private ApiResponse<List<GetAccountResponse>>? CreateErrorResponse(bool isCurrentAdminValid)
        {
            if (!isCurrentAdminValid)
            {
                return ApiResponse<List<GetAccountResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4014.ToString());
            }
            return null;
        }
    }
}