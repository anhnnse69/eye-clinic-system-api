using System.Security.Claims;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices
{
    /// <summary>
    /// Handles the operational flow to retrieve, filter, and paginate services assigned to a managed clinic profile.
    /// </summary>
    public class ViewClinicServicesService : IViewClinicServicesService
    {
        private readonly IRepositoryQueryBase<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewClinicServicesService"/> with infrastructure querying components.
        /// </summary>
        /// <param name="serviceRepository">Repository boundary managing clinic service storage matrices.</param>
        /// <param name="staffClinicRepository">Repository resolving relationships mapped between staff units and target clinics.</param>
        /// <param name="httpContextAccessor">Accessor extracting system claim structures from live security contexts.</param>
        public ViewClinicServicesService(
            IRepositoryQueryBase<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _serviceRepository = serviceRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Executes the sequential pipeline data workflow process to load clinic services based on admin account limits.
        /// </summary>
        /// <param name="request">The view clinic data criteria and explicit pagination metrics parameter details.</param>
        /// <returns>A unified standard envelope tracking result execution response block with metadata boundaries.</returns>
        public async Task<ApiResponse<List<ViewClinicServiceResponse>>> Process(ViewClinicServicesRequest request)
        {
            // Step 1: Initialize operational state tracking verification indicators
            bool isUserValid = true;

            // Step 2: Extract identity coordinate signatures from current active token context sessions
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Query the system to isolate the unique Clinic ID linked with the validated administrator account
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Synthesize dynamic lambda filter logic structures mapping input queries against target models
            var filterExpression = BuildFilterExpression(clinicId ?? Guid.Empty, request);

            // Step 5: Query the persistence store utilizing explicit pagination skipping algorithms to return matched listings
            var (services, totalRecords) = await ExecutePagedQuery(filterExpression, request);

            // Step 6: Map physical core domain model attribute collections onto decoupled serialized presentation DTO rows
            var result = MapToResponseDto(services);

            // Step 7: Build structural layout tracker indices parameters to formulate standard pagination wrappers
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Evaluate processing metrics and package structural states to deliver output endpoints safely
            return CreateResponse(result, meta, isUserValid, clinicId.HasValue);
        }

        /// <summary>
        /// Parses the current context claim identity structures to decode user identification tokens.
        /// </summary>
        /// <param name="isUserValid">Output flag updating to false if context parameters fail identity parsing.</param>
        /// <returns>The unique identifier key tracking the system account if successful; otherwise Guid.Empty.</returns>
        private Guid RetrieveUserId(out bool isUserValid)
        {
            isUserValid = true;
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
        /// Probes connection mapping rows to deduce the assigned environment bound to the manager.
        /// </summary>
        /// <param name="userId">The unique identifier tracking system account identities.</param>
        /// <param name="isUserValid">Flag guarding database access states.</param>
        /// <returns>The targeted clinic key sequence token if found; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            var staffClinic = await _staffClinicRepository
                .FindByCondition(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .FirstOrDefaultAsync();

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Combines conditional parameters dynamically into standardized queryable boolean filters.
        /// </summary>
        /// <param name="clinicId">The clinic contextual identification reference code filter token.</param>
        /// <param name="request">The data container defining search words and boolean tracking statuses.</param>
        /// <returns>A composite queryable system logic expression matrix block.</returns>
        private Expression<Func<Service, bool>> BuildFilterExpression(Guid clinicId, ViewClinicServicesRequest request)
        {
            var searchTerm = request.SearchTerm?.Trim().ToLower();

            return x =>
                x.ClinicId == clinicId
                && (!request.IsActive.HasValue || x.IsActive == request.IsActive.Value)
                && (string.IsNullOrEmpty(searchTerm) || x.ServiceName.ToLower().Contains(searchTerm));
        }

        /// <summary>
        /// Queries the physical storage executing precise skip and take logic routines with change tracking disabled.
        /// </summary>
        /// <param name="filterExpression">The structured operational logic filtering criteria.</param>
        /// <param name="request">The layout parameter bounding target query indices constraints.</param>
        /// <returns>A composite tuple pairing localized service item records with the overall matching records integer.</returns>
        private async Task<(List<Service> Services, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<Service, bool>> filterExpression,
            ViewClinicServicesRequest request)
        {
            var query = _serviceRepository.FindByCondition(filterExpression, trackChanges: false);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.ServiceName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        /// <summary>
        /// Projects database entity list attributes directly across structural transport model wrappers.
        /// </summary>
        /// <param name="data">The collections array holding database structures fetched from the data layers.</param>
        /// <returns>A serialized presentation data collection instance context matching front-end expectations.</returns>
        private List<ViewClinicServiceResponse> MapToResponseDto(List<Service> data)
        {
            return data.Select(srv => new ViewClinicServiceResponse
            {
                // Converting primary relational database keys into unique serialization text formats
                Id_service = srv.Id.ToString(),
                // Transporting core designation strings directly to prevent metadata loss
                ServiceName = srv.ServiceName,
                // Assigning standard pricing values ensuring safe handling for precision indicators
                Price = srv.Price,
                // Assigning specific timeline blocks tracking operation resource constraints
                DurationMinutes = srv.DurationMinutes,
                // Passing active deployment flags determining overall visibility statuses
                IsActive = srv.IsActive
            }).ToList();
        }

        /// <summary>
        /// Populates standard structural pagination metadata tracking descriptors.
        /// </summary>
        /// <param name="request">The parameters carrying desired configuration sizing layout matrices.</param>
        /// <param name="totalRecords">The absolute structural row volume captured by current constraint parameters.</param>
        /// <returns>A populated metadata envelope serialization module component instance.</returns>
        private MetaResponse BuildPaginationMeta(ViewClinicServicesRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Packages execution results securely into appropriate standard API transportation envelopes.
        /// </summary>
        /// <param name="result">The structured application payload rows mapped out of persistent records.</param>
        /// <param name="meta">The pagination metrics tracking indices data parameters.</param>
        /// <param name="isUserValid">Indicates context identity parsing success conditions.</param>
        /// <param name="isClinicExist">Indicates target domain clinic validation presence attributes.</param>
        /// <returns>A configured standard API application envelope ready for transport translation layers.</returns>
        private ApiResponse<List<ViewClinicServiceResponse>> CreateResponse(
            List<ViewClinicServiceResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<ViewClinicServiceResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Screens pipeline conditional flag states to issue systemized operational failure configurations.
        /// </summary>
        /// <param name="isUserValid">Indicates if identification data context parsed matching parameters successfully.</param>
        /// <param name="isClinicExist">Indicates whether a parent clinic entity relationship map could be verified.</param>
        /// <returns>A failure status description container if conditions are broken; otherwise null properties.</returns>
        private ApiResponse<List<ViewClinicServiceResponse>>? CreateErrorResponse(bool isUserValid, bool isClinicExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<ViewClinicServiceResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<List<ViewClinicServiceResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}