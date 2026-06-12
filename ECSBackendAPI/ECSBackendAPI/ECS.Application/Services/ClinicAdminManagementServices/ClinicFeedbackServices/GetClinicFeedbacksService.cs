using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices
{
    /// <summary>
    /// Handles the business logic for retrieving, filtering, and paging feedbacks inside a specific clinic.
    /// </summary>
    public class GetClinicFeedbacksService : IGetClinicFeedbacksService
    {
        private readonly IRepositoryQueryBase<Feedback, Guid, AppDbContext> _feedbackRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetClinicFeedbacksService"/> class with required infrastructure dependencies.
        /// </summary>
        /// <param name="feedbackRepository">Repository boundary instance for querying underlying domain feedback records.</param>
        /// <param name="staffClinicRepository">Repository boundary instance managing system staff-to-clinic contextual relation rows.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public GetClinicFeedbacksService(
            IRepositoryQueryBase<Feedback, Guid, AppDbContext> feedbackRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _feedbackRepository = feedbackRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to parse filters, evaluate operational bounds, and return paginated feedback data.
        /// </summary>
        /// <param name="request">The parameters containing data filters, ratings, and explicit pagination criteria details.</param>
        /// <returns>An <see cref="ApiResponse{List{GetClinicFeedbackResponse}}"/> enclosing descriptive state payloads alongside metadata boundaries.</returns>
        public async Task<ApiResponse<List<GetClinicFeedbackResponse>>> Process(
            GetClinicFeedbacksRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isClinicExist = true;

            // Step 1: Extract identity information parameter metrics from the active security claim session context
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Search operational relational databases to identify the clinic bound tightly to the active account
            var staffClinic = await RetrieveClinicData(userId, isUserValid);

            // Step 3: Track context validation parameters safely before hitting core query pipelines
            ValidateClinicContext(staffClinic, isUserValid, ref isClinicExist);

            // Step 4: Synthesize a flexible criteria lambda matrix expression using dynamic criteria logic mapping
            var filterExpression = BuildFilterExpression(staffClinic, request, isClinicExist);

            // Step 5: Query underlying target physical data storage blocks executing paged evaluation algorithms
            var (feedbacks, totalRecords) = await ExecutePagedQuery(filterExpression, request, isClinicExist);

            // Step 6: Map internal domain state model attributes onto decoupled serialized response data schemas
            var result = MapToResponseDto(feedbacks);

            // Step 7: Package contextual index layout tracker parameters to formulate pagination tracking wrappers
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Evaluate processing parameters and package state structures dynamically to handle execution outcomes
            return CreateResponse(result, meta, isUserValid, isClinicExist);
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
        /// Probes system allocation tables to isolate unique context configurations related directly onto the logged-in account.
        /// </summary>
        /// <param name="userId">The system identity identifier tracking target metrics variables.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>The staff clinic contextual relationship mapping row instance; otherwise null.</returns>
        private async Task<StaffClinic?> RetrieveClinicData(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            return await _staffClinicRepository
                .FindByCondition(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Validates whether the active context structure binds to a valid operational clinic domain reference.
        /// </summary>
        /// <param name="staffClinic">The resolved contextual staff relation record payload.</param>
        /// <param name="isUserValid">Precondition evaluation flag indicating if user resolving succeeded.</param>
        /// <param name="isClinicExist">Flag updated to <c>false</c> if verification matches fail system benchmarks.</param>
        private void ValidateClinicContext(
            StaffClinic? staffClinic,
            bool isUserValid,
            ref bool isClinicExist)
        {
            if (!isUserValid)
            {
                return;
            }

            if (staffClinic == null)
            {
                isClinicExist = false;
            }
        }

        /// <summary>
        /// Assembles dynamic lambda filter structures mapping request parameters against entity state conditions.
        /// </summary>
        /// <param name="staffClinic">The target staff context relation payload containing location bindings.</param>
        /// <param name="request">The structural entity containing filtering keys and rating search parameters.</param>
        /// <param name="isClinicExist">Guard execution flag assessing baseline tracking criteria states.</param>
        /// <returns>A composite queryable logic filter expression block.</returns>
        private Expression<Func<Feedback, bool>> BuildFilterExpression(
            StaffClinic? staffClinic,
            GetClinicFeedbacksRequest request,
            bool isClinicExist)
        {
            if (!isClinicExist || staffClinic == null)
            {
                return x => false; // Fallback expression returning zero database row hits safely
            }

            var searchTerm = request.SearchTerm?
                .Trim()
                .ToLower();

            var targetClinicId = staffClinic.ClinicId;

            return x =>
                x.ClinicId == targetClinicId
                && x.Appointment.Status == AppointmentStatus.COMPLETED
                && (!request.RatingDoctor.HasValue || x.RatingDoctor == request.RatingDoctor.Value)
                && (!request.RatingClinic.HasValue || x.RatingClinic == request.RatingClinic.Value)
                && (!request.FeedbackDate.HasValue || x.CreatedAt.Date == request.FeedbackDate.Value.Date)
                && (string.IsNullOrEmpty(searchTerm)
                    || x.Patient.FullName.ToLower().Contains(searchTerm)
                    || x.Doctor.User.FullName.ToLower().Contains(searchTerm)
                    || (x.Comment != null && x.Comment.ToLower().Contains(searchTerm)));
        }

        /// <summary>
        /// Executes eager loading database evaluations using explicit transactional tracking skips and page index limit controls.
        /// </summary>
        /// <param name="filterExpression">The structured operational logic filter condition matrix maps.</param>
        /// <param name="request">The data container tracking layout configuration bounds.</param>
        /// <param name="isClinicExist">Guard condition preventing database load pipelines on validation failure states.</param>
        /// <returns>A tuple pairing structural result detail item list rows with overall record aggregate bounds integers.</returns>
        private async Task<(List<Feedback> Feedbacks, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<Feedback, bool>> filterExpression,
            GetClinicFeedbacksRequest request,
            bool isClinicExist)
        {
            if (!isClinicExist)
            {
                return (new List<Feedback>(), 0);
            }

            IQueryable<Feedback> query = _feedbackRepository
                .FindByCondition(filterExpression, trackChanges: false)
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                    .ThenInclude(x => x.User)
                .Include(x => x.Appointment);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="data">The physical domain core model list array returned directly out of backend operational layers.</param>
        /// <returns>A structured target presentation data payload collection instance context.</returns>
        private List<GetClinicFeedbackResponse> MapToResponseDto(List<Feedback> data)
        {
            return data.Select(feedback =>
                new GetClinicFeedbackResponse
                {
                    Id_feedback = feedback.Id.ToString(),
                    PatientName = feedback.Patient.FullName,
                    DoctorName = feedback.Doctor.User.FullName,
                    RatingDoctor = feedback.RatingDoctor,
                    RatingClinic = feedback.RatingClinic,
                    Comment = string.IsNullOrWhiteSpace(feedback.Comment) ? "N/A" : feedback.Comment,
                    IsPublic = feedback.IsPublic,
                    AppointmentDate = feedback.Appointment.AppointmentDate.ToString("dd/MM/yyyy"),
                    FeedbackDate = feedback.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();
        }

        /// <summary>
        /// Assembles pagination envelope metrics parameters using custom constructor signatures.
        /// </summary>
        /// <param name="request">The tracking item block mapping target layout page configurations.</param>
        /// <param name="totalRecords">The quantified data baseline row count metrics context.</param>
        /// <returns>A populated metadata envelope serialization module component instance.</returns>
        private MetaResponse BuildPaginationMeta(GetClinicFeedbacksRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="result">The structured data response payload list projected from database layers.</param>
        /// <param name="meta">The pagination metadata structure layout configuration parameters.</param>
        /// <param name="isUserValid">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="isClinicExist">Indicates data layer tracking context presence attributes.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<List<GetClinicFeedbackResponse>> CreateResponse(
            List<GetClinicFeedbackResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<GetClinicFeedbackResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Evaluates structural tracking conditions matrix variables to issue systemized application error entries.
        /// </summary>
        /// <param name="isUserValid">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <param name="isClinicExist">The state checking indicator capturing contextual environment existence benchmarks.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<List<GetClinicFeedbackResponse>>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<GetClinicFeedbackResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<List<GetClinicFeedbackResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}