namespace ECS.Infrastructure.Ai;

/// <summary>
/// Strongly-typed binding for the <c>AiService</c> section in <c>appsettings.json</c>.
/// </summary>
public class AiServiceOptions
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = "https://eye-clinic-system-ai.onrender.com";
    public string ApiPrefix { get; set; } = "/api/v1/ai";
    public int PredictTimeoutSeconds { get; set; } = 60;
    public int PollIntervalSeconds { get; set; } = 1;
    public int MaxPollAttempts { get; set; } = 60;
    public int MaxConcurrentTasks { get; set; } = 2;
}