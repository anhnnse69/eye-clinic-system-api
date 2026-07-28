using ECS.Application.Services.ParaclinicalServices.AiSuggestionServices;
using ECS.Infrastructure.Ai;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="AiSuggestService"/> tests.
    /// </summary>
    public static class AiSuggestMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static AiSuggestRequest GetValidRequest(
            string? recordId = "rec-123",
            string? labResultId = "lab-456",
            byte[]? imageBytes = null,
            string mimeType = "image/png")
        {
            return new AiSuggestRequest
            {
                RecordId = recordId,
                LabResultId = labResultId,
                ImageBytes = imageBytes ?? new byte[] { 0x1, 0x2, 0x3, 0x4 },
                MimeType = mimeType
            };
        }

        public static AiPredictTask GetPredictTask(
            string taskId = "task-001",
            string status = "completed",
            string? predictedClass = "DME",
            double? confidence = 0.95,
            Dictionary<string, double>? allProbabilities = null,
            string? modelVersion = "v1.0.0",
            string? errorCode = null,
            string? errorMessage = null,
            DateTime? createdAt = null,
            DateTime? completedAt = null,
            int? processingTimeMs = 120)
        {
            return new AiPredictTask
            {
                TaskId = taskId,
                Status = status,
                PredictedClass = predictedClass,
                Confidence = confidence,
                AllProbabilities = allProbabilities ?? new Dictionary<string, double>
                {
                    { "NORMAL", 0.05 },
                    { "DME", 0.95 }
                },
                ModelVersion = modelVersion,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddSeconds(-2),
                CompletedAt = completedAt ?? DateTime.UtcNow,
                ProcessingTimeMs = processingTimeMs
            };
        }

        public static AiServiceOptions GetAiOptions(
            int maxPollAttempts = 3,
            int pollIntervalSeconds = 0) // Set to 0 to make unit tests run instantly
        {
            return new AiServiceOptions
            {
                MaxPollAttempts = maxPollAttempts,
                PollIntervalSeconds = pollIntervalSeconds
            };
        }
    }
}