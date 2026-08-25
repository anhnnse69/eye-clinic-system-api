using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ECS.Application.Services.MedicalRecordsServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Service implementation for creating medical records.
    /// **Refactored (2026-07-15)**: the full form payload (Medical Record +
    /// Clinical Examination + Paraclinical sub-sections) is stored as a single
    /// document in MongoDB (collection <c>medical_records</c>). SQL Server
    /// still keeps the relational metadata + <c>MongoDocumentId</c> pointer
    /// for permissioning / queries.
    /// </summary>
    public class CreateMedicalRecordService : ICreateMedicalRecordService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> _medicalRecordRepositoryAsync;
        private readonly IRepositoryBaseAsync<Queue, Guid, AppDbContext> _queueRepository;
        private readonly IMongoDbContext _mongo;
        private readonly IValidator<CreateMedicalRecordRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreateMedicalRecordService> _logger;

        public CreateMedicalRecordService(
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> medicalRecordRepositoryAsync,
            IRepositoryBaseAsync<Queue, Guid, AppDbContext> queueRepository,
            IMongoDbContext mongo,
            IValidator<CreateMedicalRecordRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreateMedicalRecordService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _medicalRecordRepositoryAsync = medicalRecordRepositoryAsync;
            _queueRepository = queueRepository;
            _mongo = mongo;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ApiResponse<CreateMedicalRecordResponse>> Process(CreateMedicalRecordRequest request)
        {
            var state = new ExecutionState();

            // 1. Validate
            ValidateRequest(request, state);
            // 2. Extract user
            RetrieveAuthenticatedUserId(state);
            // 3. Parse IDs
            ParseAppointmentId(request.AppointmentId, state);
            ParsePatientId(request.PatientId, state);
            // 4. Verify entities
            await GetAppointmentAsync(state);
            await GetDoctorProfileAsync(state);
            // 5. Check duplicates
            CheckMedicalRecordExists(state);
            // 6. Create + persist (Mongo + SQL)
            await CreateMedicalRecordAsync(request, state);
            // 7. Update appointment / queue
            await UpdateStatusesAsync(state);

            return CreateResponse(state, request);
        }

        // ────────────────────────────────────────────────────────────
        // Execution state
        // ────────────────────────────────────────────────────────────
        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }

            public Guid ActiveUserId { get; set; }
            public Guid AppointmentId { get; set; }
            public Guid PatientId { get; set; }

            public Appointment? Appointment { get; set; }
            public DoctorProfile? DoctorProfile { get; set; }
            public MedicalRecord? CreatedMedicalRecord { get; set; }

            public string? PatientName { get; set; }
            public string? DoctorName { get; set; }
            public string? RecordTypeLabel { get; set; }
            public string? MongoDocumentId { get; set; }
        }

        // ────────────────────────────────────────────────────────────
        // Steps
        // ────────────────────────────────────────────────────────────
        private void ValidateRequest(CreateMedicalRecordRequest request, ExecutionState state)
        {
            _logger.LogInformation("Starting validation. AppointmentId: {AppointmentId}, PatientId: {PatientId}, RecordType: {RecordType}",
                request.AppointmentId, request.PatientId, request.RecordType);

            var result = _validator.Validate(request);
            state.HasError = !result.IsValid;
            if (!result.IsValid)
            {
                _logger.LogWarning("Validation failed. Errors: {Errors}",
                    string.Join(", ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
            }
        }

        private void RetrieveAuthenticatedUserId(ExecutionState state)
        {
            if (state.HasError) return;
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("Extracted user ID from claims: {UserId}", principalIdValue);
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);
            state.ActiveUserId = parseResult ? parsedUserId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        private void ParseAppointmentId(string appointmentId, ExecutionState state)
        {
            if (state.HasError) return;
            var parseResult = Guid.TryParse(appointmentId, out var parsedId);
            state.AppointmentId = parseResult ? parsedId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        private void ParsePatientId(string patientId, ExecutionState state)
        {
            if (state.HasError) return;
            var parseResult = Guid.TryParse(patientId, out var parsedId);
            state.PatientId = parseResult ? parsedId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        private async Task GetAppointmentAsync(ExecutionState state)
        {
            if (state.HasError) return;
            // Track changes so the appointment entity can be updated and saved
            // inside the same transaction as the MedicalRecord insert.
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == state.AppointmentId, trackChanges: true)
                .Include(a => a.Patient)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync();

            state.Appointment = appointment;
            state.PatientName = appointment?.Patient?.FullName;
            state.DoctorName = appointment?.Doctor?.User?.FullName;

            if (appointment == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4012.ToString();
            }
        }

        private async Task GetDoctorProfileAsync(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null) return;
            var doctorProfile = await _doctorRepository
                .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                .Include(d => d.User)
                .FirstOrDefaultAsync();
            state.DoctorProfile = doctorProfile;

            if (doctorProfile == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4011.ToString();
            }
        }

        private void CheckMedicalRecordExists(ExecutionState state)
        {
            if (state.HasError) return;
            var existing = _medicalRecordRepository
                .FindByCondition(r => r.AppointmentId == state.AppointmentId, trackChanges: false)
                .FirstOrDefaultAsync().GetAwaiter().GetResult();

            if (existing != null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4027.ToString();
            }
        }

        private async Task CreateMedicalRecordAsync(CreateMedicalRecordRequest request, ExecutionState state)
        {
            if (state.HasError || state.Appointment == null || state.DoctorProfile == null) return;

            Enum.TryParse<RecordType>(request.RecordType, true, out var recordType);
            var recordId = Guid.NewGuid();

            // ─── Serialize form data ────────────────────────────────
            var jsonContent = JsonSerializer.Serialize(request.FormData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // ─── Build MongoDB document ─────────────────────────────
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

            var checksum = MongoDbContext.ComputeSha256(jsonContent);
            var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(jsonContent);

            var mongoDoc = new MedicalRecordDocument
            {
                RecordId = recordId.ToString(),
                AppointmentId = state.AppointmentId.ToString(),
                PatientId = state.Appointment.PatientId.ToString(),
                DoctorId = state.DoctorProfile.Id.ToString(),
                RecordType = recordType.ToString(),
                SchemaVersion = "1.0",
                Version = 1,
                FormData = formDataBson,
                Sha256Checksum = checksum,
                SizeBytes = sizeBytes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _mongo.MedicalRecords.InsertOneAsync(mongoDoc);
                state.MongoDocumentId = mongoDoc.Id;
                _logger.LogInformation("MongoDB insert successful. DocumentId: {DocumentId}", mongoDoc.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB insert failed. AppointmentId: {AppointmentId}, PatientId: {PatientId}",
                    state.AppointmentId, state.PatientId);
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
                return;
            }

            // ─── Insert MedicalRecord (metadata + mongo pointer) ────
            var medicalRecord = new MedicalRecord
            {
                Id = recordId,
                AppointmentId = state.AppointmentId,
                PatientId = state.Appointment.PatientId,
                DoctorId = state.DoctorProfile.Id,
                RecordType = recordType,
                Status = RecordStatus.DRAFT,
                IsLocked = false,
                Notes = request.Notes,
                ChiefComplaint = ExtractChiefComplaint(request.FormData),
                Summary = ExtractSummary(request.FormData),
                MongoDocumentId = mongoDoc.Id,
                RecordDataSchemaVersion = "1.0",
                RecordDataVersion = 1,
                RecordDataSizeBytes = sizeBytes,
                RecordDataChecksum = checksum,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MedicalRecords.Add(medicalRecord);

            // ─── Stage Appointment + Queue status changes in the same DbContext ────
            // Creating a medical record only means the clinical-examination phase
            // (Step 1–4 of the 6-step EMR workflow) has been saved. The queue/
            // appointment must NOT be marked COMPLETED here — that only happens
            // when CompleteQueueService succeeds, which itself enforces Step 5
            // (Medical record summary) + Step 6 (Prescription / Glasses Rx).
            // Instead we move the appointment/queue to IN_PROGRESS so the UI
            // can show that consultation is in progress.
            if (state.Appointment.Status == AppointmentStatus.PENDING
                || state.Appointment.Status == AppointmentStatus.CONFIRMED
                || state.Appointment.Status == AppointmentStatus.BOOKED
                || state.Appointment.Status == AppointmentStatus.ARRIVED)
            {
                state.Appointment.Status = AppointmentStatus.IN_PROGRESS;
                state.Appointment.UpdatedAt = DateTime.UtcNow;
                // Re-fetch through _context so the row is tracked by the EF Core
                // change tracker; otherwise EF Core 10's in-memory provider (and
                // SQL Server with optimistic concurrency) reports
                // DbUpdateConcurrencyException because the update is targeting
                // a row it doesn't see tracked.
                var trackedAppointment = await _context.Appointments.FirstOrDefaultAsync(
                    a => a.Id == state.Appointment.Id);
                if (trackedAppointment != null)
                {
                    trackedAppointment.Status = AppointmentStatus.IN_PROGRESS;
                    trackedAppointment.UpdatedAt = DateTime.UtcNow;
                    _context.Appointments.Update(trackedAppointment);
                }
            }

            var queue = await _context.Queues.FirstOrDefaultAsync(q => q.AppointmentId == state.AppointmentId);
            if (queue != null && queue.Status != QueueStatus.IN_PROGRESS && queue.Status != QueueStatus.COMPLETED)
            {
                queue.Status = QueueStatus.IN_PROGRESS;
                _context.Queues.Update(queue);
            }

            try
            {
                await _context.SaveChangesAsync();
                state.CreatedMedicalRecord = medicalRecord;
                state.RecordTypeLabel = GetRecordTypeLabel(recordType);
                _logger.LogInformation(
                    "SQL SaveChanges successful (single transaction). MedicalRecordId: {MedicalId}, AppointmentId: {AppointmentId} -> IN_PROGRESS (Bước 1–3 done, Bước 5+6 still pending until CompleteQueueService).",
                    medicalRecord.Id, state.AppointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL SaveChanges failed. MedicalRecordId: {RecordId}, AppointmentId: {AppointmentId}",
                    recordId, state.AppointmentId);
                // Roll back Mongo insert so we don't leave orphan documents behind.
                try
                {
                    await _mongo.MedicalRecords.DeleteOneAsync(
                        MongoDB.Driver.Builders<MedicalRecordDocument>.Filter.Eq(x => x.Id, mongoDoc.Id));
                    _logger.LogInformation("MongoDB rollback successful after SQL failure. DocumentId: {DocumentId}", mongoDoc.Id);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "MongoDB rollback failed. DocumentId: {DocumentId}", mongoDoc.Id);
                    /* swallow secondary cleanup failure */
                }
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        // ────────────────────────────────────────────────────────────
        // Status update is now staged INSIDE CreateMedicalRecordAsync so it
        // commits within the same SQL transaction as the MedicalRecord insert.
        // This method is kept for backwards compatibility and is a no-op when
        // the staged change already happened.
        //
        // IMPORTANT: Save-medical-record must NOT mark the appointment/queue as
        // COMPLETED. The 6-step EMR workflow only completes after Step 5
        // (Medical record summary) + Step 6 (Prescription / Glasses Rx) are
        // filled in, which is enforced by CompleteQueueService.
        // ────────────────────────────────────────────────────────────
        private async Task UpdateStatusesAsync(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null) return;
            if (state.Appointment.Status == AppointmentStatus.COMPLETED)
            {
                _logger.LogDebug(
                    "UpdateStatusesAsync skipped: AppointmentId {AppointmentId} already marked COMPLETED.",
                    state.AppointmentId);
                return;
            }
            if (state.Appointment.Status == AppointmentStatus.IN_PROGRESS)
            {
                _logger.LogDebug(
                    "UpdateStatusesAsync skipped: AppointmentId {AppointmentId} already marked IN_PROGRESS by the main transaction.",
                    state.AppointmentId);
                return;
            }

            try
            {
                // Only promote PENDING/CONFIRMED/BOOKED/ARRIVED -> IN_PROGRESS.
                // Do NOT flip anything to COMPLETED here.
                state.Appointment.Status = AppointmentStatus.IN_PROGRESS;
                state.Appointment.UpdatedAt = DateTime.UtcNow;
                // Re-fetch through _context so the row is tracked by the EF Core
                // change tracker. Avoids DbUpdateConcurrencyException from
                // attaching an untracked instance.
                var trackedAppointment = await _context.Appointments.FirstOrDefaultAsync(
                    a => a.Id == state.Appointment.Id);
                if (trackedAppointment != null)
                {
                    trackedAppointment.Status = AppointmentStatus.IN_PROGRESS;
                    trackedAppointment.UpdatedAt = DateTime.UtcNow;
                    _context.Appointments.Update(trackedAppointment);
                }

                var queue = await _context.Queues.FirstOrDefaultAsync(q => q.AppointmentId == state.AppointmentId);
                if (queue != null && queue.Status != QueueStatus.IN_PROGRESS && queue.Status != QueueStatus.COMPLETED)
                {
                    queue.Status = QueueStatus.IN_PROGRESS;
                    _context.Queues.Update(queue);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "UpdateStatusesAsync fallback commit successful. AppointmentId: {AppointmentId} -> IN_PROGRESS",
                    state.AppointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "UpdateStatusesAsync fallback failed for AppointmentId: {AppointmentId}",
                    state.AppointmentId);
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

        private ApiResponse<CreateMedicalRecordResponse> CreateResponse(ExecutionState state, CreateMedicalRecordRequest request)
        {
            if (state.HasError)
            {
                return ApiResponse<CreateMedicalRecordResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var response = new CreateMedicalRecordResponse
            {
                MedicalRecordId = state.CreatedMedicalRecord?.Id.ToString() ?? string.Empty,
                PatientName = state.PatientName,
                RecordTypeLabel = state.RecordTypeLabel,
                AppointmentDate = state.Appointment?.AppointmentDate.ToString("dd/MM/yyyy"),
                DoctorName = state.DoctorName,
                CreatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                MongoDocumentId = state.MongoDocumentId,
                IsSuccess = true
            };
            return ApiResponse<CreateMedicalRecordResponse>.Success(GeneralCode.APP_MESSAGE_2005.ToString(), response);
        }
    }
}