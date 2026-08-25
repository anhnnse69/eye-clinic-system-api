using ECS.Application.Common.Response;
using ECS.Domain.Enums;
using ECS.Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace ECS.Application.Services.AiTriageServices
{
    /// <summary>
    /// AI Triage Service — Symptom-based eye disease prediction
    /// 
    /// Flow:
    /// 1. Validate request
    /// 2. Call AI Service /predict-symptoms endpoint
    /// 3. Transform response to UI-friendly format
    /// 4. Return structured triage result
    /// </summary>
    public interface IAiTriageService
    {
        Task<ApiResponse<AiTriageResponse>> ProcessAsync(AiTriageRequest request, CancellationToken ct = default);
    }

    public class AiTriageService : IAiTriageService
    {
        private readonly IAiServiceClient _aiClient;
        private readonly ILogger<AiTriageService> _logger;

        public AiTriageService(
            IAiServiceClient aiClient,
            ILogger<AiTriageService> logger)
        {
            _aiClient = aiClient;
            _logger = logger;
        }

        public async Task<ApiResponse<AiTriageResponse>> ProcessAsync(
            AiTriageRequest request, 
            CancellationToken ct = default)
        {
            try
            {
                // Map request to AI format
                var aiRequest = MapToAiRequest(request);

                // Call AI Service
                var aiResponse = await _aiClient.PredictSymptomsAsync(aiRequest, ct);

                // Transform to UI response
                var response = MapToResponse(aiResponse);

                return ApiResponse<AiTriageResponse>.Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(), 
                    response);
            }
            catch (AiServiceException ex)
            {
                _logger.LogError(ex, "AI Triage service failed");
                
                var errorResponse = new AiTriageResponse
                {
                    TaskId = Guid.NewGuid().ToString(),
                    Status = "FAILED",
                    IsSuccess = false,
                    ErrorCode = "AI_SERVICE_ERROR",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
                
                return ApiResponse<AiTriageResponse>.Success(
                    GeneralCode.APP_MESSAGE_5001.ToString(), 
                    errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Triage unexpected error");
                
                return ApiResponse<AiTriageResponse>.Fail(
                    GeneralCode.APP_MESSAGE_5001.ToString());
            }
        }

        private static AiSymptomPredictRequest MapToAiRequest(AiTriageRequest request)
        {
            return new AiSymptomPredictRequest
            {
                Symptom = request.Symptom,
                Duration = request.Duration,
                PainLevel = request.PainLevel,
                EyeRedness = request.EyeRedness,
                BlurredVision = request.BlurredVision,
                LightSensitivity = request.LightSensitivity,
                Discharge = request.Discharge,
                Tearing = request.Tearing,
                Swelling = request.Swelling,
                ForeignBodySensation = request.ForeignBodySensation,
                Floaters = request.Floaters,
                Halos = request.Halos,
                EyePressure = request.EyePressure,
                CornealOpacity = request.CornealOpacity,
                PupilResponse = request.PupilResponse,
                NightBlindness = request.NightBlindness,
                DoubleVision = request.DoubleVision,
                EyeTurning = request.EyeTurning,
                WhiteReflection = request.WhiteReflection,
                Headache = request.Headache,
                Nausea = request.Nausea,
                Age = request.Age,
                Diabetes = request.Diabetes,
                Hypertension = request.Hypertension,
                FamilyHistory = request.FamilyHistory
            };
        }

        private static AiTriageResponse MapToResponse(AiSymptomPredictResponse aiResponse)
        {
            // Extract top 3 differential diagnoses (excluding primary)
            var differentials = new List<DifferentialDiagnosis>();
            
            if (aiResponse.AllProbabilities != null && aiResponse.AllProbabilities.Count > 0)
            {
                var sorted = aiResponse.AllProbabilities
                    .OrderByDescending(kv => kv.Value)
                    .Skip(1) // Skip primary
                    .Take(3);
                
                foreach (var kv in sorted)
                {
                    differentials.Add(new DifferentialDiagnosis
                    {
                        Disease = kv.Key,
                        Confidence = kv.Value
                    });
                }
            }

            return new AiTriageResponse
            {
                TaskId = aiResponse.TaskId ?? Guid.NewGuid().ToString(),
                Status = aiResponse.Status ?? "COMPLETED",
                PredictedDisease = aiResponse.PredictedDisease,
                Confidence = aiResponse.Confidence,
                RiskLevel = aiResponse.RiskLevel,
                Differentials = differentials,
                AllProbabilities = aiResponse.AllProbabilities,
                Disclaimer = aiResponse.Disclaimer ?? "Preliminary AI assessment based on symptoms only. Does NOT replace professional eye care consultation.",
                ErrorCode = aiResponse.ErrorCode,
                ErrorMessage = aiResponse.ErrorMessage,
                CreatedAt = ParseDateTime(aiResponse.CreatedAt),
                CompletedAt = ParseDateTime(aiResponse.CompletedAt),
                IsSuccess = string.IsNullOrEmpty(aiResponse.ErrorCode) 
                    && (aiResponse.Status?.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) ?? true)
            };
        }

        private static DateTime? ParseDateTime(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return null;
            if (DateTime.TryParse(dateStr, out var dt)) return dt;
            return null;
        }
    }
}
