using System.Threading.Channels;

namespace ECS.Infrastructure.Ai;

public record AiQueueItem(Func<CancellationToken, Task> ExecuteAsync, CancellationToken CancellationToken);

/// <summary>
/// Channel-based thread-safe Queue implementation for AI task execution.
/// </summary>
public class AiTaskQueue : IAiTaskQueue
{
    private readonly Channel<AiQueueItem> _channel;

    public AiTaskQueue()
    {
        _channel = Channel.CreateUnbounded<AiQueueItem>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public Task<T> EnqueueAsync<T>(Func<CancellationToken, Task<T>> aiWork, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var item = new AiQueueItem(
            async cancellationToken =>
            {
                try
                {
                    var result = await aiWork(cancellationToken);
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            },
            ct
        );

        _channel.Writer.TryWrite(item);
        return tcs.Task;
    }

    public ChannelReader<AiQueueItem> Reader => _channel.Reader;
}
