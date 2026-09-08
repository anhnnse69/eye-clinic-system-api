using ECS.Infrastructure.Persistence.MongoDb;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ECS.Application.Services.RecordApprovalServices;

public class RecordApprovalService : IRecordApprovalService
{
    private readonly IMongoDbContext _mongoDb;
    private readonly ILogger<RecordApprovalService> _logger;

    public RecordApprovalService(IMongoDbContext mongoDb, ILogger<RecordApprovalService> logger)
    {
        _mongoDb = mongoDb;
        _logger = logger;
    }

    public async Task<RecordApprovalResponseDto> CreateRequestAsync(CreateRecordApprovalRequest request, CancellationToken ct = default)
    {
        var filter = Builders<RecordApprovalDocument>.Filter.Eq(x => x.RecordId, request.RecordId);
        var existing = await _mongoDb.RecordApprovals.Find(filter).FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.PatientName = request.PatientName;
            existing.DoctorId = request.DoctorId;
            existing.DoctorName = request.DoctorName;
            existing.ClinicId = request.ClinicId ?? existing.ClinicId;
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
        var filterBuilder = Builders<RecordApprovalDocument>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(clinicId))
        {
            filter &= filterBuilder.Eq(x => x.ClinicId, clinicId);
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
        return doc != null ? MapToDto(doc) : null;
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
