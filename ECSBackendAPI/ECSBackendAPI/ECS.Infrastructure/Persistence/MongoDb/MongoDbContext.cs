using System.Security.Cryptography;
using System.Text;
using ECS.Domain.Entities.Paraclinical;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ECS.Infrastructure.Persistence.MongoDb;

/// <summary>
/// Strongly-typed wrapper around <see cref="IMongoDatabase"/> used by repositories
/// that store medical-record JSON payloads and paraclinical / lab-result documents.
/// </summary>
public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<MedicalRecordDocument> MedicalRecords { get; }
    IMongoCollection<LabResultDocument> LabResults { get; }
    IMongoCollection<AiSuggestionDocument> AiSuggestions { get; }
}

public class MongoDbContext : IMongoDbContext
{
    private static int _conventionsRegistered;

    public IMongoDatabase Database { get; }
    public IMongoCollection<MedicalRecordDocument> MedicalRecords { get; }
    public IMongoCollection<LabResultDocument> LabResults { get; }
    public IMongoCollection<AiSuggestionDocument> AiSuggestions { get; }

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        EnsureConventionsRegistered();

        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.ConnectionString))
            throw new InvalidOperationException("MongoDb:ConnectionString is not configured.");
        if (string.IsNullOrWhiteSpace(opt.Database))
            throw new InvalidOperationException("MongoDb:Database is not configured.");

        var settings = MongoClientSettings.FromConnectionString(opt.ConnectionString);
        settings.ApplicationName = opt.ApplicationName ?? "ecs-backend";
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(opt.ServerSelectionTimeoutSeconds);

        var client = new MongoClient(settings);
        Database = client.GetDatabase(opt.Database);

        MedicalRecords = Database.GetCollection<MedicalRecordDocument>(opt.MedicalRecordsCollection);
        LabResults = Database.GetCollection<LabResultDocument>(opt.LabResultsCollection);
        AiSuggestions = Database.GetCollection<AiSuggestionDocument>(opt.AiSuggestionsCollection);
    }

    private static void EnsureConventionsRegistered()
    {
        if (Interlocked.Exchange(ref _conventionsRegistered, 1) == 1) return;

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        };
        ConventionRegistry.Register("ecs-mongo", pack, _ => true);

        // Serialize Guid as string for human-readable storage.
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

// ─── Strongly-typed Mongo documents ─────────────────────────────────────

/// <summary>
/// Stores the full medical record form payload (Bệnh Án + Khám bệnh + Paraclinical)
/// as a single document. The relational row in SQL Server still holds metadata and
/// the <c>MongoDocumentId</c> string. This replaces the Cloudinary JSON-envelope
/// design (2026-07-15 refactor) for tighter security and richer queries.
/// </summary>
public class MedicalRecordDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RecordId { get; set; } = string.Empty;       // = SQL `MedicalRecords.Id`
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;     // MS21_TRAUMA … MS26_PEDIATRIC
    public string SchemaVersion { get; set; } = "1.0";
    public int Version { get; set; } = 1;
    public BsonDocument FormData { get; set; } = new();        // free-form JSON
    public string Sha256Checksum { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One document per paraclinical request (OCT, VisualField, Ultrasound B-scan,
/// general Lab order, etc.). Multi-side measurements (OD/OS) are stored in
/// the <c>Measurements</c> BsonDocument so each modality keeps its own schema.
/// </summary>
public class LabResultDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string RecordId { get; set; } = string.Empty;       // SQL MedicalRecord.Id
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;

    /// <summary>OCT | VISUAL_FIELD | ULTRASOUND | GENERAL_LAB.</summary>
    public string LabType { get; set; } = string.Empty;
    public string? Side { get; set; }                            // OD | OS | BOTH

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PerformedAt { get; set; }
    public string Status { get; set; } = "REQUESTED";          // REQUESTED | IN_PROGRESS | COMPLETED | CANCELLED

    public string? Indication { get; set; }                     // ICD/clinical indication
    public string? RequestedByTechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public string? MachineName { get; set; }
    public string? ScanPattern { get; set; }

    public BsonDocument Measurements { get; set; } = new();   // modality-specific numeric findings
    public string? ClinicalConclusion { get; set; }             // doctor's free-form conclusion
    public string? ImageUrl { get; set; }
    public BsonDocument? AiPrediction { get; set; }             // optional AI result blob

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persists AI predictions against a medical record / lab request so they can be
/// audited and re-fetched without re-running the model.
/// </summary>
public class AiSuggestionDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string? RecordId { get; set; }
    public string? LabResultId { get; set; }

    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;

    public string? PredictedClass { get; set; }
    public double? Confidence { get; set; }
    public BsonDocument AllProbabilities { get; set; } = new();
    public string Status { get; set; } = "pending";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? ProcessingTimeMs { get; set; }
}