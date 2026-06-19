using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// Handles the business logic for viewing a paginated list of patient demographics with associated medical records.
    /// </summary>
    public class ViewPatientDemographicsService : IViewPatientDemographicsService
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IValidator<ViewPatientDemographicsRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewPatientDemographicsService"/> class with required repositories and validation pipelines.
        /// </summary>
        /// <param name="patientProfileRepository">Repository for querying patient profile data.</param>
        /// <param name="medicalRecordRepository">Repository for querying medical record data.</param>
        /// <param name="validator">Validator for view patient demographics request data.</param>
        /// <param name="context">The underlying persistence database context.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities.</param>
        public ViewPatientDemographicsService(
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IValidator<ViewPatientDemographicsRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientProfileRepository = patientProfileRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the view patient demographics list request by validating input, fetching patient data and records, and returning the aggregated response.
        /// </summary>
        /// <param name="request">The view patient demographics request.</param>
        /// <returns>An <see cref="ApiResponse{ViewPatientDemographicsListResponse}"/> containing paginated patient demographics list.</returns>
        public async Task<ApiResponse<ViewPatientDemographicsListResponse>> Process(ViewPatientDemographicsRequest request)
        {
            bool isValidationPassed = true;
            bool isDataScopeExist = true;

            ValidateRequest(request, ref isValidationPassed);

            var currentUserId = RetrieveUserId(ref isDataScopeExist);
            var accessiblePatientIds = await RetrieveAccessiblePatientIds(currentUserId, isDataScopeExist);

            var (pageNumber, pageSize) = NormalizePaging(request);

            var totalRecords = await CountDemographicsAsync(request, accessiblePatientIds, isDataScopeExist);

            var totalPages = CalculateTotalPages(totalRecords, pageSize);

            var items = await FetchDemographicsAsync(request, accessiblePatientIds, isDataScopeExist, pageNumber, pageSize);

            var response = new ViewPatientDemographicsListResponse
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Items = items
            };

            return CreateResponse(response, isValidationPassed, isDataScopeExist);
        }

        /// <summary>
        /// Validates the incoming request payload against defined business rules.
        /// </summary>
        private void ValidateRequest(ViewPatientDemographicsRequest request, ref bool isValidationPassed)
        {
            var result = _validator.Validate(request);
            if (!result.IsValid)
            {
                isValidationPassed = false;
            }
        }

        /// <summary>
        /// Retrieves the logged-in user identifier from the authentication claims.
        /// </summary>
        private Guid RetrieveUserId(ref bool isDataScopeExist)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isDataScopeExist = false;
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Retrieves the list of patient profile IDs accessible by the current user (owned + linked).
        /// </summary>
        private async Task<List<Guid>> RetrieveAccessiblePatientIds(Guid userId, bool isDataScopeExist)
        {
            if (!isDataScopeExist) return new List<Guid>();

            var roleClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(System.Security.Claims.ClaimTypes.Role)?
                .Value;

            if (string.Equals(roleClaim, "DOCTOR", StringComparison.OrdinalIgnoreCase))
            {
                var doctorProfile = await _context.Set<DoctorProfile>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

                if (doctorProfile == null)
                    return new List<Guid>();

                return await _context.Set<Appointment>()
                    .AsNoTracking()
                    .Where(a => a.DoctorId == doctorProfile.Id)
                    .Select(a => a.PatientId)
                    .Distinct()
                    .ToListAsync();
            }

            var directPatientIds = await _patientProfileRepository
                .FindByCondition(x => x.UserId == userId, trackChanges: false)
                .Select(x => x.Id)
                .ToListAsync();

            var linkedPatientIds = await _context.Set<ECS.Domain.Entities.Patient.UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.PatientId)
                .ToListAsync();

            return directPatientIds.Union(linkedPatientIds).Distinct().ToList();
        }

        /// <summary>
        /// Normalizes pagination parameters.
        /// </summary>
        private static (int PageNumber, int PageSize) NormalizePaging(ViewPatientDemographicsRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            return (pageNumber, pageSize);
        }

        /// <summary>
        /// Counts total demographics items matching the request filters.
        /// </summary>
        private async Task<int> CountDemographicsAsync(
            ViewPatientDemographicsRequest request,
            List<Guid> accessiblePatientIds,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist) return 0;

            var query = BuildBaseQuery(request, accessiblePatientIds, isDataScopeExist);
            return await query.CountAsync();
        }

        /// <summary>
        /// Fetches a page of demographics items with associated patient and record data.
        /// </summary>
        private async Task<List<ViewPatientDemographicsListItem>> FetchDemographicsAsync(
            ViewPatientDemographicsRequest request,
            List<Guid> accessiblePatientIds,
            bool isDataScopeExist,
            int pageNumber,
            int pageSize)
        {
            if (!isDataScopeExist) return new List<ViewPatientDemographicsListItem>();

            var query = BuildBaseQuery(request, accessiblePatientIds, isDataScopeExist);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ViewPatientDemographicsListItem
                {
                    Id_PatientProfile = r.Patient.Id.ToString(),
                    FullName = r.Patient.FullName,
                    Gender = r.Patient.Gender.ToString(),
                    Dob = r.Patient.Dob.ToString("dd/MM/yyyy"),
                    IdentityNumber = r.Patient.IdentityNumber,
                    PhoneNumber = r.Patient.PhoneNumber,
                    Address = r.Patient.Address,
                    BhytNumber = r.Patient.BhytNumber,
                    BloodType = r.Patient.BloodType,
                    Allergies = r.Patient.Allergies,
                    MedicalHistory = r.Patient.MedicalHistory,
                    Id_MedicalRecord = r.Id.ToString(),
                    RecordType = r.RecordType.ToString(),
                    RecordTypeLabel = GetRecordTypeLabel(r.RecordType),
                    DoctorName = r.Doctor.User.FullName,
                    AppointmentDate = r.Appointment.AppointmentDate.ToString("dd/MM/yyyy"),
                    ChiefComplaint = r.ChiefComplaint,
                    DiagnosisMain = r.DiagnosisMain,
                    IsLocked = r.IsLocked,
                    HasMedicalDemographics = r.Patient.HasMedicalDemographics,
                    CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();
        }

        /// <summary>
        /// Builds the base queryable for demographics, applying accessible patient scope and filters.
        /// </summary>
        private IQueryable<MedicalRecord> BuildBaseQuery(
            ViewPatientDemographicsRequest request,
            List<Guid> accessiblePatientIds,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist)
                return _medicalRecordRepository.FindByCondition(r => false, trackChanges: false);

            IQueryable<MedicalRecord> query = _medicalRecordRepository
                .FindByCondition(r => accessiblePatientIds.Contains(r.PatientId), trackChanges: false)
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                    .ThenInclude(d => d.User)
                .Include(r => r.Appointment);

            if (Guid.TryParse(request.PatientProfileId, out Guid patientProfileId))
            {
                query = query.Where(r => r.PatientId == patientProfileId);
            }

            if (!string.IsNullOrWhiteSpace(request.RecordType) &&
                Enum.TryParse<RecordType>(request.RecordType, true, out var recordType))
            {
                query = query.Where(r => r.RecordType == recordType);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(r =>
                    (r.DiagnosisMain != null && r.DiagnosisMain.ToLower().Contains(searchTerm)) ||
                    (r.ChiefComplaint != null && r.ChiefComplaint.ToLower().Contains(searchTerm)));
            }

            return query;
        }

        /// <summary>
        /// Calculates the total number of pages.
        /// </summary>
        private static int CalculateTotalPages(int totalRecords, int pageSize)
        {
            return pageSize == 0 ? 0 : (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        /// <summary>
        /// Returns the Vietnamese display label for the given record type.
        /// </summary>
        private static string GetRecordTypeLabel(RecordType recordType)
        {
            return recordType switch
            {
                RecordType.MS21_TRAUMA => "Bệnh án mắt (Chấn thương)",
                RecordType.MS22_ANTERIOR => "Bệnh án mắt (Bán phần trước)",
                RecordType.MS23_FUNDUS => "Bệnh án mắt (Đáy mắt)",
                RecordType.MS24_GLAUCOMA => "Bệnh án mắt (Glôcôm)",
                RecordType.MS25_STRABISMUS_PTOSIS => "Bệnh án mắt (Lác, sụp mi)",
                RecordType.MS26_PEDIATRIC => "Bệnh án mắt (Mắt trẻ em)",
                _ => recordType.ToString()
            };
        }

        /// <summary>
        /// Packages the result into the standardized API response envelope.
        /// </summary>
        private static ApiResponse<ViewPatientDemographicsListResponse> CreateResponse(
            ViewPatientDemographicsListResponse result,
            bool isValidationPassed,
            bool isDataScopeExist)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<ViewPatientDemographicsListResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4019.ToString());
            }

            if (!isDataScopeExist || result == null)
            {
                return ApiResponse<ViewPatientDemographicsListResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4010.ToString());
            }

            return ApiResponse<ViewPatientDemographicsListResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }
    }
}
