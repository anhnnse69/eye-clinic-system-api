using System.Security.Claims;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ECS.Application.Services.RecordApprovalServices;

public class RecordApprovalService : IRecordApprovalService
{
    private readonly IMongoDbContext _mongoDb;
    private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
    private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RecordApprovalService> _logger;

    public RecordApprovalService(
        IMongoDbContext mongoDb,
        IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
        IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RecordApprovalService> logger)
    {
        _mongoDb = mongoDb;
        _staffClinicRepository = staffClinicRepository;
        _medicalRecordRepository = medicalRecordRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<RecordApprovalResponseDto> CreateRequestAsync(CreateRecordApprovalRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClinicId))
        {
            var callerClinicId = await RetrieveUserClinicIdAsync(ct);
            if (!string.IsNullOrWhiteSpace(callerClinicId))
            {
                request.ClinicId = callerClinicId;
            }
            else if (Guid.TryParse(request.RecordId, out var recGuid))
            {
                var rec = await _medicalRecordRepository
                    .FindByCondition(x => x.Id == recGuid)
                    .Include(x => x.Doctor)
                    .FirstOrDefaultAsync(ct);
                if (rec?.Doctor != null)
                {
                    request.ClinicId = rec.Doctor.ClinicId.ToString();
                }
            }
        }

        var filter = Builders<RecordApprovalDocument>.Filter.Eq(x => x.RecordId, request.RecordId);
        var existing = await _mongoDb.RecordApprovals.Find(filter).FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.PatientName = request.PatientName;
            existing.DoctorId = request.DoctorId;
            existing.DoctorName = request.DoctorName;
            existing.ClinicId = !string.IsNullOrWhiteSpace(request.ClinicId) ? request.ClinicId : existing.ClinicId;
            existing.Reason = request.Reason;
            existing.PermissionDoc = request.PermissionDoc;
            existing.AttachedFileName = request.AttachedFileName;
            existing.Status = "PENDING";
            existing.RequestedAt = DateTime.UtcNow;
            existing.ReviewedAt = null;
            existing.ReviewedBy = null;
            existing.ReviewNote = null;

