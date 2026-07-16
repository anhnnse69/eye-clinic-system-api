using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECS.Infrastructure.Ai;

/// <summary>
/// Typed HTTP client for the eye-clinic-system-ai FastAPI service
/// (<c>POST /api/v1/ai/predict</c>, <c>GET /api/v1/ai/predict/{task_id}</c>).
/// </summary>
public interface IAiServiceClient
{
    Task<AiPredictTask> SubmitPredictionAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default);
    Task<AiPredictTask> GetTaskAsync(string taskId, CancellationToken ct = default);
}

public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _http;
    private readonly AiServiceOptions _options;
    private readonly ILogger<AiServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AiServiceClient(
        HttpClient http,
        IOptions<AiServiceOptions> options,
        ILogger<AiServiceClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(_options.PredictTimeoutSeconds);
    }

    public async Task<AiPredictTask> SubmitPredictionAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var body = new AiPredictRequest { Image = base64 };

        using var req = new HttpRequestMessage(HttpMethod.Post, _options.ApiPrefix.TrimStart('/') + "/predict");
        req.Content = JsonContent.Create(body);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("AI predict submission failed: {Status} {Body}", resp.StatusCode, raw);
            throw new AiServiceException($"AI predict submission failed: {(int)resp.StatusCode} {resp.StatusCode}");
        }

        var envelope = JsonSerializer.Deserialize<AiApiResponse<AiPredictTask>>(raw, JsonOpts)
            ?? throw new AiServiceException("AI predict returned an empty body.");
        return envelope.Data ?? throw new AiServiceException("AI predict response missing data payload.");
    }

    public async Task<AiPredictTask> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(
            _options.ApiPrefix.TrimStart('/') + "/predict/" + Uri.EscapeDataString(taskId), ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("AI poll failed for {TaskId}: {Status} {Body}", taskId, resp.StatusCode, raw);
            throw new AiServiceException($"AI poll failed: {(int)resp.StatusCode} {resp.StatusCode}");
        }
        var envelope = JsonSerializer.Deserialize<AiApiResponse<AiPredictTask>>(raw, JsonOpts)
            ?? throw new AiServiceException("AI poll returned an empty body.");
        return envelope.Data ?? throw new AiServiceException("AI poll response missing data payload.");
    }
}

public class AiServiceException : Exception
{
    public AiServiceException(string message) : base(message) { }
    public AiServiceException(string message, Exception inner) : base(message, inner) { }
}

// ─── DTOs matching the FastAPI envelope ────────────────────────────────────

public class AiPredictRequest
{
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;
}

public class AiApiResponse<T>
{
    [JsonPropertyName("codeMessage")]
    public string CodeMessage { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("meta")]
    public object? Meta { get; set; }
}

public class AiPredictTask
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("predicted_class")]
    public string? PredictedClass { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    [JsonPropertyName("all_probabilities")]
    public Dictionary<string, double>? AllProbabilities { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("model_version")]
    public string? ModelVersion { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("processing_time_ms")]
    public int? ProcessingTimeMs { get; set; }
}