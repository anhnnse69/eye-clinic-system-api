using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace ECS.Application.Services.ParaclinicalServices.GetLabResultsServices
{
    /// <summary>
    /// Returns the list of lab / paraclinical results (OCT, Visual Field, Ultrasound) attached
    /// to a given medical record. Visibility is role-aware: the doctor who owns the record, the
    /// patient it belongs to, or any clinic staff may read; everyone else is forbidden.
    /// </summary>
    /// <remarks>
    /// Pipeline: ValidateRequest -> RetrieveUserInfo -> ResolveAccessAsync -> LoadLabResultsAsync
    /// -> CreateResponse. Optional filters by <c>LabType</c> and <c>Side</c> are applied in MongoDB.
    /// </remarks>
    public class GetLabResultsService : IGetLabResultsService
    {
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _recordRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientRepository;
        private readonly IMongoDbContext _mongo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetLabResultsService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> recordRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientRepository,
            IMongoDbContext mongo,
            IHttpContextAccessor httpContextAccessor)
        {
            _recordRepository = recordRepository;
            _doctorRepository = doctorRepository;
            _patientRepository = patientRepository;
            _mongo = mongo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<GetLabResultsResponse>> Process(GetLabResultsRequest request)
        {
            // ── Orchestration: short-circuit on the first error recorded by any step.
            var state = new ExecutionState();

            ValidateRequest(request, state);
            if (!state.HasError) RetrieveUserInfo(state);
            if (!state.HasError) await ResolveAccessAsync(state, request.RecordId);
            if (!state.HasError) await LoadLabResultsAsync(state, request);

            return CreateResponse(state);
        }

        /// <summary>
        /// Mutable per-request state shared between pipeline steps so each helper stays
        /// single-purpose and the orchestrator remains linear and readable.
        /// </summary>
        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }
            public string UserRole { get; set; } = string.Empty;
            public Guid ActiveUserId { get; set; }
            public bool IsStaff { get; set; }
            public Guid ProfileId { get; set; }
            public MedicalRecord? Record { get; set; }
            public List<LabResultDocument> Docs { get; set; } = new();
        }

        /// <summary>
        /// Confirms <c>RecordId</c> is a well-formed Guid. Sets APP_MESSAGE_4019 otherwise.
        /// </summary>
        private void ValidateRequest(GetLabResultsRequest req, ExecutionState state)
        {
            if (!Guid.TryParse(req.RecordId, out _))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
            }
        }

        /// <summary>
        /// Reads the JWT principal to extract the active user id and role. On a missing/invalid
        /// id, records APP_MESSAGE_4033 (forbidden) so subsequent steps skip work.
        /// </summary>
        private void RetrieveUserInfo(ExecutionState state)
        {
            var principalId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(principalId, out var userId))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
                return;
            }
            state.ActiveUserId = userId;
            state.UserRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Loads the medical record and authorizes the caller to read its lab results:
        /// the assigned doctor (must match <c>Record.DoctorId</c>), the patient on the record,
        /// or any clinic/system staff. Anyone else is rejected with APP_MESSAGE_4014 or 4033.
        /// </summary>
        private async Task ResolveAccessAsync(ExecutionState state, string recordIdRaw)
        {
            if (state.HasError) return;
            var recordId = Guid.Parse(recordIdRaw);

            var record = await _recordRepository
                .FindByCondition(r => r.Id == recordId, trackChanges: false)
                .FirstOrDefaultAsync();
            if (record == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
                return;
            }
            state.Record = record;

            if (state.UserRole == nameof(UserRole.DOCTOR))
            {
                var doctor = await _doctorRepository
                    .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                    .FirstOrDefaultAsync();
                if (doctor == null)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
                    return;
                }
                state.ProfileId = doctor.Id;
                if (record.DoctorId != doctor.Id)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
                }
            }
            else if (state.UserRole == nameof(UserRole.PATIENT))
            {
                var patient = await _patientRepository
                    .FindByCondition(p => p.UserId == state.ActiveUserId, trackChanges: false)
                    .FirstOrDefaultAsync();
                if (patient == null || record.PatientId != patient.Id)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
                    return;
                }
                state.ProfileId = patient.Id;
            }
            else if (state.UserRole == nameof(UserRole.CLINIC_ADMIN) ||
                     state.UserRole == nameof(UserRole.RECEPTIONIST) ||
                     state.UserRole == nameof(UserRole.SYSTEM_ADMIN))
            {
                state.IsStaff = true;
            }
            else
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
            }
        }

        /// <summary>
        /// Queries MongoDB <c>lab_results</c> for documents linked to the resolved record,
        /// optionally filtered by <c>LabType</c> and <c>Side</c>, sorted newest first.
        /// </summary>
        private async Task LoadLabResultsAsync(ExecutionState state, GetLabResultsRequest req)
        {
            if (state.HasError || state.Record == null) return;

            var filterBuilder = Builders<LabResultDocument>.Filter;
            var filter = filterBuilder.Eq(x => x.RecordId, state.Record.Id.ToString());

            if (!string.IsNullOrWhiteSpace(req.LabType))
                filter &= filterBuilder.Eq(x => x.LabType, req.LabType!.ToUpperInvariant());
            if (!string.IsNullOrWhiteSpace(req.Side))
                filter &= filterBuilder.Eq(x => x.Side, req.Side!.ToUpperInvariant());

            var docs = await _mongo.LabResults
                .Find(filter)
                .SortByDescending(x => x.RequestedAt)
                .ToListAsync();
            state.Docs = docs;
        }

        /// <summary>
        /// Projects the loaded MongoDB documents into <see cref="LabResultSummary"/> DTOs and
        /// wraps them in the standard success envelope. Bson measurements are rendered as
        /// RelaxedExtendedJson so the FE can consume them directly.
        /// </summary>
        private ApiResponse<GetLabResultsResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                return ApiResponse<GetLabResultsResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var settings = new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson };
            var resp = new GetLabResultsResponse
            {
                RecordId = state.Record!.Id.ToString(),
                Count = state.Docs.Count,
                Results = state.Docs.Select(d => new LabResultSummary
                {
                    LabResultId = d.Id,
                    LabType = d.LabType,
                    Side = d.Side,
                    Status = d.Status,
                    MachineName = d.MachineName,
                    ScanPattern = d.ScanPattern,
                    ImageUrl = d.ImageUrl,
                    ClinicalConclusion = d.ClinicalConclusion,
                    RequestedAt = d.RequestedAt,
                    PerformedAt = d.PerformedAt,
                    UpdatedAt = d.UpdatedAt,
                    Measurements = JsonDocument.Parse(d.Measurements.ToJson(settings)).RootElement,
                    AiPrediction = d.AiPrediction != null
                        ? JsonDocument.Parse(d.AiPrediction.ToJson(settings)).RootElement
                        : null
                }).ToList()
            };

            return ApiResponse<GetLabResultsResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), resp);
        }
    }
}