            await _mongoDb.RecordApprovals.ReplaceOneAsync(filter, existing, cancellationToken: ct);
            return MapToDto(existing);
        }

        var doc = new RecordApprovalDocument
        {
            RecordId = request.RecordId,
            PatientName = request.PatientName,
            DoctorId = request.DoctorId,
            DoctorName = request.DoctorName,
            ClinicId = request.ClinicId,
            Reason = request.Reason,
            PermissionDoc = request.PermissionDoc,
            AttachedFileName = request.AttachedFileName,
            Status = "PENDING",
            RequestedAt = DateTime.UtcNow
        };

        await _mongoDb.RecordApprovals.InsertOneAsync(doc, cancellationToken: ct);
        _logger.LogInformation("Created new RecordApprovalDocument in MongoDb for RecordId={RecordId}, ClinicId={ClinicId}", request.RecordId, request.ClinicId);

        return MapToDto(doc);
    }

    public async Task<List<RecordApprovalResponseDto>> GetRequestsAsync(string? status = null, string? search = null, string? clinicId = null, CancellationToken ct = default)
    {
        await BackfillMissingClinicIdsAsync(ct);

        var userClinicId = await RetrieveUserClinicIdAsync(ct);
        var userRole = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

        string? effectiveClinicId = clinicId;

        if (userRole != "SYSTEM_ADMIN" && !string.IsNullOrEmpty(userClinicId))
        {
            effectiveClinicId = userClinicId;
        }
        else if (userRole == "CLINIC_ADMIN" && string.IsNullOrEmpty(userClinicId))
        {
            return new List<RecordApprovalResponseDto>();
        }

        var filterBuilder = Builders<RecordApprovalDocument>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(effectiveClinicId))
        {
            filter &= filterBuilder.Eq(x => x.ClinicId, effectiveClinicId);
        }
        else if (userRole == "CLINIC_ADMIN")
        {
            return new List<RecordApprovalResponseDto>();
        }

        if (!string.IsNullOrWhiteSpace(status) && status.ToUpperInvariant() != "ALL")
        {
            filter &= filterBuilder.Eq(x => x.Status, status.ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim();
            filter &= filterBuilder.Or(
                filterBuilder.Regex(x => x.PatientName, new MongoDB.Bson.BsonRegularExpression(searchLower, "i")),
                filterBuilder.Regex(x => x.DoctorName, new MongoDB.Bson.BsonRegularExpression(searchLower, "i")),
                filterBuilder.Regex(x => x.PermissionDoc, new MongoDB.Bson.BsonRegularExpression(searchLower, "i"))
            );
        }

        var docs = await _mongoDb.RecordApprovals.Find(filter)
            .SortByDescending(x => x.RequestedAt)
            .ToListAsync(ct);

        return docs.Select(MapToDto).ToList();
    }

    public async Task<RecordApprovalResponseDto?> GetByRecordIdAsync(string recordId, CancellationToken ct = default)
    {
        var filter = Builders<RecordApprovalDocument>.Filter.Eq(x => x.RecordId, recordId);
        var doc = await _mongoDb.RecordApprovals.Find(filter).FirstOrDefaultAsync(ct);

        if (doc == null) return null;

        var userClinicId = await RetrieveUserClinicIdAsync(ct);
        var userRole = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == "CLINIC_ADMIN" && !string.IsNullOrEmpty(userClinicId) && !string.IsNullOrEmpty(doc.ClinicId))
        {
            if (!string.Equals(doc.ClinicId, userClinicId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Clinic Admin user associated with ClinicId={UserClinicId} attempted to access RecordApproval belonging to ClinicId={DocClinicId}", userClinicId, doc.ClinicId);
                return null;
            }
        }

        return MapToDto(doc);
    }

    public async Task<RecordApprovalResponseDto?> ApproveRequestAsync(string recordId, string? reviewerId = null, CancellationToken ct = default)
    {
        var filter = Builders<RecordApprovalDocument>.Filter.Eq(x => x.RecordId, recordId);
        var doc = await _mongoDb.RecordApprovals.Find(filter).FirstOrDefaultAsync(ct);

        if (doc == null) return null;

        doc.Status = "APPROVED";
        doc.ReviewedAt = DateTime.UtcNow;
        doc.ReviewedBy = reviewerId ?? "ClinicAdmin";

        await _mongoDb.RecordApprovals.ReplaceOneAsync(filter, doc, cancellationToken: ct);
        _logger.LogInformation("Approved RecordApprovalDocument in MongoDb for RecordId={RecordId}", recordId);

        return MapToDto(doc);
    }

    public async Task<RecordApprovalResponseDto?> RejectRequestAsync(string recordId, string? reviewerId = null, string? reviewNote = null, CancellationToken ct = default)
    {
        var filter = Builders<RecordApprovalDocument>.Filter.Eq(x => x.RecordId, recordId);
        var doc = await _mongoDb.RecordApprovals.Find(filter).FirstOrDefaultAsync(ct);

        if (doc == null) return null;

        doc.Status = "REJECTED";
        doc.ReviewedAt = DateTime.UtcNow;
        doc.ReviewedBy = reviewerId ?? "ClinicAdmin";
        doc.ReviewNote = reviewNote;

        await _mongoDb.RecordApprovals.ReplaceOneAsync(filter, doc, cancellationToken: ct);
        _logger.LogInformation("Rejected RecordApprovalDocument in MongoDb for RecordId={RecordId}", recordId);

        return MapToDto(doc);
    }

    public async Task<bool> CheckPermissionAsync(string recordId, CancellationToken ct = default)
    {
        var filter = Builders<RecordApprovalDocument>.Filter.And(
            Builders<RecordApprovalDocument>.Filter.Eq(x => x.RecordId, recordId),
            Builders<RecordApprovalDocument>.Filter.Eq(x => x.Status, "APPROVED")
        );
        var doc = await _mongoDb.RecordApprovals.Find(filter).FirstOrDefaultAsync(ct);
        return doc != null;
    }

    public async Task<bool> ResetApprovalAsync(string recordId, CancellationToken ct = default)
    {
        var filter = Builders<RecordApprovalDocument>.Filter.Eq(x => x.RecordId, recordId);
        var doc = await _mongoDb.RecordApprovals.Find(filter).FirstOrDefaultAsync(ct);

        if (doc == null) return false;

        doc.Status = "RESET";
        doc.ReviewedAt = DateTime.UtcNow;

        await _mongoDb.RecordApprovals.ReplaceOneAsync(filter, doc, cancellationToken: ct);
        _logger.LogInformation("Reset RecordApprovalDocument in MongoDb for RecordId={RecordId}", recordId);

        return true;
    }

    private async Task<string?> RetrieveUserClinicIdAsync(CancellationToken ct = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null) return null;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return null;

        var staffClinic = await _staffClinicRepository
            .FindByCondition(x => x.UserId == userId && x.IsActive)
            .FirstOrDefaultAsync(ct);

        return staffClinic?.ClinicId.ToString();
    }

    private async Task BackfillMissingClinicIdsAsync(CancellationToken ct = default)
    {
        try
        {
            var filterMissing = Builders<RecordApprovalDocument>.Filter.Or(
                Builders<RecordApprovalDocument>.Filter.Eq(x => x.ClinicId, null),
                Builders<RecordApprovalDocument>.Filter.Eq(x => x.ClinicId, "")
            );

            var missingDocs = await _mongoDb.RecordApprovals.Find(filterMissing).ToListAsync(ct);
            if (missingDocs.Count == 0) return;

            foreach (var doc in missingDocs)
            {
                string? resolvedClinicId = null;

                if (Guid.TryParse(doc.RecordId, out var recGuid))
                {
                    var rec = await _medicalRecordRepository
                        .FindByCondition(x => x.Id == recGuid)
                        .Include(x => x.Doctor)
                        .FirstOrDefaultAsync(ct);

                    if (rec?.Doctor != null)
                    {
                        resolvedClinicId = rec.Doctor.ClinicId.ToString();
                    }
                }

                if (string.IsNullOrEmpty(resolvedClinicId) && Guid.TryParse(doc.DoctorId, out var docUserId))
                {
                    var staff = await _staffClinicRepository
                        .FindByCondition(x => x.UserId == docUserId && x.IsActive)
                        .FirstOrDefaultAsync(ct);

                    if (staff != null)
                    {
                        resolvedClinicId = staff.ClinicId.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(resolvedClinicId))
                {
                    doc.ClinicId = resolvedClinicId;
                    var docFilter = Builders<RecordApprovalDocument>.Filter.Eq(x => x.Id, doc.Id);
                    await _mongoDb.RecordApprovals.ReplaceOneAsync(docFilter, doc, cancellationToken: ct);
                    _logger.LogInformation("Backfilled missing ClinicId={ClinicId} for RecordApproval Document Id={Id}", resolvedClinicId, doc.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to backfill missing ClinicIds for RecordApprovals.");
        }
    }

    private static RecordApprovalResponseDto MapToDto(RecordApprovalDocument doc)
    {
        return new RecordApprovalResponseDto
        {
            Id = doc.Id,
            RecordId = doc.RecordId,
            PatientName = doc.PatientName,
            DoctorId = doc.DoctorId,
            DoctorName = doc.DoctorName,
            ClinicId = doc.ClinicId,
            Reason = doc.Reason,
            PermissionDoc = doc.PermissionDoc,
            AttachedFileName = doc.AttachedFileName,
            Status = doc.Status,
            RequestedAt = doc.RequestedAt,
            ReviewedAt = doc.ReviewedAt,
            ReviewedBy = doc.ReviewedBy,
            ReviewNote = doc.ReviewNote
        };
    }
}

