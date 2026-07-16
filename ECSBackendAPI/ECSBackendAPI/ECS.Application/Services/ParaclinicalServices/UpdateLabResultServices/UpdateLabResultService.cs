using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ECS.Application.Services.ParaclinicalServices.UpdateLabResultServices
{
    /// <summary>
    /// Applies a partial update to an existing lab / paraclinical result document (status,
    /// clinical conclusion, image url, measurements, etc.). Only the doctor who owns the
    /// underlying medical record may mutate it; everyone else is rejected.
    /// </summary>
    /// <remarks>
    /// Pipeline (each step short-circuits on failure via <see cref="ExecutionState"/>):
    ///   EnsureLabResultIdProvided -> RetrieveUserId -> ResolveAccessAsync -> UpdateAsync
    ///   -> Build. Builds a list of <c>UpdateDefinition</c>s so only the provided fields
    ///   are touched (no full-document replacement).
    /// </remarks>
    public class UpdateLabResultService : IUpdateLabResultService
    {
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _recordRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IMongoDbContext _mongo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateLabResultService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> recordRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IMongoDbContext mongo,
            IHttpContextAccessor httpContextAccessor)
        {
            _recordRepository = recordRepository;
            _doctorRepository = doctorRepository;
            _mongo = mongo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<UpdateLabResultResponse>> Process(UpdateLabResultRequest request)
        {
            // ── Orchestration: each step runs only if no prior step recorded an error.
            var state = new ExecutionState();

            EnsureLabResultIdProvided(request, state);
            if (!state.HasError) RetrieveUserId(state);
            if (!state.HasError) await ResolveAccessAsync(state, request.LabResultId);
            if (!state.HasError) await UpdateAsync(state, request);
            return Build(state);
        }

        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }
            public Guid ActiveUserId { get; set; }
            public Guid DoctorId { get; set; }
            public LabResultDocument? Updated { get; set; }
        }

        /// <summary>
        /// Convenience failure-builder used by all early-exit branches in this service.
        /// </summary>
        private ApiResponse<UpdateLabResultResponse> Fail(ExecutionState state) =>
            ApiResponse<UpdateLabResultResponse>.Fail(
                state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());

        /// <summary>
        /// Ensures a non-empty <c>LabResultId</c> was provided. Sets APP_MESSAGE_4003 otherwise.
        /// </summary>
        private void EnsureLabResultIdProvided(UpdateLabResultRequest request, ExecutionState state)
        {
            if (string.IsNullOrWhiteSpace(request.LabResultId))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4003.ToString();
            }
        }

        /// <summary>
        /// Reads the caller's user id from the JWT principal. On a missing/invalid id the
        /// state is flagged with APP_MESSAGE_4033 (forbidden) so subsequent steps skip work.
        /// </summary>
        private void RetrieveUserId(ExecutionState state)
        {
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
        /// Resolves the active doctor and verifies the targeted lab result belongs to one of
        /// their medical records. Sets APP_MESSAGE_4011 (no doctor profile), APP_MESSAGE_4028
        /// (lab doc not found), APP_MESSAGE_4019 (bad record id), or APP_MESSAGE_4014 (not owner).
        /// </summary>
        private async Task ResolveAccessAsync(ExecutionState state, string labResultId)
        {
            // 1) fetch doctor profile
            var doctor = await _doctorRepository
                .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                .FirstOrDefaultAsync();
            if (doctor == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4011.ToString();
                return;
            }
            state.DoctorId = doctor.Id;

            // 2) fetch the lab document
            var doc = await _mongo.LabResults
                .Find(Builders<LabResultDocument>.Filter.Eq(x => x.Id, labResultId))
                .FirstOrDefaultAsync();
            if (doc == null)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
                return;
            }

            // 3) verify the lab doc belongs to a medical record owned by this doctor
            if (!Guid.TryParse(doc.RecordId, out var recordId))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
                return;
            }
            var record = await _recordRepository
                .FindByCondition(r => r.Id == recordId, trackChanges: false)
                .FirstOrDefaultAsync();
            if (record == null || record.DoctorId != doctor.Id)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
                return;
            }
        }

        /// <summary>
        /// Builds a partial <c>UpdateDefinition</c> from the request fields, persists it with
        /// <c>FindOneAndUpdate</c>, and stores the post-update document on the state.
        /// Always touches <c>UpdatedAt</c>. Bad measurements JSON or a failed update is
        /// surfaced via the state error code rather than thrown.
        /// </summary>
        private async Task UpdateAsync(ExecutionState state, UpdateLabResultRequest req)
        {
            var updates = new List<UpdateDefinition<LabResultDocument>>();
            var b = Builders<LabResultDocument>.Update;

            if (!string.IsNullOrWhiteSpace(req.Status))
                updates.Add(b.Set(x => x.Status, req.Status!.ToUpperInvariant()));
            if (req.ClinicalConclusion != null)
                updates.Add(b.Set(x => x.ClinicalConclusion, req.ClinicalConclusion));
            if (req.ImageUrl != null)
                updates.Add(b.Set(x => x.ImageUrl, req.ImageUrl));
            if (req.TechnicianName != null)
                updates.Add(b.Set(x => x.TechnicianName, req.TechnicianName));
            if (req.MachineName != null)
                updates.Add(b.Set(x => x.MachineName, req.MachineName));
            if (req.ScanPattern != null)
                updates.Add(b.Set(x => x.ScanPattern, req.ScanPattern));
            if (req.PerformedAt.HasValue)
                updates.Add(b.Set(x => x.PerformedAt, req.PerformedAt.Value));
            if (req.Measurements.HasValue && req.Measurements.Value.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    var bson = BsonDocument.Parse(req.Measurements.Value.GetRawText());
                    updates.Add(b.Set(x => x.Measurements, bson));
                }
                catch
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4019.ToString();
                    return;
                }
            }

            updates.Add(b.Set(x => x.UpdatedAt, DateTime.UtcNow));

            try
            {
                var updated = await _mongo.LabResults.FindOneAndUpdateAsync(
                    Builders<LabResultDocument>.Filter.Eq(x => x.Id, req.LabResultId),
                    b.Combine(updates),
                    new FindOneAndUpdateOptions<LabResultDocument> { ReturnDocument = ReturnDocument.After });
                if (updated == null)
                {
                    state.HasError = true;
                    state.ErrorCode = GeneralCode.APP_MESSAGE_4028.ToString();
                    return;
                }
                state.Updated = updated;
            }
            catch
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        /// <summary>
        /// Wraps the updated document into the standard success envelope. Returns the captured
        /// failure code if any earlier step set <c>HasError</c>.
        /// </summary>
        private ApiResponse<UpdateLabResultResponse> Build(ExecutionState state)
        {
            if (state.HasError) return Fail(state);
            var d = state.Updated!;
            return ApiResponse<UpdateLabResultResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                new UpdateLabResultResponse
                {
                    LabResultId = d.Id,
                    Status = d.Status,
                    UpdatedAt = d.UpdatedAt,
                    IsSuccess = true
                });
        }
    }
}