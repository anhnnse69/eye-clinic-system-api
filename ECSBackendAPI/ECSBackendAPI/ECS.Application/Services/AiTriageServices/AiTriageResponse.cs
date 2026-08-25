namespace ECS.Application.Services.AiTriageServices
{
    /// <summary>
    /// AI Triage Response — Output to FE
    /// </summary>
    public class AiTriageResponse
    {
        public string TaskId { get; set; } = Guid.NewGuid().ToString();
        public string Status { get; set; } = "COMPLETED";
        
        // Primary prediction
        public string? PredictedDisease { get; set; }
        public double? Confidence { get; set; }
        public string? RiskLevel { get; set; }
        
        // Differential diagnoses (top 3)
        public List<DifferentialDiagnosis> Differentials { get; set; } = new();
        
        // All probabilities for display
        public Dictionary<string, double>? AllProbabilities { get; set; }
        
        // Medical disclaimer
        public string Disclaimer { get; set; } = 
            "Preliminary AI assessment based on symptoms only. Does NOT replace professional eye care consultation.";
        
        // Error handling
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Success flag
        public bool IsSuccess { get; set; } = true;
    }

    /// <summary>
    /// Differential diagnosis from AI
    /// </summary>
    public class DifferentialDiagnosis
    {
        public string Disease { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}
