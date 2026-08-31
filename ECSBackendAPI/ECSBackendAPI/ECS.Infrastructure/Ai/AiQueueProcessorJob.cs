using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECS.Infrastructure.Ai;

/// <summary>
/// Background Service worker consuming AI tasks from the queue with controlled concurrency.
/// Prevents concurrent thread overload on the FastAPI AI inference service.
/// </summary>
public class AiQueueProcessorJob : BackgroundService
{
    private readonly AiTaskQueue _queue;
    private readonly IOptionsMonitor<AiServiceOptions> _optionsMonitor;
    private readonly ILogger<AiQueueProcessorJob> _logger;

    public AiQueueProcessorJob(
        AiTaskQueue queue,
        IOptionsMonitor<AiServiceOptions> optionsMonitor,
        ILogger<AiQueueProcessorJob> logger)
    {
        _queue = queue;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxConcurrent = Math.Max(1, _optionsMonitor.CurrentValue.MaxConcurrentTasks);
        using var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

        _logger.LogInformation("AI Queue Processor Background Worker initialized. Max Concurrent AI Threads: {MaxConcurrent}", maxConcurrent);

        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await semaphore.WaitAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, item.CancellationToken);
                    await item.ExecuteAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in background AI queue task execution");
                }
                finally
                {
                    semaphore.Release();
                }
            }, stoppingToken);
        }
    }
}
