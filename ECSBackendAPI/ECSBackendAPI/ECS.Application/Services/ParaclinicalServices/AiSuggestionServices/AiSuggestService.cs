using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Enums;
using ECS.Infrastructure.Ai;
using ECS.Infrastructure.Persistence.MongoDb;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace ECS.Application.Services.ParaclinicalServices.AiSuggestionServices
{
    /// <summary>
    /// Orchestrates an AI suggestion request for an OCT image:
    ///   1. submit the image to the FastAPI inference service,
    ///   2. poll the task until it reaches a terminal status (<c>completed</c>/<c>failed</c>),
    ///   3. persist the result in MongoDB (<c>ai_suggestions</c>) so doctors can re-fetch.
    /// </summary>
    /// <remarks>
    /// Used by UC42 (OCT AI suggestion). The pipeline is broken into small focused steps
    /// (auth -> submit -> poll -> persist -> build) so each stage is independently testable
    /// and follows the same ExecutionState pattern as the other Paraclinical services.
    /// </remarks>
    public class AiSuggestService : IAiSuggestService
    {
        private readonly IAiServiceClient _ai;
        private readonly IMongoDbContext _mongo;
        private readonly AiServiceOptions _opt;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AiSuggestService> _logger;

        public AiSuggestService(
            IAiServiceClient ai,
            IMongoDbContext mongo,
            IOptions<AiServiceOptions> opt,
            IHttpContextAccessor http,
            ILogger<AiSuggestService> logger)
        {
            _ai = ai;
            _mongo = mongo;
            _opt = opt.Value;
            _http = http;
            _logger = logger;
        }

        /// <summary>
        /// Entry point. Runs each pipeline step in order, short-circuiting on first error.
        /// </summary>
        public async Task<ApiResponse<AiSuggestResponse>> Process(AiSuggestRequest request)
        {
            var state = new ExecutionState();

            Authenticate(state);
            if (!state.HasError) await SubmitToAiAsync(request, state);
            if (!state.HasError) await PollUntilTerminalAsync(state);
            if (!state.HasError) await PersistSuggestionAsync(request, state);
            return BuildResponse(state);
        }

        /// <summary>
        /// Mutable state passed between pipeline steps so each helper has a single
        /// responsibility and the orchestrator stays linear and readable.
        /// </summary>
        private class ExecutionState
        {
            public bool HasError { get; set; }
            public string? ErrorCode { get; set; }
            public AiPredictTask? Task { get; set; }
            public AiSuggestionDocument? Document { get; set; }
        }

        /// <summary>
        /// Validates the JWT principal and captures the caller's user id
        /// (currently only used to assert the caller is authenticated).
        /// </summary>
        private void Authenticate(ExecutionState state)
        {
            var principalId = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(principalId, out _))
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
            }
        }

        /// <summary>
        /// Submits the OCT image bytes to the AI service and stores the initial task descriptor.
        /// </summary>
        private async Task SubmitToAiAsync(AiSuggestRequest request, ExecutionState state)
        {
            try
            {
                state.Task = await _ai.SubmitPredictionAsync(request.ImageBytes, request.MimeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI submit failed");
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        /// <summary>
        /// Polls the AI service at <see cref="AiServiceOptions.PollIntervalSeconds"/> until the
        /// task reports a terminal status or <see cref="AiServiceOptions.MaxPollAttempts"/> is reached.
        /// Transient poll failures are logged but do not abort the pipeline.
        /// </summary>
        private async Task PollUntilTerminalAsync(ExecutionState state)
        {
            var task = state.Task;
            if (task == null) return;

            var attempts = 0;
            while (!IsTerminal(task.Status) && attempts < _opt.MaxPollAttempts)
            {
                attempts++;
                await Task.Delay(TimeSpan.FromSeconds(_opt.PollIntervalSeconds));
                try
                {
                    task = await _ai.GetTaskAsync(task.TaskId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI poll {Attempt}/{Max} failed", attempts, _opt.MaxPollAttempts);
                }
            }
            state.Task = task;
        }

        /// <summary>
        /// Persists the AI suggestion document in MongoDB. Persistence failures are logged
        /// but non-fatal — the suggestion is still returned in the response.
        /// </summary>
        private async Task PersistSuggestionAsync(AiSuggestRequest request, ExecutionState state)
        {
            var task = state.Task;
            if (task == null) return;

            var doc = new AiSuggestionDocument
            {
                RecordId = request.RecordId,
                LabResultId = request.LabResultId,
                ModelName = "VGG16_BN_OCT_Classifier",
                ModelVersion = task.ModelVersion ?? "unknown",
                PredictedClass = task.PredictedClass,
                Confidence = task.Confidence,
                AllProbabilities = BuildProbabilitiesBson(task.AllProbabilities),
                Status = task.Status,
                ErrorCode = task.ErrorCode,
                ErrorMessage = task.ErrorMessage,
                CreatedAt = task.CreatedAt ?? DateTime.UtcNow,
                CompletedAt = task.CompletedAt,
                ProcessingTimeMs = task.ProcessingTimeMs
            };

            try
            {
                await _mongo.AiSuggestions.InsertOneAsync(doc);
                state.Document = doc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI suggestion persistence failed (task {TaskId})", task.TaskId);
            }
        }

        /// <summary>
        /// Maps the execution state into the final API response, or returns the captured error code.
        /// </summary>
        private ApiResponse<AiSuggestResponse> BuildResponse(ExecutionState state)
        {
            if (state.HasError || state.Task == null || state.Document == null)
            {
                return ApiResponse<AiSuggestResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }

            var task = state.Task;
            var doc = state.Document;
            var resp = new AiSuggestResponse
            {
                SuggestionId = doc.Id,
                TaskId = task.TaskId,
                Status = task.Status,
                PredictedClass = task.PredictedClass,
                Confidence = task.Confidence,
                AllProbabilities = task.AllProbabilities,
                ModelVersion = task.ModelVersion,
                ErrorCode = task.ErrorCode,
                ErrorMessage = task.ErrorMessage,
                CreatedAt = doc.CreatedAt,
                CompletedAt = doc.CompletedAt,
                ProcessingTimeMs = task.ProcessingTimeMs,
                IsSuccess = task.Status == "completed"
            };
            return ApiResponse<AiSuggestResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(), resp);
        }

        /// <summary>
        /// Returns <c>true</c> if the AI task has reached a final state and polling should stop.
        /// </summary>
        private static bool IsTerminal(string status) =>
            status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("failed", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Converts the AI service's string -> double probability map into a Mongo BsonDocument.
        /// Returns an empty document when input is null.
        /// </summary>
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