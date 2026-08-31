namespace ECS.Infrastructure.Ai;

/// <summary>
/// Thread-safe in-memory task queue interface for rate-limiting AI predictions.
/// Ensures AI requests are processed cleanly via background workers without overloading the AI service.
/// </summary>
public interface IAiTaskQueue
{
    Task<T> EnqueueAsync<T>(Func<CancellationToken, Task<T>> aiWork, CancellationToken ct = default);
}
