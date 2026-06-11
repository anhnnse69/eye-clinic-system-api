using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace ECS.Application.Services.SystemAdminServices.ClinicManagementServices
{
    /// <summary>
    /// Handles the business logic for retrieving, filtering, and paging the master list of registered clinics.
    /// </summary>
    public class GetClinicsService : IGetClinicsService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _queryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GetClinicsService"/> with required database query context.
        /// </summary>
        /// <param name="queryRepo">Repository for querying clinic persistence data.</param>
        public GetClinicsService(IRepositoryQueryBase<Clinic, Guid, AppDbContext> queryRepo)
        {
            _queryRepo = queryRepo;
        }

        /// <summary>
        /// Processes the query request by filtering by active status or text identifiers without containing direct inline IF blocks.
        /// </summary>
        /// <param name="request">The request parameter containing page sizes and filtering tokens.</param>
        /// <returns>A standardized api response wrapper enveloping the list of response dtos.</returns>
        public async Task<ApiResponse<List<GetClinicResponse>>> Process(GetClinicsRequest request)
        {
            // 1. Build filter expressions dynamically 
            var filterExpression = BuildFilterExpression(request);
            // 2. Fetch sorted and paginated dataset directly from db infrastructure
            var pagedQuery = ExecutePagedQuery(filterExpression, request, out int totalRecords);
            // 3. Map infrastructure model definitions to exact UI contract structure
            var formattedList = MapToResponseDto(pagedQuery);
            // 4. Wrap up pagination metrics
            var paginationMeta = BuildPaginationMeta(request, totalRecords);
            // 5. Encapsulate into standardized communication response envelope
            return CreateApiResponse(formattedList, paginationMeta);
        }

        /// <summary>
        /// Compiles dynamic expressions using predicate filtering criteria based on UI mapping demands.
        /// </summary>
        /// <param name="request">The clinic filter arguments containing search keywords and target status.</param>
        /// <returns>A linq expression tree representing the cumulative search criteria predicate.</returns>
        private Expression<Func<Clinic, bool>> BuildFilterExpression(GetClinicsRequest request)
        {
            Expression<Func<Clinic, bool>> filter = x => true;
            var status = request.Status;
            var searchTerm = request.SearchTerm;
            var hasStatus = !string.IsNullOrEmpty(status);
            var hasSearchTerm = !string.IsNullOrEmpty(searchTerm);
            if (hasStatus)
            {
                // Mapping UI ACTIVE/INACTIVE strings onto persistence Boolean fields
                bool targetActiveState = status!.ToUpper() == "ACTIVE";
                filter = x => x.IsActive == targetActiveState;
            }
            if (hasSearchTerm)
            {
                var search = searchTerm!.ToLower();
                if (hasStatus)
                {
                    bool targetActiveState = status!.ToUpper() == "ACTIVE";
                    filter = x => x.IsActive == targetActiveState &&
                                  (x.Name.ToLower().Contains(search) || x.Id.ToString().ToLower().Contains(search));
                }
                else
                {
                    filter = x => x.Name.ToLower().Contains(search) || x.Id.ToString().ToLower().Contains(search);
                }
            }
            return filter;
        }

        /// <summary>
        /// Executes querying, strict descending ordering based on creation timeline, and offsets processing.
        /// </summary>
        /// <param name="filterExpression">The compiled lambda filtering filter expression tree.</param>
        /// <param name="request">Paging metrics indicating target index and record boundary sizes.</param>
        /// <param name="totalRecords">Output parameter detailing the aggregate filtered record volume prior to slicing.</param>
        /// <returns>A segmented list of clinic records pulled out from data store context.</returns>
        private List<Clinic> ExecutePagedQuery(
            Expression<Func<Clinic, bool>> filterExpression,
            GetClinicsRequest request,
            out int totalRecords)
        {
            var baseQuery = _queryRepo.FindByCondition(filterExpression, trackChanges: false);
            // Newest clinics display on top
            baseQuery = baseQuery.OrderByDescending(x => x.CreatedAt);
            totalRecords = baseQuery.Count();
            return baseQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }

        /// <summary>
        /// Maps domain entities attributes into clean presentation structural items.
        /// </summary>
        /// <param name="data">The list of domain database objects collected from infrastructural operations.</param>
        /// <returns>A collection of structural payload objects compiled specifically for front-end interface consumption.</returns>
        private List<GetClinicResponse> MapToResponseDto(List<Clinic> data)
        {
            return data.Select(clinic => new GetClinicResponse
            {
                Id_clinic = clinic.Id.ToString(),
                ClinicName = clinic.Name,
                Address = clinic.Address,
                // If phone/email is empty strings in DB from minimal configuration, falls back to standard placeholder
                ContactPhone = string.IsNullOrEmpty(clinic.Phone) ? "N/A" : clinic.Phone,
                ContactEmail = string.IsNullOrEmpty(clinic.Email) ? "N/A" : clinic.Email,
                CreatedAt = clinic.CreatedAt.ToString("dd/MM/yyyy"),
                // Conversions back into exact text labels expected by the React state engine
                Status = clinic.IsActive ? "ACTIVE" : "INACTIVE"
            }).ToList();
        }

        /// <summary>
        /// Generates tracking metadata layout.
        /// </summary>
        /// <param name="request">The underlying data metrics detailing pagination requested sizes.</param>
        /// <param name="totalRecords">The absolute record volume matching the lookup criteria.</param>
        /// <returns>A tracking <see cref="MetaResponse"/> element incorporating paging bounds definitions.</returns>
        private MetaResponse BuildPaginationMeta(GetClinicsRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Finishes transaction envelope mapping with tracking standard response code.
        /// </summary>
        /// <param name="resultList">The processed presentation collection intended for display layers.</param>
        /// <param name="meta">The supplementary database offset layout statistics.</param>
        /// <returns>A final fully enveloped <see cref="ApiResponse{T}"/> wrapper object tagged with transaction metadata signals.</returns>
        private ApiResponse<List<GetClinicResponse>> CreateApiResponse(
            List<GetClinicResponse> resultList,
            MetaResponse meta)
        {
            return ApiResponse<List<GetClinicResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                resultList,
                meta
            );
        }
    }
}