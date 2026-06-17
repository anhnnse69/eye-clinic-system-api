using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// Handles the business logic for viewing patient demographics and their medical records list.
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
        /// Processes the view patient demographics request by validating input, fetching patient data and records, and returning the aggregated response.
        /// </summary>
        /// <param name="request">The view patient demographics request.</param>
        /// <returns>An <see cref="ApiResponse{ViewPatientDemographicsResponse}"/> containing patient demographics and records list.</returns>
        public async Task<ApiResponse<ViewPatientDemographicsResponse>> Process(ViewPatientDemographicsRequest request)
        {
            bool isValidationPassed = true;
            bool isDataScopeExist = true;

            ValidateRequest(request, ref isValidationPassed);

            var currentUserId = RetrieveUserId(ref isDataScopeExist);
            var accessiblePatientIds = await RetrieveAccessiblePatientIds(currentUserId, isDataScopeExist);

            ValidatePatientAccess(request, accessiblePatientIds, isDataScopeExist, ref isDataScopeExist);

            var (patientProfile, records, totalRecords) = await RetrievePatientData(request, accessiblePatientIds, isDataScopeExist);

            var result = MapToResponse(patientProfile, records, totalRecords);

            return CreateResponse(result, isValidationPassed, isDataScopeExist);
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
        /// Validates that the requested patient profile is within the accessible scope of the current user.
        /// </summary>
        private void ValidatePatientAccess(
            ViewPatientDemographicsRequest request,
            List<Guid> accessiblePatientIds,
            bool isDataScopeExist,
            ref bool isDataScopeExistResult)
        {
            if (!isDataScopeExist) return;

            if (!Guid.TryParse(request.PatientProfileId, out Guid targetPatientId))
            {
                isDataScopeExistResult = false;
                return;
            }

            if (!accessiblePatientIds.Contains(targetPatientId))
            {
                isDataScopeExistResult = false;
            }
        }

        /// <summary>
        /// Queries the database for the patient profile and their medical records with pagination.
        /// </summary>
        private async Task<(PatientProfile? Patient, List<MedicalRecord> Records, int TotalRecords)> RetrievePatientData(
            ViewPatientDemographicsRequest request,
            List<Guid> accessiblePatientIds,
            bool isDataScopeExist)
        {
            if (!isDataScopeExist) return (null, new List<MedicalRecord>(), 0);

            if (!Guid.TryParse(request.PatientProfileId, out Guid patientProfileId))
            {
                return (null, new List<MedicalRecord>(), 0);
            }

            var patientProfile = await _patientProfileRepository
                .FindByCondition(x => x.Id == patientProfileId, trackChanges: false)
                .FirstOrDefaultAsync();

            if (patientProfile == null)
            {
                return (null, new List<MedicalRecord>(), 0);
            }

            Expression<Func<MedicalRecord, bool>> recordFilter = x => x.PatientId == patientProfileId;

            if (!string.IsNullOrWhiteSpace(request.RecordType))
            {
                if (Enum.TryParse<RecordType>(request.RecordType, true, out var recordType))
                {
                    recordFilter = x => x.PatientId == patientProfileId && x.RecordType == recordType;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                recordFilter = x => x.PatientId == patientProfileId
                    && (x.DiagnosisMain != null && x.DiagnosisMain.ToLower().Contains(searchTerm)
                        || x.ChiefComplaint != null && x.ChiefComplaint.ToLower().Contains(searchTerm));
            }

            var query = _medicalRecordRepository
                .FindByCondition(recordFilter, trackChanges: false)
                .Include(x => x.Doctor.User)
                .Include(x => x.Appointment);

            var totalRecords = await query.CountAsync();

            var records = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (patientProfile, records, totalRecords);
        }

        /// <summary>
        /// Maps domain entities to the response DTO.
        /// </summary>
        private ViewPatientDemographicsResponse MapToResponse(
            PatientProfile? patientProfile,
            List<MedicalRecord> records,
            int totalRecords)
        {
            if (patientProfile == null)
            {
                return new ViewPatientDemographicsResponse();
            }

            var recordItems = records.Select(r => new MedicalRecordSummaryItem
            {
                Id_MedicalRecord = r.Id.ToString(),
                RecordType = r.RecordType.ToString(),
                RecordTypeLabel = GetRecordTypeLabel(r.RecordType),
                DoctorName = r.Doctor?.User?.FullName ?? "N/A",
                AppointmentDate = r.Appointment?.AppointmentDate.ToString("dd/MM/yyyy") ?? "N/A",
                ChiefComplaint = r.ChiefComplaint,
                DiagnosisMain = r.DiagnosisMain,
                IsLocked = r.IsLocked,
                CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            return new ViewPatientDemographicsResponse
            {
                Id_PatientProfile = patientProfile.Id.ToString(),
                FullName = patientProfile.FullName,
                Gender = patientProfile.Gender.ToString(),
                Dob = patientProfile.Dob.ToString("dd/MM/yyyy"),
                IdentityNumber = patientProfile.IdentityNumber,
                PhoneNumber = patientProfile.PhoneNumber,
                Address = patientProfile.Address,
                BhytNumber = patientProfile.BhytNumber,
                BloodType = patientProfile.BloodType,
                Allergies = patientProfile.Allergies,
                MedicalHistory = patientProfile.MedicalHistory,
                TotalRecords = totalRecords,
                Records = recordItems
            };
        }

        /// <summary>
        /// Returns the Vietnamese display label for the given record type.
        /// </summary>
        private string GetRecordTypeLabel(RecordType recordType)
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
        private ApiResponse<ViewPatientDemographicsResponse> CreateResponse(
            ViewPatientDemographicsResponse result,
            bool isValidationPassed,
            bool isDataScopeExist)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<ViewPatientDemographicsResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4019.ToString());
            }

            if (!isDataScopeExist || result == null || string.IsNullOrEmpty(result.Id_PatientProfile))
            {
                return ApiResponse<ViewPatientDemographicsResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4010.ToString());
            }

            return ApiResponse<ViewPatientDemographicsResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }
    }
}
