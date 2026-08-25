namespace ECS.Infrastructure.Persistence.MongoDb;

/// <summary>
/// Strongly-typed binding for the <c>MongoDb</c> section in <c>appsettings.json</c>.
/// </summary>
public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = "ecs_clinic";
    public string MedicalRecordsCollection { get; set; } = "medical_records";
    public string LabResultsCollection { get; set; } = "lab_results";
    public string AiSuggestionsCollection { get; set; } = "ai_suggestions";
    public string RecordApprovalsCollection { get; set; } = "record_approvals";

    public string? ApplicationName { get; set; } = "ecs-backend";
    public int ServerSelectionTimeoutSeconds { get; set; } = 10;
}