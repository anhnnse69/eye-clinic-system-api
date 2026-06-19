using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices
{
    /// <summary>
    /// Handles business workflows for retrieving feedback history accessible to the authenticated patient.
    /// </summary>
    public class ViewMyFeedbackHistoryService : IViewMyFeedbackHistoryService
    {
        private readonly IRepositoryQueryBase<Feedback, Guid, AppDbContext> _feedbackRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewMyFeedbackHistoryService"/> class with required repositories,
        /// persistence context handlers, and authenticated user access pipelines.
        /// </summary>
        /// <param name="feedbackRepository">
        /// Repository abstraction responsible for querying persisted feedback domain entities.
        /// </param>
        /// <param name="patientProfileRepository">
        /// Repository abstraction used for resolving patient ownership mappings.
        /// </param>
        /// <param name="context">
        /// The underlying database context coordinating multi-entity relational state operations.
        /// </param>
        /// <param name="httpContextAccessor">
        /// Accessor used to retrieve authenticated user identity claims from the active HTTP request.
        /// </param>
        public ViewMyFeedbackHistoryService(
            IRepositoryQueryBase<Feedback, Guid, AppDbContext> feedbackRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _feedbackRepository = feedbackRepository;
            _patientProfileRepository = patientProfileRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the complete workflow for retrieving feedback history records available to the authenticated patient.
        /// </summary>
        /// <param name="request">
        /// Contains filtering criteria, keyword search parameters, and pagination configuration values.
        /// </param>
        /// <returns>
        /// An <see cref="ApiResponse{T}"/> wrapping feedback history records together with paging metadata.
        /// </returns>
        public async Task<ApiResponse<List<ViewMyFeedbackHistoryResponse>>> Process(
            ViewMyFeedbackHistoryRequest request)
        {
            // Initialize workflow validation state trackers
            bool isUserValid = true;
            bool isDataScopeExist = true;
            // Step 1: Resolve authenticated user identity information from current security claims context
            var userId = RetrieveUserId(ref isUserValid);
            // Step 2: Determine all patient profiles accessible by the current authenticated account
            var patientIds = await RetrieveAccessiblePatientIds(userId, isUserValid);
            // Step 3: Validate whether the resolved user has any accessible patient scope available
            ValidateDataContext(patientIds, isUserValid, ref isDataScopeExist);
            // Step 4: Construct dynamic query filtering expressions based on request criteria
            var filterExpression = BuildFilterExpression(patientIds, request, isDataScopeExist);
            // Step 5: Execute database queries with paging and filtering constraints applied
            var (feedbacks, totalRecords) = await ExecutePagedQuery(filterExpression, request, isDataScopeExist);
            // Step 6: Transform domain entities into response DTO projections
            var result = MapToResponseDto(feedbacks);
            // Step 7: Generate pagination metadata structure
            var meta = BuildPaginationMeta(request, totalRecords);
            // Step 8: Package final response payload according to workflow execution outcomes
            return CreateResponse(result, meta, isUserValid, isDataScopeExist);
        }

        /// <summary>
        /// Resolves the authenticated user identifier from security claim collections.
        /// </summary>
        /// <param name="isUserValid">
        /// Validation flag updated when identity extraction or parsing operations fail.
        /// </param>
        /// <returns>
        /// The authenticated user identifier if available; otherwise <see cref="Guid.Empty"/>.
        /// </returns>
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
        /// Retrieves all patient identifiers directly owned or linked to the authenticated account.
        /// </summary>
        /// <param name="userId">
        /// The authenticated user identifier.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether user identity validation completed successfully.
        /// </param>
        /// <returns>
        /// A collection containing unique patient identifiers accessible by the current user.
        /// </returns>
        private async Task<List<Guid>> RetrieveAccessiblePatientIds(
            Guid userId,
            bool isUserValid)
        {
            if (!isUserValid)
            {
                return new List<Guid>();
            }

            var directPatientIds = await _patientProfileRepository
                .FindByCondition(x => x.UserId == userId, false)
                .Select(x => x.Id)
                .ToListAsync();

            var linkedPatientIds = await _context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.PatientId)
                .ToListAsync();

            return directPatientIds
                .Union(linkedPatientIds)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Validates whether the authenticated user has any accessible patient scope available.
        /// </summary>
        /// <param name="patientIds">
        /// Collection of patient identifiers available to the current user.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether user validation completed successfully.
        /// </param>
        /// <param name="isDataScopeExist">
        /// Updated to false when no accessible patient records are available.
        /// </param>
        private void ValidateDataContext(
            List<Guid> patientIds,
            bool isUserValid,
            ref bool isDataScopeExist)
        {
            if (!isUserValid)
            {
                return;
            }

            if (!patientIds.Any())
            {
                isDataScopeExist = false;
            }
        }

        /// <summary>
        /// Builds a dynamic filtering expression for feedback retrieval operations.
        /// </summary>
        /// <param name="patientIds">
        /// Collection of accessible patient identifiers.
        /// </param>
        /// <param name="request">
        /// Request object containing search and paging criteria.
        /// </param>
        /// <param name="isDataScopeExist">
        /// Indicates whether a valid patient scope exists.
        /// </param>
        /// <returns>
        /// A LINQ expression used to filter feedback entities according to access scope and search criteria.
        /// </returns>
        private Expression<Func<Feedback, bool>> BuildFilterExpression(
            List<Guid> patientIds,
            ViewMyFeedbackHistoryRequest request,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist)
            {
                return x => false;
            }

            var searchTerm = request.SearchTerm?.Trim().ToLower();

            return x =>
                patientIds.Contains(x.PatientId)
                &&
                (
                    string.IsNullOrEmpty(searchTerm)
                    || x.Patient.FullName.ToLower().Contains(searchTerm)
                    || x.Doctor.User.FullName.ToLower().Contains(searchTerm)
                    || x.Clinic.Name.ToLower().Contains(searchTerm)
                    || (x.Comment != null
                        && x.Comment.ToLower().Contains(searchTerm))
                );
        }

        /// <summary>
        /// Executes feedback retrieval queries with filtering, sorting, and pagination applied.
        /// </summary>
        /// <param name="filterExpression">
        /// Dynamic filtering expression generated from request criteria.
        /// </param>
        /// <param name="request">
        /// Pagination and search configuration values.
        /// </param>
        /// <param name="isDataScopeExist">
        /// Indicates whether valid patient scope exists.
        /// </param>
        /// <returns>
        /// A tuple containing feedback records and total matching record count.
        /// </returns>
        private async Task<(List<Feedback>, int)> ExecutePagedQuery(
            Expression<Func<Feedback, bool>> filterExpression,
            ViewMyFeedbackHistoryRequest request,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist)
            {
                return (new List<Feedback>(), 0);
            }

            IQueryable<Feedback> query = _feedbackRepository
                .FindByCondition(filterExpression, false)
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                    .ThenInclude(x => x.User)
                .Include(x => x.Clinic)
                .Include(x => x.Appointment);

            var totalRecords = await query.CountAsync();

            var feedbacks = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (feedbacks, totalRecords);
        }

        /// <summary>
        /// Projects feedback domain entities into transport response DTO structures.
        /// </summary>
        /// <param name="feedbacks">
        /// Collection of feedback entities retrieved from persistence storage.
        /// </param>
        /// <returns>
        /// A collection of serialized feedback response models.
        /// </returns>
        private List<ViewMyFeedbackHistoryResponse> MapToResponseDto(
            List<Feedback> feedbacks)
        {
            return feedbacks.Select(x => new ViewMyFeedbackHistoryResponse
            {
                FeedbackId = x.Id.ToString(),
                AppointmentId = x.AppointmentId.ToString(),
                PatientName = x.Patient.FullName,
                DoctorName = x.Doctor.User.FullName,
                ClinicName = x.Clinic.Name,
                RatingDoctor = x.RatingDoctor,
                RatingClinic = x.RatingClinic,
                Comment = x.Comment,
                IsPublic = x.IsPublic,
                AppointmentDate = x.Appointment.AppointmentDate
                    .ToString("dd/MM/yyyy"),
                CreatedAt = x.CreatedAt
                    .ToString("dd/MM/yyyy HH:mm")
            })
            .ToList();
        }

        /// <summary>
        /// Creates pagination metadata describing the current query result boundaries.
        /// </summary>
        /// <param name="request">
        /// Request object containing paging configuration values.
        /// </param>
        /// <param name="totalRecords">
        /// Total number of records matching the specified criteria.
        /// </param>
        /// <returns>
        /// A populated pagination metadata structure.
        /// </returns>
        private MetaResponse BuildPaginationMeta(
            ViewMyFeedbackHistoryRequest request,
            int totalRecords)
        {
            return new MetaResponse(
                request.PageNumber,
                request.PageSize,
                totalRecords);
        }

        /// <summary>
        /// Creates the final API response payload based on workflow execution outcomes.
        /// </summary>
        /// <param name="result">
        /// Collection of feedback history response records.
        /// </param>
        /// <param name="meta">
        /// Pagination metadata associated with the query result.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether user identity validation completed successfully.
        /// </param>
        /// <param name="isDataScopeExist">
        /// Indicates whether an accessible patient scope exists.
        /// </param>
        /// <returns>
        /// A standardized API response payload containing feedback history information.
        /// </returns>
        private ApiResponse<List<ViewMyFeedbackHistoryResponse>> CreateResponse(
            List<ViewMyFeedbackHistoryResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isDataScopeExist)
        {
            var errorResponse = CreateErrorResponse(
                isUserValid,
                isDataScopeExist);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<ViewMyFeedbackHistoryResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Evaluates workflow validation states and generates standardized error responses when required.
        /// </summary>
        /// <param name="isUserValid">
        /// Indicates whether authenticated user information could be resolved successfully.
        /// </param>
        /// <param name="isDataScopeExist">
        /// Indicates whether the authenticated user has any accessible patient scope.
        /// </param>
        /// <returns>
        /// A standardized API response representing an error or empty-state result; otherwise null.
        /// </returns>
        private ApiResponse<List<ViewMyFeedbackHistoryResponse>>? CreateErrorResponse(
            bool isUserValid,
            bool isDataScopeExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<ViewMyFeedbackHistoryResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isDataScopeExist)
            {
                return ApiResponse<List<ViewMyFeedbackHistoryResponse>>.Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(),
                    new List<ViewMyFeedbackHistoryResponse>(),
                    new MetaResponse(1, 10, 0));
            }

            return null;
        }
    }
}