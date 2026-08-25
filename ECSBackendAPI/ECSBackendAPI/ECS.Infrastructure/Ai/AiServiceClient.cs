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
    Task<AiSymptomPredictResponse> PredictSymptomsAsync(AiSymptomPredictRequest request, CancellationToken ct = default);
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

    public async Task<AiSymptomPredictResponse> PredictSymptomsAsync(AiSymptomPredictRequest request, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, _options.ApiPrefix.TrimStart('/') + "/predict-symptoms");
        req.Content = JsonContent.Create(request, options: JsonOpts);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("AI predict-symptoms failed: {Status} {Body}", resp.StatusCode, raw);
            throw new AiServiceException($"AI predict-symptoms failed: {(int)resp.StatusCode} {resp.StatusCode}");
        }

        var envelope = JsonSerializer.Deserialize<AiApiResponse<AiSymptomPredictResponse>>(raw, JsonOpts)
            ?? throw new AiServiceException("AI predict-symptoms returned an empty body.");
        return envelope.Data ?? throw new AiServiceException("AI predict-symptoms response missing data payload.");
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

public class AiSymptomPredictRequest
{
    [JsonPropertyName("symptom")]
    public string Symptom { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = "acute";

    [JsonPropertyName("pain_level")]
    public string PainLevel { get; set; } = "low";

    [JsonPropertyName("eye_redness")]
    public string EyeRedness { get; set; } = "no";

    [JsonPropertyName("blurred_vision")]
    public string BlurredVision { get; set; } = "no";

    [JsonPropertyName("light_sensitivity")]
    public string LightSensitivity { get; set; } = "no";

    [JsonPropertyName("discharge")]
    public string Discharge { get; set; } = "no";

    [JsonPropertyName("tearing")]
    public string Tearing { get; set; } = "no";

    [JsonPropertyName("swelling")]
    public string Swelling { get; set; } = "no";

    [JsonPropertyName("foreign_body_sensation")]
    public string ForeignBodySensation { get; set; } = "no";

    [JsonPropertyName("floaters")]
    public string Floaters { get; set; } = "no";

    [JsonPropertyName("halos")]
    public string Halos { get; set; } = "no";

    [JsonPropertyName("eye_pressure")]
    public string EyePressure { get; set; } = "normal";

    [JsonPropertyName("corneal_opacity")]
    public string CornealOpacity { get; set; } = "no";

    [JsonPropertyName("pupil_response")]
    public string PupilResponse { get; set; } = "normal";

    [JsonPropertyName("night_blindness")]
    public string NightBlindness { get; set; } = "no";

    [JsonPropertyName("double_vision")]
    public string DoubleVision { get; set; } = "no";

    [JsonPropertyName("eye_turning")]
    public string EyeTurning { get; set; } = "no";

    [JsonPropertyName("white_reflection")]
    public string WhiteReflection { get; set; } = "no";

    [JsonPropertyName("headache")]
    public string Headache { get; set; } = "no";

    [JsonPropertyName("nausea")]
    public string Nausea { get; set; } = "no";

    [JsonPropertyName("age")]
    public string Age { get; set; } = "adult";

    [JsonPropertyName("diabetes")]
    public string Diabetes { get; set; } = "no";

    [JsonPropertyName("hypertension")]
    public string Hypertension { get; set; } = "no";

    [JsonPropertyName("family_history")]
    public string FamilyHistory { get; set; } = "no";
}

public class AiSymptomPredictResponse
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("predicted_disease")]
    public string? PredictedDisease { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    [JsonPropertyName("all_probabilities")]
    public Dictionary<string, double>? AllProbabilities { get; set; }

    [JsonPropertyName("risk_level")]
    public string? RiskLevel { get; set; }

    [JsonPropertyName("disclaimer")]
    public string? Disclaimer { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }
}