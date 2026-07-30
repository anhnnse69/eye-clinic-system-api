using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Service implementation for retrieving a single medical record detail.
    /// **Refactored (2026-07-15)**: complex form data is fetched from MongoDB
    /// using the SQL <c>MongoDocumentId</c> pointer; Cloudinary fallback remains
    /// for legacy records.
    /// </summary>
    public class GetMedicalRecordDetailService : IGetMedicalRecordDetailService
    {
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorProfileRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IMongoDbContext _mongo;
        private readonly IValidator<GetMedicalRecordDetailRequest> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetMedicalRecordDetailService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorProfileRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IMongoDbContext mongo,
            IValidator<GetMedicalRecordDetailRequest> validator,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _doctorProfileRepository = doctorProfileRepository;
            _patientProfileRepository = patientProfileRepository;
            _mongo = mongo;
            _validator = validator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<GetMedicalRecordDetailResponse>> Process(GetMedicalRecordDetailRequest request)
        {
            var state = new ExecutionState();

            ValidateRequest(request, state);
            RetrieveAuthenticatedUserInfo(state);
            (state.ProfileId, state.IsStaff, state) = await ResolveUserProfileAsync(state);
            (state.Record, state) = await FetchMedicalRecordAsync(request.Id, state);
            ValidateRecordAccess(state);

            bool canEdit = false;
            bool canViewOnly = true;
            string? editRestrictionReason = null;
            if (!state.HasError && state.Record != null)
            {
                var today = DateTime.UtcNow.Date;
                bool isCreatedToday = state.Record.CreatedAt.Date == today || state.Record.Appointment.AppointmentDate.Date == today;

                if (state.IsStaff)
                {
                    canEdit = isCreatedToday;
                    canViewOnly = !canEdit;
                    editRestrictionReason = !isCreatedToday ? "Hồ sơ bệnh án chỉ được phép chỉnh sửa trong ngày tạo. Đã qua ngày nên không thể chỉnh sửa." : null;
                }
                else if (state.UserRole == nameof(UserRole.DOCTOR) && state.Record.DoctorId == state.ProfileId)
                {
                    canEdit = !state.Record.IsLocked && isCreatedToday;
                    canViewOnly = state.Record.IsLocked || !isCreatedToday;
                    if (state.Record.IsLocked)
                    {
                        editRestrictionReason = "Hồ sơ đã bị khóa";
                    }
                    else if (!isCreatedToday)
                    {
                        editRestrictionReason = "Hồ sơ bệnh án chỉ được phép chỉnh sửa trong ngày tạo. Đã qua ngày nên không thể chỉnh sửa.";
                    }
                }
            }

            return await CreateResponseAsync(state, canEdit, canViewOnly, editRestrictionReason);
        }

        // ────────────────────────────────────────────────────────────
        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }

            public Guid ActiveUserId { get; set; }
            public string UserRole { get; set; } = string.Empty;
            public Guid ProfileId { get; set; }
            public bool IsStaff { get; set; }

            public MedicalRecord? Record { get; set; }
            public JsonElement? FormData { get; set; }
            public bool IntegrityValid { get; set; } = true;
        }

        // ────────────────────────────────────────────────────────────
        // Steps
        // ────────────────────────────────────────────────────────────
        private void ValidateRequest(GetMedicalRecordDetailRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            if (!result.IsValid)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4003.ToString();
            }
        }

        private void RetrieveAuthenticatedUserInfo(ExecutionState state)
        {
            if (state.HasError) return;
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);
            if (!parseResult)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
                return;
            }
            state.ActiveUserId = parsedUserId;
            state.UserRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        private async Task<(Guid ProfileId, bool IsStaff, ExecutionState State)> ResolveUserProfileAsync(ExecutionState state)
        {
            if (state.HasError) return (Guid.Empty, false, state);

            Guid profileId = Guid.Empty;
            bool isStaff = false;

            if (state.UserRole == nameof(UserRole.PATIENT))
            {
                var patientProfile = await _patientProfileRepository
                    .FindByCondition(p => p.UserId == state.ActiveUserId, trackChanges: false)
                    .FirstOrDefaultAsync();
                if (patientProfile == null)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
                    return (Guid.Empty, false, state);
                }
                profileId = patientProfile.Id;
            }
            else if (state.UserRole == nameof(UserRole.DOCTOR))
            {
                var doctorProfile = await _doctorProfileRepository
                    .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                    .Include(d => d.User)
                    .FirstOrDefaultAsync();
                if (doctorProfile == null)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
                    return (Guid.Empty, false, state);
                }
                profileId = doctorProfile.Id;
            }
            else if (state.UserRole == nameof(UserRole.CLINIC_ADMIN) ||
                     state.UserRole == nameof(UserRole.RECEPTIONIST) ||
                     state.UserRole == nameof(UserRole.SYSTEM_ADMIN))
            {
                isStaff = true;
            }
            else
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
            }

            return (profileId, isStaff, state);
        }

        private async Task<(MedicalRecord? Record, ExecutionState State)> FetchMedicalRecordAsync(Guid requestId, ExecutionState state)
        {
            if (state.HasError) return (null, state);

            var record = await _medicalRecordRepository
                .FindByCondition(x => x.Id == requestId || x.AppointmentId == requestId, trackChanges: false)
                .AsNoTracking()
                .Include(x => x.Appointment)
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Doctor.Specialty)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
            }
            return (record, state);
        }

        private void ValidateRecordAccess(ExecutionState state)
        {
            if (state.HasError || state.Record == null) return;
            if (state.IsStaff) return;

            if (state.UserRole == nameof(UserRole.PATIENT))
            {
                if (state.Record.PatientId != state.ProfileId)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
                }
            }
            else if (state.UserRole == nameof(UserRole.DOCTOR))
            {
                if (state.Record.DoctorId != state.ProfileId)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
                }
            }
        }

        // ────────────────────────────────────────────────────────────
        // Mongo fetch (form data)
        // ────────────────────────────────────────────────────────────
        private async Task FetchFormDataAsync(ExecutionState state)
        {
            if (state.Record == null) return;
            if (string.IsNullOrEmpty(state.Record.MongoDocumentId))
            {
                // Legacy record without MongoDocumentId — return empty payload.
                state.FormData = JsonDocument.Parse("{}").RootElement;
                return;
            }

            try
            {
                var doc = await _mongo.MedicalRecords
                    .Find(Builders<MedicalRecordDocument>.Filter.Eq(x => x.Id, state.Record.MongoDocumentId))
                    .FirstOrDefaultAsync();

                if (doc == null)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
                    return;
                }

                // BsonDocument → JSON text → JsonElement so FE can consume it directly.
                var json = doc.FormData.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
                state.FormData = JsonDocument.Parse(json).RootElement;

                // Verify SHA-256 integrity.
                var recomputed = MongoDbContext.ComputeSha256(doc.FormData.ToJson());
                state.IntegrityValid = string.Equals(recomputed, doc.Sha256Checksum, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        // ────────────────────────────────────────────────────────────
        // Response
        // ────────────────────────────────────────────────────────────
        private async Task<ApiResponse<GetMedicalRecordDetailResponse>> CreateResponseAsync(
            ExecutionState state,
            bool canEdit, bool canViewOnly, string? editRestrictionReason)
        {
            if (state.HasError)
            {
                return ApiResponse<GetMedicalRecordDetailResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var record = state.Record!;

            await FetchFormDataAsync(state);
            if (state.HasError)
            {
                return ApiResponse<GetMedicalRecordDetailResponse>.Fail(state.ErrorCode!);
            }

            var chiefComplaint = record.ChiefComplaint;
            var summary = record.Summary;

            if (string.IsNullOrEmpty(chiefComplaint) && state.FormData.HasValue)
            {
                chiefComplaint = ExtractChiefComplaint(state.FormData.Value);
            }

            if (string.IsNullOrEmpty(summary) && state.FormData.HasValue)
            {
                summary = ExtractSummary(state.FormData.Value);
            }

            var response = new GetMedicalRecordDetailResponse
            {
                Id = record.Id,
                AppointmentId = record.AppointmentId,
                PatientId = record.PatientId,
                DoctorId = record.DoctorId,
                RecordType = record.RecordType.ToString(),
                Status = record.Status.ToString(),
                ChiefComplaint = chiefComplaint,
                Summary = summary,
                Notes = record.Notes,
                IsLocked = record.IsLocked,
                FinalizedAt = record.FinalizedAt,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,

                PatientFullName = record.Patient.FullName,
                PatientDob = record.Patient.Dob.ToString("dd/MM/yyyy"),
                PatientPhone = record.Patient.PhoneNumber,
                PatientEmail = record.Patient.User?.Email ?? string.Empty,
                PatientGender = record.Patient.Gender.ToString(),
                PatientAddress = record.Patient.Address ?? string.Empty,
                PatientIdentityNumber = record.Patient.IdentityNumber ?? string.Empty,

                DoctorFullName = record.Doctor.User?.FullName ?? "N/A",
                DoctorTitle = record.Doctor.Title,
                DoctorSpecialty = record.Doctor.Specialty?.Name ?? string.Empty,

                AppointmentDate = record.Appointment.AppointmentDate,
                AppointmentStatus = record.Appointment.Status.ToString(),
                AppointmentNotes = record.Appointment.NoteReason,

                // MongoDB-backed payload
                FormData = state.FormData,
                MongoDocumentId = record.MongoDocumentId,
                RecordDataSchemaVersion = record.RecordDataSchemaVersion,
                RecordDataVersion = record.RecordDataVersion,
                RecordDataSizeBytes = record.RecordDataSizeBytes,
                IntegrityValid = state.IntegrityValid,

                CanEdit = canEdit,
                CanViewOnly = canViewOnly,
                EditRestrictionReason = editRestrictionReason
            };

            return ApiResponse<GetMedicalRecordDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }

        private static string? ExtractChiefComplaint(JsonElement formData)
        {
            if (formData.ValueKind != JsonValueKind.Object) return null;
            if (formData.TryGetProperty("benhAn", out var benhAn) && benhAn.ValueKind == JsonValueKind.Object)
            {
                if (benhAn.TryGetProperty("lyDoVaoVien", out var lyDo) && lyDo.ValueKind == JsonValueKind.String)
                    return lyDo.GetString();
            }
            if (formData.TryGetProperty("lyDoVaoVien", out var lyDoDirect) && lyDoDirect.ValueKind == JsonValueKind.String)
                return lyDoDirect.GetString();
            return null;
        }

        private static string? ExtractSummary(JsonElement formData)
        {
            if (formData.ValueKind != JsonValueKind.Object) return null;
            if (formData.TryGetProperty("benhAn", out var benhAn) && benhAn.ValueKind == JsonValueKind.Object)
            {
                if (benhAn.TryGetProperty("summary", out var sum) && sum.ValueKind == JsonValueKind.String)
                    return sum.GetString();
                if (benhAn.TryGetProperty("benhSu", out var benhSu) && benhSu.ValueKind == JsonValueKind.String)
                    return benhSu.GetString();
            }
            return null;
        }
    }
}