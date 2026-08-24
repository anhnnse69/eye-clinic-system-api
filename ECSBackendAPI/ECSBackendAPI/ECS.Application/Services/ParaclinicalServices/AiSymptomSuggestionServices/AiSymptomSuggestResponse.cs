namespace ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices
{
    public class AiSymptomSuggestResponse
    {
        public string? SuggestionId { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PredictedDisease { get; set; }
        public double? Confidence { get; set; }
        public Dictionary<string, double>? AllProbabilities { get; set; }
        public string? RiskLevel { get; set; }
        public string? Disclaimer { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsSuccess { get; set; }
    }
}
