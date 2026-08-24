using ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices;
using ECS.Infrastructure.Ai;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="AiSymptomSuggestService"/> tests.
    /// </summary>
    public static class AiSymptomSuggestMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static AiSymptomSuggestRequest GetValidRequest(
            string symptom = "Eye_Pain",
            string duration = "acute",
            string painLevel = "high")
        {
            return new AiSymptomSuggestRequest
            {
                RecordId = "rec-123",
                AppointmentId = "app-456",
                Symptom = symptom,
                Duration = duration,
                PainLevel = painLevel,
                EyeRedness = "yes",
                BlurredVision = "yes",
                LightSensitivity = "yes",
                Discharge = "no",
                Tearing = "yes",
                Swelling = "no",
                ForeignBodySensation = "no",
                Floaters = "no",
                Halos = "yes",
                EyePressure = "high",
                CornealOpacity = "no",
                PupilResponse = "normal",
                NightBlindness = "no",
                DoubleVision = "no",
                EyeTurning = "no",
                WhiteReflection = "no",
                Headache = "yes",
                Nausea = "no",
                Age = "adult",
                Diabetes = "no",
                Hypertension = "no",
                FamilyHistory = "no"
            };
        }

        public static AiSymptomPredictResponse GetSymptomPredictResponse(
            string taskId = "task-sym-001",
            string status = "completed",
            string predictedDisease = "Acute_Glaucoma",
            double confidence = 0.92,
            string riskLevel = "HIGH",
            Dictionary<string, double>? allProbabilities = null,
            string? errorCode = null,
            string? errorMessage = null,
            string? createdAt = null,
            string? completedAt = null)
        {
            return new AiSymptomPredictResponse
            {
                TaskId = taskId,
                Status = status,
                PredictedDisease = predictedDisease,
                Confidence = confidence,
                RiskLevel = riskLevel,
                Disclaimer = "Preliminary AI assessment",
                AllProbabilities = allProbabilities ?? new Dictionary<string, double>
                {
                    { "Acute_Glaucoma", 0.92 },
                    { "Conjunctivitis", 0.08 }
                },
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddSeconds(-2).ToString("O"),
                CompletedAt = completedAt ?? DateTime.UtcNow.ToString("O")
            };
        }
    }
}
