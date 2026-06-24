using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentHistoryServices
{
    /// <summary>
    /// Handles the business logic for retrieving, multi-table joining, filtering, and paging appointments tied to the authorized user accounts.
    /// </summary>
    public class GetAppointmentHistoryService : IGetAppointmentHistoryService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAppointmentHistoryService"/> class with required infrastructure boundaries.
        /// </summary>
        /// <param name="appointmentRepository">Repository boundary instance for querying physical appointment records.</param>
        /// <param name="context">The underlying database persistence instance mapping multi-entity relational relational graph data models.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public GetAppointmentHistoryService(
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _appointmentRepository = appointmentRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to compute identity contexts, query multi-clinic database aggregates, and map responses.
        /// </summary>
        /// <param name="request">The parameters containing keyword queries, page sequences, and sizing structures.</param>
        /// <returns>An <see cref="ApiResponse{List{GetAppointmentHistoryResponse}}"/> enclosing aggregated descriptive data structures.</returns>
        public async Task<ApiResponse<List<GetAppointmentHistoryResponse>>> Process(GetAppointmentHistoryRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isDataScopeExist = true;

            // Step 1: Extract identity information metrics from active token pipelines
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Search relational links mapping permission boundaries across profile layers
            var accessibleProfileIds = await RetrieveLinkedProfileIds(userId, isUserValid);

            // Step 3: Track context validation parameters safely before execution
            ValidateDataContext(accessibleProfileIds, isUserValid, ref isDataScopeExist);

            // Step 4: Synthesize a flexible criteria lambda expression handling data filtration matrices
            var filterExpression = BuildFilterExpression(accessibleProfileIds, userId, request, isDataScopeExist);

            // Step 5: Query storage layout engines utilizing paging evaluation bounds
            var (appointments, totalRecords) = await ExecutePagedQuery(filterExpression, request, isDataScopeExist);

            // Step 6: Map internal domain state segments to serialized outcome presentation representations
            var result = MapToResponseDto(appointments);

            // Step 7: Formulate tracking index metrics for wrapping pagination blocks
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Package contextual payloads dynamically to manage outcome states
            return CreateResponse(result, meta, isUserValid, isDataScopeExist);
        }

        /// <summary>
        /// Resolves the logged-in user credentials via claims identity mapping streams.
        /// </summary>
        /// <param name="isUserValid">Guard state flag modified by reference to track authentication integrity.</param>
        /// <returns>The extracted structural global unique identifiers token block mapping the active user account context.</returns>
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
        /// Probes many-to-many link tables to extract authorized patient metrics identifiers.
        /// </summary>
        /// <param name="userId">The system reference credentials tracking structural context nodes.</param>
        /// <param name="isUserValid">Guard validation state assessing token evaluation integrity parameters.</param>
        /// <returns>A list tracking unique entity vector addresses mapping authorized relational profiles data blocks.</returns>
        private async Task<List<Guid>> RetrieveLinkedProfileIds(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return new List<Guid>();

            // Step 1: Query database layers using optimized streams to gather core direct profiles mapping target indicators
            var directProfileIds = await _context.Set<PatientProfile>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .ToListAsync();

            // Step 2: Extract relational identifiers linked across cross-reference matrix graph structures
            var linkedProfileIds = await _context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.PatientId)
                .ToListAsync();

            // Step 3: Consolidate unique coordinate tracks into standardized sequential blocks filtering overlapping nodes
            return directProfileIds.Union(linkedProfileIds).Distinct().ToList();
        }

        /// <summary>
        /// Checks baseline query pre-conditions prior to executing backend database loads.
        /// </summary>
        /// <param name="accessibleProfileIds">The tracking vector identifiers collection identifying permitted visibility layers.</param>
        /// <param name="isUserValid">Guard state monitoring authentication pipeline checkpoints.</param>
        /// <param name="isDataScopeExist">State checkpoint trace modified to lock database operations dynamically.</param>
        private void ValidateDataContext(List<Guid> accessibleProfileIds, bool isUserValid, ref bool isDataScopeExist)
        {
            if (!isUserValid) return;

            if (accessibleProfileIds == null || !accessibleProfileIds.Any())
            {
                isDataScopeExist = false;
            }
        }

        /// <summary>
        /// Assembles dynamic lambda filters assessing target keyword boundaries across relational structures.
        /// </summary>
        /// <param name="accessibleProfileIds">The structural permission boundary arrays checking operational security bounds.</param>
        /// <param name="userId">The authenticated identity metrics mapping current context states.</param>
        /// <param name="request">The incoming parameters block holding dynamic filter tokens.</param>
        /// <param name="isDataScopeExist">Guard flag validating structural integrity blocks before evaluation execution loops.</param>
        /// <returns>A compound logic criteria filter block representing continuous query boundaries.</returns>
        private Expression<Func<Appointment, bool>> BuildFilterExpression(
             List<Guid> accessibleProfileIds,
             Guid userId,
             GetAppointmentHistoryRequest request,
             bool isDataScopeExist)
        {
            if (!isDataScopeExist || accessibleProfileIds == null || !accessibleProfileIds.Any())
            {
                return x => false;
            }

            var searchTerm = request.SearchTerm?.Trim().ToLower();

            AppointmentStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(request.Status)
                && Enum.TryParse<AppointmentStatus>(request.Status, true, out var parsedStatus))
            {
                statusEnum = parsedStatus;
            }

            // Step 1: Compose base baseline multi-node expressions filtering access keys and search matrix parameters
            Expression<Func<Appointment, bool>> baseExpr = x =>
                (accessibleProfileIds.Contains(x.PatientId) || x.CreatedById == userId)
                && (string.IsNullOrEmpty(searchTerm)
                    || x.Doctor.Clinic.Name.ToLower().Contains(searchTerm)
                    || x.Doctor.User.FullName.ToLower().Contains(searchTerm)
                    || x.Patient.FullName.ToLower().Contains(searchTerm));

            if (!statusEnum.HasValue)
            {
                return baseExpr;
            }

            var status = statusEnum.Value;

            // Step 2: Inject discrete analytical status filtering metrics into secondary dynamic expressions pipelines
            return x =>
                (accessibleProfileIds.Contains(x.PatientId) || x.CreatedById == userId)
                && (string.IsNullOrEmpty(searchTerm)
                    || x.Doctor.Clinic.Name.ToLower().Contains(searchTerm)
                    || x.Doctor.User.FullName.ToLower().Contains(searchTerm)
                    || x.Patient.FullName.ToLower().Contains(searchTerm))
                && x.Status == status;
        }

        /// <summary>
        /// Executes database lookups mapping nested object trees with transactional optimization pipelines.
        /// </summary>
        /// <param name="filterExpression">The complex relational lambda validation rule criteria matrices maps.</param>
        /// <param name="request">The data object carrying explicit size restrictions and offset variables.</param>
        /// <param name="isDataScopeExist">Guard framework flag checking empty collection blocks safely.</param>
        /// <returns>A composite tuple containing structural models alongside actual aggregate ledger index data rows counts.</returns>
        private async Task<(List<Appointment> Appointments, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<Appointment, bool>> filterExpression,
            GetAppointmentHistoryRequest request,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist)
            {
                return (new List<Appointment>(), 0);
            }

            // Step 1: Initialize database graph streams attaching mandatory relational model layouts eagerly
            IQueryable<Appointment> query = _appointmentRepository
                .FindByCondition(filterExpression, trackChanges: false)
                .Include(x => x.Patient)
                .Include(x => x.Slot)
                .Include(x => x.Service)
                .Include(x => x.Doctor).ThenInclude(d => d.Clinic)
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Feedback);

            // Step 2: Query historical database clusters to safely aggregate absolute total tracking record dimensions
            var totalRecords = await query.CountAsync();

            // Step 3: Run targeted evaluation pipelines extracting offset index packets sorted chronologically
            var items = await query
                .OrderByDescending(x => x.AppointmentDate)
                .ThenByDescending(x => x.Slot.StartTime)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        /// <summary>
        /// Transforms persistent domain models properties directly into target presentation DTO schemas.
        /// </summary>
        /// <param name="data">The physical contextual database models array structures generated out of execution loops.</param>
        /// <returns>A presentation data representation list optimized for target serializations layers.</returns>
        private List<GetAppointmentHistoryResponse> MapToResponseDto(List<Appointment> data)
        {
            return data.Select(appointment =>
            {
                string timeSlotFormatted = appointment.Slot != null
                    ? $"{appointment.Slot.StartTime:HH:mm} - {appointment.Slot.EndTime:HH:mm}"
                    : "N/A";

                var doctor = appointment.Doctor;
                var clinic = doctor?.Clinic;
                var doctorUser = doctor?.User;

                string doctorName = doctor != null
                    ? $"{doctor.Title} {doctorUser?.FullName}".Trim()
                    : "N/A";

                return new GetAppointmentHistoryResponse
                {
                    Id_appointment = appointment.Id.ToString(),
                    AppointmentDate = appointment.AppointmentDate.ToString("dd/MM/yyyy"),
                    TimeSlot = timeSlotFormatted,
                    Status = appointment.Status.ToString(),
                    ClinicName = clinic?.Name ?? "N/A",
                    ClinicAddress = clinic?.Address ?? "N/A",
                    PatientName = appointment.Patient?.FullName ?? "N/A",
                    DoctorName = doctorName,
                    ServiceName = appointment.Service != null ? appointment.Service.ServiceName : "Khám mắt tổng quát",
                    ServicePrice = appointment.Service?.Price != null ? appointment.Service.Price.Value.ToString("N0") + " VND" : "Miễn phí",
                    HasFeedback = appointment.Feedback != null
                };
            })
            .ToList();
        }

        /// <summary>
        /// Assembles internal tracking metadata attributes bounding envelope packages.
        /// </summary>
        /// <param name="request">The state block enclosing initial offset sequences constraints parameters.</param>
        /// <param name="totalRecords">The continuous evaluation dimensions captured directly out of storage engines.</param>
        /// <returns>A configured metadata payload controlling interface pagination indicators.</returns>
        private MetaResponse BuildPaginationMeta(GetAppointmentHistoryRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Resolves transaction outcome wrappers packing serialization nodes safely.
        /// </summary>
        /// <param name="result">The internal serializable structures returned out of core projection chains.</param>
        /// <param name="meta">The pagination control bounds evaluating index allocations mappings.</param>
        /// <param name="isUserValid">Guard context parameter evaluating token claim validity bounds.</param>
        /// <param name="isDataScopeExist">Guard monitoring parameter checking physical ledger boundaries.</param>
        /// <returns>A structured envelope holding operational response outcomes ready for presentation nodes.</returns>
        private ApiResponse<List<GetAppointmentHistoryResponse>> CreateResponse(
            List<GetAppointmentHistoryResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isDataScopeExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isDataScopeExist);
            if (errorResponse != null)
            {
                // Return dynamic exception responses immediately on encountering validation pipeline failures
                return errorResponse;
            }

            return ApiResponse<List<GetAppointmentHistoryResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Evaluates functional exceptions sequences to render failure metadata nodes.
        /// </summary>
        /// <param name="isUserValid">Guard indicating whether authorization checkpoints cleared successfully.</param>
        /// <param name="isDataScopeExist">Indicator verifying database profile relational data existence benchmarks.</param>
        /// <returns>A failure configuration block, or null if execution tracks meet standard benchmarks.</returns>
        private ApiResponse<List<GetAppointmentHistoryResponse>>? CreateErrorResponse(bool isUserValid, bool isDataScopeExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<List<GetAppointmentHistoryResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isDataScopeExist)
            {
                return ApiResponse<List<GetAppointmentHistoryResponse>>.Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(),
                    new List<GetAppointmentHistoryResponse>(),
                    new MetaResponse(1, 10, 0));
            }

            return null;
        }
    }
}