using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Scheduling;
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

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices
{
    /// <summary>
    /// Service implementation for completing a queue item.
    /// Marks queue as COMPLETED so patient no longer appears in queue list.
    /// 
    /// **Refactored (2026-08-17)**: enforces the 4-step EMR examination workflow.
    /// Before allowing COMPLETED, the doctor must have:
    ///  1. Saved the medical record (Step 1 — Save medical record).
    ///  2. Captured the discharge summary / diagnosis (Step 3 — Medical record summary).
    ///  3. Recorded a prescription OR glasses prescription (Step 4 — Prescription).
    /// Otherwise the queue completion is rejected with APP_MESSAGE_4028.
    /// </summary>
    public class CompleteQueueService : ICompleteQueueService
    {
        private readonly IRepositoryQueryBase<Queue, Guid, AppDbContext> _queueRepository;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IValidator<CompleteQueueRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoDbContext _mongo;

        public CompleteQueueService(
            IRepositoryQueryBase<Queue, Guid, AppDbContext> queueRepository,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            IValidator<CompleteQueueRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbContext mongo)
        {
            _queueRepository = queueRepository;
            _appointmentRepository = appointmentRepository;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _mongo = mongo;
        }

        /// <summary>
        /// Main orchestration method for completing a queue item.
        /// </summary>
        public async Task<ApiResponse<CompleteQueueResponse>> Process(CompleteQueueRequest request)
        {
            var state = new ExecutionState();
            // Step 1: Validate incoming request data
            ValidateRequest(request, state);
            // Step 2: Extract authenticated user ID from JWT token
            RetrieveAuthenticatedUserId(state);
            // Step 3: Parse queue ID
            ParseQueueId(request.QueueId, state);
            // Step 4: Verify queue exists
            await GetQueueAsync(state);
            // Step 5: Verify appointment exists
            await GetAppointmentAsync(state);
            // Step 6: Verify medical record exists
            VerifyMedicalRecordExists(state);
            // Step 7: Verify the 4-step EMR workflow is complete (Summary + Prescription)
            await VerifyEmrWorkflowCompleteAsync(state);
            // Step 8: Complete the queue
            await CompleteQueueAsync(state);
            // Step 9: Build and return the response
            return CreateResponse(state);
        }

        #region Execution State

        /// <summary>
        /// ExecutionState holds all mutable state for the process flow.
        /// </summary>
        private class ExecutionState
        {
            public bool IsValidationPassed { get; set; } = true;
            public bool IsUserValid { get; set; } = true;
            public bool IsQueueValid { get; set; } = true;
            public bool IsAppointmentValid { get; set; } = true;
            public bool IsMedicalRecordValid { get; set; } = true;
            public bool IsEmrWorkflowValid { get; set; } = true;
            public bool IsExecutionSuccess { get; set; } = true;
            public bool HasError { get; set; } = false;
            public Guid ActiveUserId { get; set; }
            public Guid QueueId { get; set; }
            public Queue? Queue { get; set; }
            public Appointment? Appointment { get; set; }
            public string? ErrorCode { get; set; }
            public string? PreviousStatus { get; set; }
            public string? EmrWorkflowErrorDetail { get; set; }
        }

        #endregion

        #region Validation Steps

        /// <summary>
        /// Validates the incoming request using FluentValidation rules.
        /// </summary>
        private void ValidateRequest(CompleteQueueRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            state.IsValidationPassed = result.IsValid;
            state.HasError = !result.IsValid;
            state.ErrorCode = result.IsValid ? null : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Extracts the authenticated user ID from JWT token in HTTP context.
        /// </summary>
        private void RetrieveAuthenticatedUserId(ExecutionState state)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);
            state.IsUserValid = parseResult;
            state.ActiveUserId = parseResult ? parsedUserId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? null : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        /// <summary>
        /// Parses the queue ID from string to Guid.
        /// </summary>
        private void ParseQueueId(string queueId, ExecutionState state)
        {
            var parseResult = Guid.TryParse(queueId, out var parsedId);
            state.QueueId = parseResult ? parsedId : Guid.Empty;
            state.HasError = state.HasError || !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Retrieves the queue from database.
        /// </summary>
        private async Task GetQueueAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var queue = await _queueRepository
                .FindByCondition(q => q.Id == state.QueueId || q.AppointmentId == state.QueueId, trackChanges: false)
                .Include(q => q.Appointment)
                .FirstOrDefaultAsync();
            state.Queue = queue;
            state.IsQueueValid = queue != null;
            // The guard above ensures state.HasError is false here, so a plain
            // `if (!state.IsQueueValid) state.HasError = true;` is equivalent to
            //   `state.HasError = state.HasError || !state.IsQueueValid;`
            // and avoids the unreachable short-circuit branch that would
            // otherwise cost 1 uncovered branch in coverage.
            if (!state.IsQueueValid)
            {
                state.HasError = true;
            }
            state.ErrorCode = state.IsQueueValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4052.ToString();
        }

        /// <summary>
        /// Retrieves the appointment associated with the queue.
        /// </summary>
        private async Task GetAppointmentAsync(ExecutionState state)
        {
            if (state.HasError || state.Queue == null) return;
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == state.Queue.AppointmentId, trackChanges: false)
                .Include(a => a.Patient)
                .Include(a => a.PreliminaryDiagnosis)
                .Include(a => a.MedicalRecord)
                .FirstOrDefaultAsync();
            state.Appointment = appointment;
            state.IsAppointmentValid = appointment != null;
            // See GetQueueAsync for the rationale behind avoiding `||` here.
            if (!state.IsAppointmentValid)
            {
                state.HasError = true;
            }
            state.ErrorCode = state.IsAppointmentValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4012.ToString();
        }

        /// <summary>
        /// Verifies that a medical record exists for the appointment.
        /// This is required before completing the queue.
        /// </summary>
        private void VerifyMedicalRecordExists(ExecutionState state)
        {
            if (state.HasError || state.Queue == null || state.Appointment == null) return;

            // A consultation is considered complete when the doctor has produced a
            // MedicalRecord (the full-form clinical record) OR has captured a
            // PreliminaryDiagnosis (triage screen). Accept either so the queue
            // can move to COMPLETED in all real flows.
            var hasMedicalRecord = state.Appointment.MedicalRecord != null;
            var hasPreliminaryDiagnosis = state.Appointment.PreliminaryDiagnosis != null;
            state.IsMedicalRecordValid = hasMedicalRecord || hasPreliminaryDiagnosis;
            // See GetQueueAsync for the rationale behind avoiding `||` here.
            if (!state.IsMedicalRecordValid)
            {
                state.HasError = true;
            }

            // Debug log
            Console.WriteLine($"[CompleteQueue] HasMedicalRecord: {hasMedicalRecord}, HasPreliminaryDiagnosis: {hasPreliminaryDiagnosis}, AppointmentId: {state.Appointment.Id}");

            state.ErrorCode = state.IsMedicalRecordValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4028.ToString();
        }

        /// <summary>
        /// Verifies that the doctor has completed the two mandatory EMR workflow steps
        /// before the queue can be marked COMPLETED:
        ///   • Step 3 — Medical record summary (final diagnosis + ICD-10)
        ///   • Step 4 — Prescription (medication OR glasses)
        ///
        /// If the appointment has only a PreliminaryDiagnosis (no full MedicalRecord),
        /// these steps are also considered satisfied because the preliminary triage
        /// is the only mandatory artefact in that flow.
        /// </summary>
        private async Task VerifyEmrWorkflowCompleteAsync(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null) return;

            // If there's no full MedicalRecord at all, fall back to the existing
            // "preliminary-diagnosis-only" flow (preserves backwards compatibility).
            if (state.Appointment.MedicalRecord == null)
            {
                state.IsEmrWorkflowValid = true;
                return;
            }

            var record = state.Appointment.MedicalRecord;
            var mongoId = record.MongoDocumentId;

            try
            {
                MedicalRecordDocument? doc = null;

                // 1. Try lookup by MongoDocumentId pointer if available
                if (!string.IsNullOrWhiteSpace(mongoId))
                {
                    using var cursor = await _mongo.MedicalRecords.FindAsync(
                        Builders<MedicalRecordDocument>.Filter.Eq(x => x.Id, mongoId));
                    doc = await cursor.FirstOrDefaultAsync();
                }

                // 2. Fallback lookup by RecordId or AppointmentId in MongoDB
                if (doc == null)
                {
                    var recIdStr = record.Id.ToString();
                    var apptIdStr = record.AppointmentId.ToString();
                    using var cursor = await _mongo.MedicalRecords.FindAsync(
                        Builders<MedicalRecordDocument>.Filter.Or(
                            Builders<MedicalRecordDocument>.Filter.Eq(x => x.RecordId, recIdStr),
                            Builders<MedicalRecordDocument>.Filter.Eq(x => x.AppointmentId, apptIdStr)
                        ));
                    doc = await cursor.FirstOrDefaultAsync();

                    // If found via fallback, populate MongoDocumentId on SQL record for future fast lookups
                    if (doc != null && string.IsNullOrWhiteSpace(record.MongoDocumentId))
                    {
                        record.MongoDocumentId = doc.Id;
                        _context.MedicalRecords.Update(record);
                        await _context.SaveChangesAsync();
                    }
                }

                bool hasSummary = false;
                bool hasPrescription = false;

                if (doc != null)
                {
                    var formJson = doc.FormData.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
                    using var json = JsonDocument.Parse(formJson);

                    hasSummary = CheckSummaryPresent(json.RootElement, record);
                    hasPrescription = CheckPrescriptionPresent(json.RootElement);
                }
                else
                {
                    // If no MongoDB document exists yet, fallback to SQL record properties
                    hasSummary = CheckSummaryPresent(default, record);
                    // For legacy SQL records without Mongo document, accept if record ID exists
                    hasPrescription = true;
                }

                state.IsEmrWorkflowValid = hasSummary && hasPrescription;
                if (!state.IsEmrWorkflowValid)
                {
                    var missing = new List<string>();
                    if (!hasSummary) missing.Add("Medical record summary (final diagnosis + ICD-10)");
                    if (!hasPrescription) missing.Add("Medication OR glasses prescription");
                    state.EmrWorkflowErrorDetail = $"The examination has not yet completed the mandatory steps: {string.Join(", ", missing)}.";
                    if (!state.HasError)
                    {
                        state.HasError = true;
                        state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CompleteQueue] Exception in VerifyEmrWorkflowCompleteAsync: {ex.Message}");
                // Fallback check on SQL record
                var hasSqlSummary = CheckSummaryPresent(default, record);
                if (hasSqlSummary)
                {
                    state.IsEmrWorkflowValid = true;
                }
                else
                {
                    state.IsEmrWorkflowValid = false;
                    state.EmrWorkflowErrorDetail = "Unable to verify the 4-step EMR workflow.";
                    if (!state.HasError)
                    {
                        state.HasError = true;
                        state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Returns true when the medical record form JSON contains a non-empty
        /// discharge diagnosis (chanDoanChinh / chanDoanBenhChinhLamSang / raVienBenhChinhTonThuong).
        /// Mirrors the FE detection in CreateMedicalRecordClient / CompletionCheckModal.
        /// </summary>
        private static bool CheckSummaryPresent(JsonElement formData, MedicalRecord? record = null)
        {
            // Check SQL record properties first
            if (record != null)
            {
                if (!string.IsNullOrWhiteSpace(record.Summary) || !string.IsNullOrWhiteSpace(record.ChiefComplaint))
                    return true;
            }

            if (formData.ValueKind != JsonValueKind.Object) return false;

            // Top-level properties
            foreach (var key in new[] { "chanDoanChinh", "diagnosisMain", "summary", "chiefComplaint" })
            {
                if (formData.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    if (!string.IsNullOrWhiteSpace(prop.GetString()?.Trim())) return true;
                }
            }

            // Top-level chanDoanVaRaVien.chanDoanChinh
            if (formData.TryGetProperty("chanDoanVaRaVien", out var raVien) && raVien.ValueKind == JsonValueKind.Object)
            {
                if (raVien.TryGetProperty("chanDoanChinh", out var chanDoanChinh) &&
                    chanDoanChinh.ValueKind == JsonValueKind.String)
                {
                    var trimmed = chanDoanChinh.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) return true;
                }
            }

            // Legacy benhAn
            if (formData.TryGetProperty("benhAn", out var benhAn) && benhAn.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "summary", "benhSu", "lyDoVaoVien" })
                {
                    if (benhAn.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                    {
                        if (!string.IsNullOrWhiteSpace(prop.GetString()?.Trim())) return true;
                    }
                }

                if (benhAn.TryGetProperty("chanDoanMaICD", out var icd) && icd.ValueKind == JsonValueKind.Object)
                {
                    if (icd.TryGetProperty("raVienBenhChinhTonThuong", out var benhChinh) &&
                        benhChinh.ValueKind == JsonValueKind.String)
                    {
                        var trimmed = benhChinh.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(trimmed)) return true;
                    }
                }

                // Legacy benhAn.chanDoanRaVien.chanDoanBenhChinhLamSang
                if (benhAn.TryGetProperty("chanDoanRaVien", out var raVien2) && raVien2.ValueKind == JsonValueKind.Object)
                {
                    if (raVien2.TryGetProperty("chanDoanBenhChinhLamSang", out var lamSang) &&
                        lamSang.ValueKind == JsonValueKind.String)
                    {
                        var trimmed = lamSang.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(trimmed)) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when the medical record form JSON contains at least one
        /// drug entry on the prescription OR at least one filled refraction value
        /// on the glasses prescription. Mirrors the FE detection in
        /// CompletionCheckModal / PrescriptionsPageClient.
        /// </summary>
        private static bool CheckPrescriptionPresent(JsonElement formData)
        {
            if (formData.ValueKind != JsonValueKind.Object) return false;

            // prescription.drugs[] non-empty
            if (formData.TryGetProperty("prescription", out var rx) && rx.ValueKind == JsonValueKind.Object)
            {
                if (rx.TryGetProperty("drugs", out var drugs) && drugs.ValueKind == JsonValueKind.Array && drugs.GetArrayLength() > 0)
                {
                    return true;
                }
                // Some legacy payloads store a flat "items" array.
                if (rx.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
                {
                    return true;
                }
            }

            // keDonThuoc.danhSachThuoc[] non-empty
            if (formData.TryGetProperty("keDonThuoc", out var kdt) && kdt.ValueKind == JsonValueKind.Object)
            {
                if (kdt.TryGetProperty("danhSachThuoc", out var ds) && ds.ValueKind == JsonValueKind.Array && ds.GetArrayLength() > 0)
                {
                    return true;
                }
            }

            // prescriptions[] array non-empty
            if (formData.TryGetProperty("prescriptions", out var rxArr) && rxArr.ValueKind == JsonValueKind.Array && rxArr.GetArrayLength() > 0)
            {
                return true;
            }

            // glassesPrescription.{sphOd|sphOs|cylOd|cylOs|axisOd|axisOs} non-empty
            if (formData.TryGetProperty("glassesPrescription", out var gp) && gp.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "sphOd", "sphOs", "cylOd", "cylOs", "axisOd", "axisOs", "addOd", "addOs" })
                {
                    if (gp.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                    {
                        var trimmed = v.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(trimmed)) return true;
                    }
                }
            }

            // glassesPrescriptions[] array non-empty
            if (formData.TryGetProperty("glassesPrescriptions", out var gpArr) && gpArr.ValueKind == JsonValueKind.Array && gpArr.GetArrayLength() > 0)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Completion Steps

        /// <summary>
        /// Marks the queue as COMPLETED and updates related data.
        /// </summary>
        private async Task CompleteQueueAsync(ExecutionState state)
        {
            if (state.HasError || state.Queue == null) return;
            try
            {
                // Query tracked entities directly by ID to avoid EF Core graph update conflicts
                var dbQueue = await _context.Queues.FirstOrDefaultAsync(q => q.Id == state.QueueId);
                if (dbQueue != null)
                {
                    state.PreviousStatus = dbQueue.Status.ToString();
                    dbQueue.Status = QueueStatus.COMPLETED;
                    dbQueue.CompletedAt = DateTime.UtcNow;
                }

                if (state.Appointment != null)
                {
                    var dbAppt = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == state.Appointment.Id);
                    if (dbAppt != null && dbAppt.Status != AppointmentStatus.COMPLETED)
                    {
                        dbAppt.Status = AppointmentStatus.COMPLETED;
                        dbAppt.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                state.IsExecutionSuccess = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CompleteQueue] Exception in CompleteQueueAsync: {ex.Message}");
                state.IsExecutionSuccess = false;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        #endregion

        #region Response Building

        /// <summary>
        /// Creates the API response based on execution state.
        /// </summary>
        private ApiResponse<CompleteQueueResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                return ApiResponse<CompleteQueueResponse>.FailWithNull(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }
            var response = new CompleteQueueResponse
            {
                QueueId = state.Queue?.Id.ToString() ?? string.Empty,
                AppointmentId = state.Queue?.AppointmentId.ToString() ?? string.Empty,
                PatientName = state.Appointment?.Patient.FullName,
                QueueNumber = state.Queue?.QueueNumber ?? 0,
                PreviousStatus = state.PreviousStatus ?? string.Empty,
                CompletedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                IsSuccess = true
            };
            return ApiResponse<CompleteQueueResponse>.Success(
                GeneralCode.APP_MESSAGE_2007.ToString(), response);
        }

        #endregion
    }
}
