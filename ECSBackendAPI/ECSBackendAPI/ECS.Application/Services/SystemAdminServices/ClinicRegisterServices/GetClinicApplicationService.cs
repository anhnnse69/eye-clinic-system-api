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

        private Expression<Func<ClinicRegistrationRequest, bool>> BuildFilterExpression(GetClinicApplicationsRequest request)
        {
            // 1. Tạo một query cơ bản mặc định luôn đúng
            Expression<Func<ClinicRegistrationRequest, bool>> filter = x => true;

            // 2. Từng bước kết hợp điều kiện lọc theo Trạng thái (Status) nếu Front-end truyền lên
            if (!string.IsNullOrEmpty(request.Status))
            {
                var statusUpper = request.Status.ToUpper();
                filter = x => x.Status == statusUpper;
            }

            // 3. Từng bước kết hợp điều kiện lọc theo Từ khóa tìm kiếm (SearchTerm)
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();

                // Lưu lại filter hiện tại để kết hợp với điều kiện search mới qua toán tử AND (&&)
                var currentFilter = filter;

                if (!string.IsNullOrEmpty(request.Status))
                {
                    // Nếu có cả Status và SearchTerm: lọc cả 2 điều kiện cùng lúc
                    var statusUpper = request.Status.ToUpper();
                    filter = x => x.Status == statusUpper &&
                                  (x.ClinicName.ToLower().Contains(search) || x.Id.ToString().ToLower().Contains(search));
                }
                else
                {
                    // Nếu chỉ có SearchTerm: lọc theo search term
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