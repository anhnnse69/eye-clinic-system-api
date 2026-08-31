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
    Task<AiSymptomPredictResponse> PredictSymptomsAsync(AiSymptomPredictRequest request, CancellationToken ct = default);
    Task<AiOctPredictResponse> PredictOctImageAsync(AiOctPredictRequest request, CancellationToken ct = default);
}

public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _http;
    private readonly IOptionsMonitor<AiServiceOptions> _optionsMonitor;
    private readonly IAiTaskQueue _taskQueue;
    private readonly ILogger<AiServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AiServiceClient(
        HttpClient http,
        IOptionsMonitor<AiServiceOptions> optionsMonitor,
        IAiTaskQueue taskQueue,
        ILogger<AiServiceClient> logger)
    {
        _http = http;
        _optionsMonitor = optionsMonitor;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    private Uri GetEndpointUri(string relativePath)
    {
        var options = _optionsMonitor.CurrentValue;
        var baseUrl = (options.BaseUrl ?? "http://localhost:8000").TrimEnd('/');
        var prefix = (options.ApiPrefix ?? "/api/v1/ai").Trim('/');
        return new Uri($"{baseUrl}/{prefix}/{relativePath.TrimStart('/')}");
    }

    public Task<AiSymptomPredictResponse> PredictSymptomsAsync(AiSymptomPredictRequest request, CancellationToken ct = default)
    {
        return _taskQueue.EnqueueAsync(cancellationToken => DirectPredictSymptomsAsync(request, cancellationToken), ct);
    }

    public Task<AiOctPredictResponse> PredictOctImageAsync(AiOctPredictRequest request, CancellationToken ct = default)
    {
        return _taskQueue.EnqueueAsync(cancellationToken => DirectPredictOctImageAsync(request, cancellationToken), ct);
    }

    private async Task<AiSymptomPredictResponse> DirectPredictSymptomsAsync(AiSymptomPredictRequest request, CancellationToken ct)
    {
        var endpointUri = GetEndpointUri("predict-symptoms");
        using var req = new HttpRequestMessage(HttpMethod.Post, endpointUri);
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

    private async Task<AiOctPredictResponse> DirectPredictOctImageAsync(AiOctPredictRequest request, CancellationToken ct)
    {
        var endpointUri = GetEndpointUri("predict-oct");
        using var req = new HttpRequestMessage(HttpMethod.Post, endpointUri);
        req.Content = JsonContent.Create(request, options: JsonOpts);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("AI predict-oct failed: {Status} {Body}", resp.StatusCode, raw);
            throw new AiServiceException($"AI predict-oct failed: {(int)resp.StatusCode} {resp.StatusCode}");
        }

        var envelope = JsonSerializer.Deserialize<AiApiResponse<AiOctPredictResponse>>(raw, JsonOpts)
            ?? throw new AiServiceException("AI predict-oct returned an empty body.");
        return envelope.Data ?? throw new AiServiceException("AI predict-oct response missing data payload.");
    }
}

public class AiServiceException : Exception
{
    public AiServiceException(string message) : base(message) { }
    public AiServiceException(string message, Exception inner) : base(message, inner) { }
}

// ─── DTOs matching the FastAPI envelope ────────────────────────────────────

public class AiApiResponse<T>
{
    [JsonPropertyName("codeMessage")]
    public string CodeMessage { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("meta")]
    public object? Meta { get; set; }
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

public class AiOctPredictRequest
{
    [JsonPropertyName("image_base64")]
    public string ImageBase64 { get; set; } = string.Empty;
}

public class AiOctPredictResponse
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("predicted_class")]
    public string PredictedClass { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("probabilities")]
    public Dictionary<string, double>? Probabilities { get; set; }

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = string.Empty;

    [JsonPropertyName("is_low_confidence")]
    public bool IsLowConfidence { get; set; }

    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; } = string.Empty;

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }
}