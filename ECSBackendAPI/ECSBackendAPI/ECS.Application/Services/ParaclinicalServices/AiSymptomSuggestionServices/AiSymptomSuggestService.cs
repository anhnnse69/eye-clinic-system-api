using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Enums;
using ECS.Infrastructure.Ai;
using ECS.Infrastructure.Persistence.MongoDb;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices
{
    /// <summary>
    /// Orchestrates a preliminary symptom-based AI suggestion request:
    ///   1. submit patient symptom features to the FastAPI symptom inference endpoint,
    ///   2. persist the prediction result in MongoDB (<c>ai_suggestions</c>) for audit/retrieval,
    ///   3. return structured preliminary disease diagnosis response.
    /// </summary>
    public class AiSymptomSuggestService : IAiSymptomSuggestService
    {
        private readonly IAiServiceClient _ai;
        private readonly IMongoDbContext _mongo;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AiSymptomSuggestService> _logger;

        public AiSymptomSuggestService(
            IAiServiceClient ai,
            IMongoDbContext mongo,
            IHttpContextAccessor http,
            ILogger<AiSymptomSuggestService> logger)
        {
            _ai = ai;
            _mongo = mongo;
            _http = http;
            _logger = logger;
        }

        public async Task<ApiResponse<AiSymptomSuggestResponse>> Process(AiSymptomSuggestRequest request)
        {
            var state = new ExecutionState();

            Authenticate(state);
            if (!state.HasError) await PredictSymptomsAsync(request, state);
            if (!state.HasError) await PersistSuggestionAsync(request, state);
            return BuildResponse(state);
        }

        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }
            public AiSymptomPredictResponse? Task { get; set; }
            public AiSuggestionDocument? Document { get; set; }
        }

        private void Authenticate(ExecutionState state)
        {
            var principalId = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(principalId, out _))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
            }
        }

        private async Task PredictSymptomsAsync(AiSymptomSuggestRequest request, ExecutionState state)
        {
            try
            {
                var aiRequest = new AiSymptomPredictRequest
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

                state.Task = await _ai.PredictSymptomsAsync(aiRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI symptom predict failed");
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        private async Task PersistSuggestionAsync(AiSymptomSuggestRequest request, ExecutionState state)
        {
            var task = state.Task;
            if (task == null) return;

            DateTime createdAtParsed = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(task.CreatedAt) && DateTime.TryParse(task.CreatedAt, out var dtCreated))
            {
                createdAtParsed = dtCreated.ToUniversalTime();
            }

            DateTime? completedAtParsed = null;
            if (!string.IsNullOrEmpty(task.CompletedAt) && DateTime.TryParse(task.CompletedAt, out var dtCompleted))
            {
                completedAtParsed = dtCompleted.ToUniversalTime();
            }

            var doc = new AiSuggestionDocument
            {
                RecordId = request.RecordId,
                ModelName = "Symptom_Eye_Disease_19class",
                ModelVersion = "1.0.0",
                PredictedClass = task.PredictedDisease,
                Confidence = task.Confidence,
                AllProbabilities = BuildProbabilitiesBson(task.AllProbabilities),
                Status = task.Status,
                ErrorCode = task.ErrorCode,
                ErrorMessage = task.ErrorMessage,
                CreatedAt = createdAtParsed,
                CompletedAt = completedAtParsed
            };

            try
            {
                await _mongo.AiSuggestions.InsertOneAsync(doc);
                state.Document = doc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI symptom suggestion persistence failed (task {TaskId})", task.TaskId);
            }
        }

        private ApiResponse<AiSymptomSuggestResponse> BuildResponse(ExecutionState state)
        {
            if (state.HasError || state.Task == null || state.Document == null)
            {
                return ApiResponse<AiSymptomSuggestResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var task = state.Task;
            var doc = state.Document;

            DateTime? createdAtParsed = doc.CreatedAt;
            DateTime? completedAtParsed = doc.CompletedAt;

            var resp = new AiSymptomSuggestResponse
            {
                SuggestionId = doc.Id,
                TaskId = task.TaskId,
                Status = task.Status,
                PredictedDisease = task.PredictedDisease,
                Confidence = task.Confidence,
                AllProbabilities = task.AllProbabilities,
                RiskLevel = task.RiskLevel,
                Disclaimer = task.Disclaimer,
                ErrorCode = task.ErrorCode,
                ErrorMessage = task.ErrorMessage,
                CreatedAt = createdAtParsed,
                CompletedAt = completedAtParsed,
                IsSuccess = task.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(task.ErrorCode)
            };

            return ApiResponse<AiSymptomSuggestResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), resp);
        }

        private static BsonDocument BuildProbabilitiesBson(Dictionary<string, double>? probs)
        {
            var bson = new BsonDocument();
            if (probs == null) return bson;
            foreach (var kv in probs)
                bson[kv.Key] = kv.Value;
            return bson;
        }
    }
}
