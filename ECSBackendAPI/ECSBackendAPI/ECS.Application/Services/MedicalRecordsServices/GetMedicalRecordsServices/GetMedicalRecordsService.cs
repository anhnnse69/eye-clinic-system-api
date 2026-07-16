using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices
{
    /// <summary>
    /// Handles the business logic for retrieving medical records history for doctors.
    /// Only returns records for patients that the doctor has treated.
    /// Edit permission is granted only for today's appointments where the doctor is the treating physician.
    /// </summary>
    public class GetMedicalRecordsService : IGetMedicalRecordsService
    {
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="GetMedicalRecordsService"/> with query repositories and database context.
        /// </summary>
        /// <param name="medicalRecordRepository">The query repository for medical record entities.</param>
        /// <param name="context">The application database context for direct entity access.</param>
        /// <param name="httpContextAccessor">HTTP context accessor to extract authenticated user claims.</param>
        public GetMedicalRecordsService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Orchestrates the process of building criteria, executing queries, and extracting paginated medical records.
        /// </summary>
        /// <param name="request">The filtration and pagination arguments for the medical records query.</param>
        /// <returns>A structured framework response wrapping final data arrays and pagination metadata.</returns>
        public async Task<ApiResponse<List<GetMedicalRecordsResponse>>> Process(
            GetMedicalRecordsRequest request)
        {
            var doctorProfileId = RetrieveDoctorProfileId(out bool isUserValid);
            var filterExpression = BuildFilterExpression(request, doctorProfileId);
            var databaseRecords = ExecutePatientsQuery(filterExpression, request, out int totalRecords);
            var formattedList = MapToPresentationDto(databaseRecords, doctorProfileId);
            var paginationMetadata = BuildPaginationMeta(request, totalRecords);
            return CreateApiResponse(formattedList, paginationMetadata, isUserValid);
        }

        /// <summary>
        /// Extracts the doctor profile ID from the authenticated user's JWT token.
        /// </summary>
        /// <param name="isUserValid">Output flag indicating whether the user validation succeeded.</param>
        /// <returns>The doctor profile ID if found and active; otherwise, an empty GUID.</returns>
        private Guid RetrieveDoctorProfileId(out bool isUserValid)
        {
            isUserValid = true;
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            Guid.TryParse(userIdClaim, out Guid userId);
            isUserValid = isUserValid && !string.IsNullOrEmpty(userIdClaim);

            var doctorProfile = _context.Set<DoctorProfile>()
                .AsNoTracking()
                .FirstOrDefault(x => x.UserId == userId && x.IsActive);

            isUserValid = isUserValid && doctorProfile != null;

            return doctorProfile?.Id ?? Guid.Empty;
        }

        /// <summary>
        /// Builds dynamic expression trees targeting medical record entities based on UI criteria parameters.
        /// </summary>
        /// <param name="request">The filters package from consumer query strings.</param>
        /// <param name="doctorProfileId">The doctor profile ID to filter records by treating physician.</param>
        /// <returns>A reusable LINQ system predicate expression lambda.</returns>
        private Expression<Func<MedicalRecord, bool>> BuildFilterExpression(
            GetMedicalRecordsRequest request,
            Guid doctorProfileId)
        {
            var searchTerm = request.SearchTerm?.Trim().ToLower();
            var startDate = request.StartDate;
            var endDate = request.EndDate;
            var recordType = request.RecordType;
            var filterDoctorId = request.DoctorId;

            return x =>
                x.DoctorId == doctorProfileId
                && (recordType == null || x.RecordType == recordType)
                && (startDate == null || x.CreatedAt >= startDate.Value)
                && (endDate == null || x.CreatedAt <= endDate.Value.AddDays(1))
                && (string.IsNullOrEmpty(searchTerm)
                    || x.Patient.FullName.ToLower().Contains(searchTerm)
                    || x.AppointmentId.ToString().Contains(searchTerm)
                    || (x.ChiefComplaint != null && x.ChiefComplaint.ToLower().Contains(searchTerm))
                    || (x.Summary != null && x.Summary.ToLower().Contains(searchTerm)))
                && (filterDoctorId == null || x.DoctorId == filterDoctorId.Value);
        }

        /// <summary>
        /// Executes the optimized database lookup to extract a structured slice of paginated medical records.
        /// </summary>
        /// <param name="filterExpression">The compiled lambda filters expression.</param>
        /// <param name="request">The current requested page pagination size parameters context.</param>
        /// <param name="totalRecords">Output parameter tracing total matching rows before partition constraints.</param>
        /// <returns>A list of resolved <see cref="MedicalRecord"/> data tracking fragments.</returns>
        private List<MedicalRecord> ExecutePatientsQuery(
            Expression<Func<MedicalRecord, bool>> filterExpression,
            GetMedicalRecordsRequest request,
            out int totalRecords)
        {
            IQueryable<MedicalRecord> query = _medicalRecordRepository
                .FindByCondition(filterExpression, trackChanges: false)
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .ThenInclude(d => d.User)
                .Include(x => x.Appointment);

            query = query.OrderByDescending(x => x.CreatedAt);
            totalRecords = query.Count();

            return query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }

        /// <summary>
        /// Maps tracking database records into presentation layer DTO sequences with edit/view permission flags.
        /// </summary>
        /// <param name="records">The raw source list tracking database values.</param>
        /// <param name="currentDoctorId">The current authenticated doctor profile ID for permission evaluation.</param>
        /// <returns>The collection containing formatted <see cref="GetMedicalRecordsResponse"/> items.</returns>
        private List<GetMedicalRecordsResponse> MapToPresentationDto(
            List<MedicalRecord> records,
            Guid currentDoctorId)
        {
            var today = DateTime.UtcNow.Date;

            return records.Select(record =>
            {
                var appointmentDate = record.Appointment.AppointmentDate.Date;
                var isToday = appointmentDate == today;
                var isPast = appointmentDate < today;
                var isFuture = appointmentDate > today;
                var isCurrentDoctor = record.DoctorId == currentDoctorId;
                var isLocked = record.IsLocked;

                bool canEdit = isToday && isCurrentDoctor && !isLocked;
                bool canViewOnly = !canEdit;

                string? restrictionReason = isLocked ? "Hồ sơ đã bị khóa"
                    : !isCurrentDoctor ? "Bạn không phải bác sĩ điều trị của hồ sơ này"
                    : isPast ? "Cuộc hẹn đã qua ngày khám"
                    : isFuture ? "Cuộc hẹn chưa đến ngày khám"
                    : null;

                return new GetMedicalRecordsResponse
                {
                    Id = record.Id,
                    AppointmentId = record.AppointmentId,
                    PatientId = record.PatientId,
                    PatientFullName = record.Patient.FullName,
                    PatientDob = record.Patient.Dob.ToString("dd/MM/yyyy"),
                    PatientPhone = record.Patient.PhoneNumber,
                    DoctorId = record.DoctorId,
                    DoctorFullName = record.Doctor.User?.FullName ?? "N/A",
                    AppointmentDate = record.Appointment.AppointmentDate,
                    RecordType = record.RecordType.ToString(),
                    ChiefComplaint = record.ChiefComplaint,
                    DiagnosisMain = record.Summary,
                    TreatmentPlan = record.Notes,
                    IsLocked = record.IsLocked,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                    CanEdit = canEdit,
                    CanViewOnly = canViewOnly,
                    EditRestrictionReason = restrictionReason
                };
            }).ToList();
        }

        /// <summary>
        /// Computes and instantiates pagination framework structural metadata models.
        /// </summary>
        /// <param name="request">The current requested page pagination size parameters context.</param>
        /// <param name="totalRecords">The total number of records matching the filter criteria.</param>
        /// <returns>A structured pagination metadata object.</returns>
        private MetaResponse BuildPaginationMeta(GetMedicalRecordsRequest request, int totalRecords)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
        }

        /// <summary>
        /// Wraps the generated pagination payload elements inside standard response success framework wrappers.
        /// </summary>
        /// <param name="resultList">The mapped DTO list to include in the response.</param>
        /// <param name="meta">The pagination metadata for client-side navigation.</param>
        /// <param name="isUserValid">Flag indicating whether user authentication and authorization succeeded.</param>
        /// <returns>A structured API response with either success data or failure code.</returns>
        private ApiResponse<List<GetMedicalRecordsResponse>> CreateApiResponse(
            List<GetMedicalRecordsResponse> resultList,
            MetaResponse meta,
            bool isUserValid)
        {
            var errorCode = !isUserValid ? GeneralCode.APP_MESSAGE_4033.ToString() : null;
            var successCode = GeneralCode.APP_MESSAGE_2000.ToString();

            return !isUserValid
                ? ApiResponse<List<GetMedicalRecordsResponse>>.Fail(errorCode!)
                : ApiResponse<List<GetMedicalRecordsResponse>>.Success(successCode, resultList, meta);
        }
    }
}
