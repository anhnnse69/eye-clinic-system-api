namespace ECS.Application.Services.ParaclinicalServices.GetLabResultsServices
{
    public class GetLabResultsRequest
    {
        public string RecordId { get; set; } = string.Empty;
        public string? LabType { get; set; }
        public string? Side { get; set; }
    }

    public class GetLabResultsResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<LabResultSummary> Results { get; set; } = new();
    }

    public class LabResultSummary
    {
        public string LabResultId { get; set; } = string.Empty;
        public string LabType { get; set; } = string.Empty;
        public string? Side { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public string? ScanPattern { get; set; }
        public string? ImageUrl { get; set; }
        public string? ClinicalConclusion { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? PerformedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Modality-specific measurements as raw JSON (rnflAverage, cmt, axialLength, etc.).
        /// </summary>
        public System.Text.Json.JsonElement? Measurements { get; set; }

        /// <summary>
        /// AI prediction snapshot (if any) as raw JSON.
        /// </summary>
        public System.Text.Json.JsonElement? AiPrediction { get; set; }
    }
}