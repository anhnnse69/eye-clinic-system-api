using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Scheduling;
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
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetMedicalRecordsService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _appointmentRepository = appointmentRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<List<GetMedicalRecordsResponse>>> GetMedicalRecordsAsync(
            GetMedicalRecordsRequest request)
        {
            bool isUserValid = true;
            bool hasData = true;
            var doctorProfileId = RetrieveDoctorProfileId(ref isUserValid);
            var filterExpression = BuildFilterExpression(request, doctorProfileId);
            var (records, totalRecords, hasDataFromQuery) = await ExecutePagedQueryAsync(filterExpression, request);
            hasData = hasDataFromQuery;
            var result = MapToResponseDto(records, doctorProfileId);
            var meta = new MetaResponse(request.PageNumber, request.PageSize, totalRecords);
            return CreateResponse(result, meta, isUserValid);
        }

        private Guid RetrieveDoctorProfileId(ref bool isUserValid)
        {
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
                    || (x.DiagnosisMain != null && x.DiagnosisMain.ToLower().Contains(searchTerm)))
                && (filterDoctorId == null || x.DoctorId == filterDoctorId.Value);
        }

        private async Task<(List<MedicalRecord> Records, int TotalCount, bool HasData)> ExecutePagedQueryAsync(
            Expression<Func<MedicalRecord, bool>> filterExpression,
            GetMedicalRecordsRequest request)
        {
            IQueryable<MedicalRecord> query = _medicalRecordRepository
                .FindByCondition(filterExpression, trackChanges: false)
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .ThenInclude(d => d.User)
                .Include(x => x.Appointment);

            var totalRecords = await query.CountAsync();
            var hasData = totalRecords > 0;

            var items = query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return (items, totalRecords, hasData);
        }

        private List<GetMedicalRecordsResponse> MapToResponseDto(
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
                    DiagnosisMain = record.DiagnosisMain,
                    TreatmentPlan = record.TreatmentPlan,
                    IsLocked = record.IsLocked,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                    CanEdit = canEdit,
                    CanViewOnly = canViewOnly,
                    EditRestrictionReason = restrictionReason
                };
            }).ToList();
        }

        private ApiResponse<List<GetMedicalRecordsResponse>> CreateResponse(
            List<GetMedicalRecordsResponse> result,
            MetaResponse meta,
            bool isUserValid)
        {
            var errorCode = !isUserValid ? GeneralCode.APP_MESSAGE_4033.ToString() : null;
            var successCode = GeneralCode.APP_MESSAGE_2000.ToString();

            return !isUserValid
                ? ApiResponse<List<GetMedicalRecordsResponse>>.Fail(errorCode!)
                : ApiResponse<List<GetMedicalRecordsResponse>>.Success(successCode, result, meta);
        }
    }
}
