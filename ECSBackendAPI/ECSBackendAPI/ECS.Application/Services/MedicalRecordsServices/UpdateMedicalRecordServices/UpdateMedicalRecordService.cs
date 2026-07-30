using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Service implementation for updating medical records.
    /// **Refactored (2026-07-20)**: follows the same MongoDB-backed JSON envelope
    /// pattern as CreateMedicalRecordService. The form payload is stored in MongoDB
    /// (collection: medical_records); SQL Server keeps relational metadata + pointer.
    /// </summary>
    public class UpdateMedicalRecordService : IUpdateMedicalRecordService
    {
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> _medicalRecordRepositoryAsync;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IMongoDbContext _mongo;
        private readonly IValidator<UpdateMedicalRecordRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateMedicalRecordService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> medicalRecordRepositoryAsync,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IMongoDbContext mongo,
            IValidator<UpdateMedicalRecordRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _medicalRecordRepositoryAsync = medicalRecordRepositoryAsync;
            _doctorRepository = doctorRepository;
            _mongo = mongo;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<UpdateMedicalRecordResponse>> Process(Guid recordId, UpdateMedicalRecordRequest request)
        {
            var state = new ExecutionState();

            // 1. Validate
            ValidateRequest(request, state);

            // 2. Extract user from JWT
            RetrieveAuthenticatedUserId(state);

            // 3. Fetch existing MedicalRecord
            await FetchMedicalRecordAsync(recordId, state);

            // 4. Verify doctor ownership
            await VerifyDoctorOwnershipAsync(state);

            // 5. Check record is not locked
            CheckRecordLockStatus(state);

            // 6. Update dual-storage (MongoDB + SQL)
            await UpdateMedicalRecordAsync(request, state);

            return CreateResponse(state);
        }

        // ────────────────────────────────────────────────────────────
        // Execution state
        // ────────────────────────────────────────────────────────────
        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }

            public Guid ActiveUserId { get; set; }
            public MedicalRecord? MedicalRecord { get; set; }
            public DoctorProfile? DoctorProfile { get; set; }

            public string? PatientName { get; set; }
            public string? DoctorName { get; set; }
            public string? RecordTypeLabel { get; set; }
        }

        // ────────────────────────────────────────────────────────────
        // Steps
        // ────────────────────────────────────────────────────────────
        private void ValidateRequest(UpdateMedicalRecordRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            state.HasError = !result.IsValid;
            if (!result.IsValid)
            {
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
            }
        }

        private void RetrieveAuthenticatedUserId(ExecutionState state)
        {
            if (state.HasError) return;
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);
            state.ActiveUserId = parseResult ? parsedUserId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        private async Task FetchMedicalRecordAsync(Guid recordId, ExecutionState state)
        {
            if (state.HasError) return;

            var record = await _medicalRecordRepository
                .FindByCondition(r => r.Id == recordId, trackChanges: false)
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Patient)
                .Include(r => r.Doctor)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync();

            state.MedicalRecord = record;
            state.PatientName = record?.Appointment?.Patient?.FullName;
            state.DoctorName = record?.Doctor?.User?.FullName;

            if (record == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4004.ToString();
            }
        }

        private async Task VerifyDoctorOwnershipAsync(ExecutionState state)
        {
            if (state.HasError || state.MedicalRecord == null) return;

            var doctorProfile = await _doctorRepository
                .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                .Include(d => d.User)
                .FirstOrDefaultAsync();

            state.DoctorProfile = doctorProfile;

            // Only the doctor who created the record can update it
            if (doctorProfile == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4011.ToString();
                return;
            }

            if (state.MedicalRecord.DoctorId != doctorProfile.Id)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
            }
        }

        private void CheckRecordLockStatus(ExecutionState state)
        {
            if (state.HasError || state.MedicalRecord == null) return;

            if (state.MedicalRecord.IsLocked)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
                return;
            }

            var today = DateTime.UtcNow.Date;
            bool isCreatedToday = state.MedicalRecord.CreatedAt.Date == today || (state.MedicalRecord.Appointment != null && state.MedicalRecord.Appointment.AppointmentDate.Date == today);
            if (!isCreatedToday)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
            }
        }

        private async Task UpdateMedicalRecordAsync(UpdateMedicalRecordRequest request, ExecutionState state)
        {
            if (state.HasError || state.MedicalRecord == null) return;

            var record = state.MedicalRecord;

            // ─── Serialize form data ────────────────────────────────
            var jsonContent = JsonSerializer.Serialize(request.FormData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // ─── Parse JSON → BsonDocument ────────────────────────
            BsonDocument formDataBson;
            try
            {
                formDataBson = BsonDocument.Parse(jsonContent);
            }
            catch (Exception)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
                return;
            }

            var newChecksum = MongoDbContext.ComputeSha256(jsonContent);
            var newSizeBytes = System.Text.Encoding.UTF8.GetByteCount(jsonContent);
            var now = DateTime.UtcNow;

            // ─── Update MongoDB document ───────────────────────────
            var mongoFilter = MongoDB.Driver.Builders<MedicalRecordDocument>.Filter.Eq(x => x.Id, record.MongoDocumentId);
            var mongoUpdate = MongoDB.Driver.Builders<MedicalRecordDocument>.Update
                .Set(x => x.FormData, formDataBson)
                .Set(x => x.Sha256Checksum, newChecksum)
                .Set(x => x.SizeBytes, newSizeBytes)
                .Set(x => x.UpdatedAt, now)
                .Inc(x => x.Version, 1);

            UpdateResult mongoResult;
            try
            {
                mongoResult = await _mongo.MedicalRecords.UpdateOneAsync(mongoFilter, mongoUpdate);

                if (mongoResult.MatchedCount == 0)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4004.ToString();
                    return;
                }
            }
            catch (Exception)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
                return;
            }

            // ─── Update SQL metadata ───────────────────────────────
            record.Notes = request.Notes ?? record.Notes;
            record.ChiefComplaint = ExtractChiefComplaint(request.FormData);
            record.Summary = ExtractSummary(request.FormData);
            record.UpdatedAt = now;
            record.RecordDataVersion += 1;
            record.RecordDataChecksum = newChecksum;
            record.RecordDataSizeBytes = newSizeBytes;

            _context.MedicalRecords.Update(record);

            try
            {
                await _context.SaveChangesAsync();
                state.RecordTypeLabel = GetRecordTypeLabel(record.RecordType);
            }
            catch (Exception)
            {
                // Roll back MongoDB update so data stays consistent
                try
                {
                    var revertUpdate = MongoDB.Driver.Builders<MedicalRecordDocument>.Update
                        .Set(x => x.UpdatedAt, record.UpdatedAt)
                        .Set(x => x.Version, record.RecordDataVersion - 1);
                    await _mongo.MedicalRecords.UpdateOneAsync(mongoFilter, revertUpdate);
                }
                catch
                {
                    /* swallow secondary rollback failure */
                }

                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        // ────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────
        private static string? ExtractChiefComplaint(JsonElement formData)
        {
            if (formData.ValueKind != JsonValueKind.Object) return null;
            if (!formData.TryGetProperty("benhAn", out var benhAn)) return null;
            if (!benhAn.TryGetProperty("lyDoVaoVien", out var lyDo)) return null;
            return lyDo.ValueKind == JsonValueKind.String ? lyDo.GetString() : null;
        }

        private static string? ExtractSummary(JsonElement formData)
        {
            if (formData.ValueKind != JsonValueKind.Object) return null;
            if (!formData.TryGetProperty("benhAn", out var benhAn)) return null;
            if (benhAn.TryGetProperty("summary", out var sum) && sum.ValueKind == JsonValueKind.String)
                return sum.GetString();
            var lyDo = ExtractChiefComplaint(formData);
            return lyDo is { Length: > 0 } ? lyDo[..Math.Min(200, lyDo.Length)] : null;
        }

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

        private ApiResponse<UpdateMedicalRecordResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                return ApiResponse<UpdateMedicalRecordResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var response = new UpdateMedicalRecordResponse
            {
                MedicalRecordId = state.MedicalRecord?.Id.ToString() ?? string.Empty,
                PatientName = state.PatientName,
                RecordTypeLabel = state.RecordTypeLabel,
                AppointmentDate = state.MedicalRecord?.Appointment?.AppointmentDate.ToString("dd/MM/yyyy"),
                DoctorName = state.DoctorName,
                UpdatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                IsSuccess = true
            };

            return ApiResponse<UpdateMedicalRecordResponse>.Success(GeneralCode.APP_MESSAGE_2006.ToString(), response);
        }
    }
}
