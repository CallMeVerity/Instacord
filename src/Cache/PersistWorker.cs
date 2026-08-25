using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Instacord.Cache;

public class PersistWorker : IHostedService
{
    private readonly Channel<PersistRequest> _channel;
    private readonly PostPersistJob _job;
    private readonly CacheOptions _options;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly List<Task> _runners = new();
    private CancellationTokenSource _cts = new();

    public PersistWorker(PostPersistJob job, IOptions<CacheOptions> options, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _job = job;
        _options = options.Value;
        _delay = delay ?? Task.Delay;
        _channel = Channel.CreateBounded<PersistRequest>(new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public virtual void Enqueue(PersistRequest request)
    {
        _channel.Writer.TryWrite(request);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        for (var i = 0; i < Math.Max(1, _options.PersistConcurrency); i++)
            _runners.Add(Task.Run(() => RunLoopAsync(_cts.Token)));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();
        try
        {
            await Task.WhenAll(_runners);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        await foreach (var request in _channel.Reader.ReadAllAsync(ct))
        {
            for (var attempt = 1; attempt <= _options.PersistMaxAttempts; attempt++)
            {
                var ok = await _job.RunAsync(request, ct);
                if (ok || attempt == _options.PersistMaxAttempts)
                    break;
                try
                {
                    await _delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}