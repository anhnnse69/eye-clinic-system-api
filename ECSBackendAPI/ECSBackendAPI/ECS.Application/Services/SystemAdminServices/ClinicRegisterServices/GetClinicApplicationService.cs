using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace ECS.Application.Services.SystemAdminServices.ClinicRegisterServices
{
    /// <summary>
    /// Handles the business logic for retrieving and filtering clinic registration applications.
    /// </summary>
    public class GetClinicApplicationService : IGetClinicApplicationService
    {
        private readonly IRepositoryQueryBase<ClinicRegistrationRequest, Guid, AppDbContext> _queryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GetClinicApplicationService"/> with required dependencies.
        /// </summary>
        /// <param name="queryRepo">Repository for querying clinic registration requests.</param>
        public GetClinicApplicationService(IRepositoryQueryBase<ClinicRegistrationRequest, Guid, AppDbContext> queryRepo)
        {
            _queryRepo = queryRepo;
        }

        /// <summary>
        /// Processes the query request by filtering, sorting, and paginating the clinic applications.
        /// </summary>
        /// <param name="request">The request containing search filter metrics.</param>
        /// <returns>An <see cref="ApiResponse{List{GetClinicApplicationResponse}}"/> containing the formatted paged list.</returns>
        public async Task<ApiResponse<List<GetClinicApplicationResponse>>> Process(GetClinicApplicationsRequest request)
        {
            // Build filter criteria based ONLY on SearchTerm (Status removed)
            var filterExpression = BuildFilterExpression(request);
            // Fetch sorted and paginated application query results from storage
            var pagedQuery = ExecutePagedQuery(filterExpression, request, out int totalRecords);
            // Map database entities onto UI responsive structures
            var formattedList = MapToResponseDto(pagedQuery);
            // Construct unified meta response wrapper
            var paginationMeta = BuildPaginationMeta(request, totalRecords);
            // Return standardized API envelope response
            return CreateApiResponse(formattedList, paginationMeta);
        }

        /// <summary>
        /// Builds the filter expression for clinic registration requests using optional status and search term criteria.
        /// </summary>
        private Expression<Func<ClinicRegistrationRequest, bool>> BuildFilterExpression(GetClinicApplicationsRequest request)
        {
            Expression<Func<ClinicRegistrationRequest, bool>> filter = x => true;
            var status = request.Status;
            var searchTerm = request.SearchTerm;
            var hasStatus = !string.IsNullOrEmpty(status);
            var hasSearchTerm = !string.IsNullOrEmpty(searchTerm);
            if (hasStatus)
            {
                var statusUpper = status!.ToUpper();
                filter = x => x.Status == statusUpper;
            }
            if (hasSearchTerm)
            {
                var search = searchTerm!.ToLower();
                if (hasStatus)
                {
                    var statusUpper = status!.ToUpper();
                    filter = x => x.Status == statusUpper &&
                                  (x.ClinicName.ToLower().Contains(search) || x.Id.ToString().ToLower().Contains(search));
                }
                else
                {
                    filter = x => x.ClinicName.ToLower().Contains(search) || x.Id.ToString().ToLower().Contains(search);
                }
            }
            return filter;
        }

        /// <summary>
        /// Applies query projections, strict sorting rules, and custom offsets onto the active data dataset.
        /// </summary>
        private List<ClinicRegistrationRequest> ExecutePagedQuery(
            Expression<Func<ClinicRegistrationRequest, bool>> filterExpression,
            GetClinicApplicationsRequest request,
            out int totalRecords)
        {
            var baseQuery = _queryRepo.FindByCondition(filterExpression, trackChanges: false);
            baseQuery = baseQuery.OrderByDescending(x => x.RequestedAt);

            totalRecords = baseQuery.Count();

            return baseQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }

        /// <summary>
        /// Maps internal data persistence items onto explicit outbound properties.
        /// </summary>
        private List<GetClinicApplicationResponse> MapToResponseDto(List<ClinicRegistrationRequest> data)
        {
            return data.Select(app => new GetClinicApplicationResponse
            {
                Id_clinic_registration = app.Id.ToString(),
                ClinicName = app.ClinicName,
                ContactEmail = app.ContactEmail,
                ContactPhone = app.ContactPhone,
                SubmissionDate = app.RequestedAt.ToString("dd/MM/yyyy"),
                Status = app.Status
            }).ToList();
        }

        /// <summary>
        /// Creates the unified metadata pagination wrapper.
        /// </summary>
        private MetaResponse BuildPaginationMeta(GetClinicApplicationsRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Packages mapped data objects into the official ApiResponse wrapper structure using global tracking codes.
        /// </summary>
        private ApiResponse<List<GetClinicApplicationResponse>> CreateApiResponse(
            List<GetClinicApplicationResponse> resultList,
            MetaResponse meta)
        {
            return ApiResponse<List<GetClinicApplicationResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                resultList,
                meta
            );
        }
    }
}