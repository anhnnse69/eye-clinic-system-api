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

namespace ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices
{
    /// <summary>
    /// Creates a paraclinical / lab request and persists the result document in MongoDB
    /// (collection <c>lab_results</c>). UC42 (OCT), UC43 (Visual Field), UC44 (Ultrasound)
    /// all share this service — the modality is encoded in <see cref="LabType"/>.
    /// </summary>
    /// <remarks>
    /// Pipeline (each step short-circuits on failure via <see cref="ExecutionState"/>):
    ///   ValidateRequest -> RetrieveUserId -> ParseRecordId -> ResolveRecordAsync
    ///   -> ResolveDoctorAsync -> InsertLabDocumentAsync -> CreateResponse.
    /// </remarks>
    public class CreateLabRequestService : ICreateLabRequestService
    {
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IMongoDbContext _mongo;
        private readonly IValidator<CreateLabRequestRequest> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateLabRequestService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IMongoDbContext mongo,
            IValidator<CreateLabRequestRequest> validator,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _doctorRepository = doctorRepository;
            _mongo = mongo;
            _validator = validator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<CreateLabRequestResponse>> Process(CreateLabRequestRequest request)
        {
            // ── Orchestration: each step runs only if no prior step recorded an error.
            var state = new ExecutionState();

            ValidateRequest(request, state);
            RetrieveUserId(state);
            ParseRecordId(request.RecordId, state);
            if (!state.HasError) await ResolveRecordAsync(state);
            if (!state.HasError) await ResolveDoctorAsync(state);
            if (!state.HasError) await InsertLabDocumentAsync(request, state);

            return CreateResponse(state);
        }

        /// <summary>
        /// Mutable per-request state shared between pipeline steps so each helper can
        /// stay single-purpose and the orchestrator stays linear and readable.
        /// </summary>
        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }
            public Guid ActiveUserId { get; set; }
            public Guid RecordId { get; set; }
            public MedicalRecord? Record { get; set; }
            public DoctorProfile? DoctorProfile { get; set; }
            public LabResultDocument? Inserted { get; set; }
        }

        /// <summary>
        /// Runs FluentValidation against the inbound request and records a validation error
        /// (APP_MESSAGE_4019) on the shared state if it fails.
        /// </summary>
        private void ValidateRequest(CreateLabRequestRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            if (!result.IsValid)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
            }
        }

        /// <summary>
        /// Reads the caller's user id from the JWT principal. On a missing/invalid id the
        /// state is flagged with APP_MESSAGE_4033 (forbidden) so subsequent steps skip work.
        /// </summary>
        private void RetrieveUserId(ExecutionState state)
        {
            if (state.HasError) return;
            var principalId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(principalId, out var userId))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
                return;
            }
            state.ActiveUserId = userId;
        }

        /// <summary>
        /// Validates and stores the parsed <see cref="Guid"/> record id from the request payload.
        /// </summary>
        private void ParseRecordId(string raw, ExecutionState state)
        {
            if (state.HasError) return;
            if (!Guid.TryParse(raw, out var id))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
                return;
            }
            state.RecordId = id;
        }

        /// <summary>
        /// Loads the <see cref="MedicalRecord"/> referenced by the request and stores it on the state.
        /// On miss, records APP_MESSAGE_4028 (record not found).
        /// </summary>
        private async Task ResolveRecordAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var record = await _medicalRecordRepository
                .FindByCondition(r => r.Id == state.RecordId, trackChanges: false)
                .FirstOrDefaultAsync();
            if (record == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
                return;
            }
            state.Record = record;
        }

        /// <summary>
        /// Resolves the active doctor's profile so the lab document can be attributed to them.
        /// On miss, records APP_MESSAGE_4011 (doctor profile not found / inactive).
        /// </summary>
        private async Task ResolveDoctorAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var doctor = await _doctorRepository
                .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                .FirstOrDefaultAsync();
            if (doctor == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4011.ToString();
                return;
            }
            state.DoctorProfile = doctor;
        }

        /// <summary>
        /// Builds the <see cref="LabResultDocument"/> from the request + resolved profile data and
        /// inserts it into the MongoDB <c>lab_results</c> collection. Bad measurements JSON or a
        /// failed insert records an appropriate error code but does not throw.
        /// </summary>
        private async Task InsertLabDocumentAsync(CreateLabRequestRequest request, ExecutionState state)
        {
            if (state.HasError || state.Record == null || state.DoctorProfile == null) return;

            BsonDocument measurements;
            try
            {
                measurements = request.Measurements.ValueKind == JsonValueKind.Object
                    ? BsonDocument.Parse(request.Measurements.GetRawText())
                    : new BsonDocument();
            }
            catch (Exception)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
                return;
            }

            var status = string.IsNullOrWhiteSpace(request.Status) ? "REQUESTED" : request.Status!.ToUpperInvariant();
            var requestedAt = DateTime.UtcNow;

            var doc = new LabResultDocument
            {
                RecordId = state.Record.Id.ToString(),
                AppointmentId = state.Record.AppointmentId.ToString(),
                PatientId = state.Record.PatientId.ToString(),
                DoctorId = state.DoctorProfile.Id.ToString(),
                LabType = request.LabType.ToUpperInvariant(),
                Side = request.Side?.ToUpperInvariant(),
                RequestedAt = requestedAt,
                Status = status,
                Indication = request.Indication,
                RequestedByTechnicianId = request.TechnicianId,
                TechnicianName = request.TechnicianName,
                MachineName = request.MachineName,
                ScanPattern = request.ScanPattern,
                Measurements = measurements,
                ClinicalConclusion = request.ClinicalConclusion,
                ImageUrl = request.ImageUrl,
                CreatedAt = requestedAt,
                UpdatedAt = requestedAt
            };

            try
            {
                await _mongo.LabResults.InsertOneAsync(doc);
                state.Inserted = doc;
            }
            catch (Exception)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        /// <summary>
        /// Builds the final API response from the execution state. Returns a failure envelope
        /// when an error was recorded at any earlier step, otherwise returns the inserted
        /// lab document summary with APP_MESSAGE_2005.
        /// </summary>
        private ApiResponse<CreateLabRequestResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                return ApiResponse<CreateLabRequestResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var doc = state.Inserted!;
            var resp = new CreateLabRequestResponse
            {
                LabResultId = doc.Id,
                RecordId = doc.RecordId,
                LabType = doc.LabType,
                Side = doc.Side,
                Status = doc.Status,
                RequestedAt = doc.RequestedAt,
                IsSuccess = true
            };
            return ApiResponse<CreateLabRequestResponse>.Success(
                GeneralCode.APP_MESSAGE_2005.ToString(), resp);
        }
    }
}