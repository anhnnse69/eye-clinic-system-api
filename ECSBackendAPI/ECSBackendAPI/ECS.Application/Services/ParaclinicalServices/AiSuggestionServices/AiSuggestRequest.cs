namespace ECS.Application.Services.ParaclinicalServices.AiSuggestionServices
{
    public class AiSuggestRequest
    {
        /// <summary>
        /// Optional medical record to attach the suggestion to.
        /// </summary>
        public string? RecordId { get; set; }

        /// <summary>
        /// Optional lab result (paraclinical) to attach the suggestion to.
        /// </summary>
        public string? LabResultId { get; set; }

        /// <summary>
        /// Raw OCT image bytes (the doctor uploads from the FE).
        /// </summary>
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// MIME type for the image — defaults to image/jpeg when omitted.
        /// </summary>
        public string MimeType { get; set; } = "image/jpeg";
    }

    public class AiSuggestResponse
    {
        public string SuggestionId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PredictedClass { get; set; }
        public double? Confidence { get; set; }
        public string? ImageUrl { get; set; }
        public Dictionary<string, double>? AllProbabilities { get; set; }
        public string? ModelVersion { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? ProcessingTimeMs { get; set; }
        public bool IsSuccess { get; set; }
    }
}