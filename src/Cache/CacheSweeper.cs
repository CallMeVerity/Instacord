using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Instacord.Cache;

public sealed class CacheSweeper : IHostedService, IDisposable
{
    private readonly IObjectStore _store;
    private readonly CacheOptions _options;
    private Timer? _timer;

    public CacheSweeper(IObjectStore store, IOptions<CacheOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(_ => _ = SweepAsync(CancellationToken.None), null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    internal async Task<int> SweepAsync(CancellationToken ct)
    {
        var keys = await _store.ListAsync(CacheKeys.Prefix, ct);
        var codes = keys
            .Where(k => k.EndsWith("/meta.json", StringComparison.Ordinal))
            .Select(k => k.Substring(CacheKeys.Prefix.Length, k.IndexOf("/meta.json", StringComparison.Ordinal) - CacheKeys.Prefix.Length))
            .Distinct()
            .ToList();

        var evicted = 0;
        foreach (var code in codes)
        {
            var stream = await _store.GetAsync(CacheKeys.MetaKey(code), ct);
            if (stream is null)
                continue;
            
            using var reader = new StreamReader(stream);
            var envelope = CachedPostEnvelope.Deserialize(await reader.ReadToEndAsync(ct));
            if (envelope is null)
                continue;
            
            if (DateTimeOffset.UtcNow - envelope.CachedAt <= TimeSpan.FromDays(_options.TtlDays)) continue;
            var prefix = CacheKeys.CodePrefix(code);
            foreach (var key in await _store.ListAsync(prefix, ct))
                await _store.DeleteAsync(key, ct);
            evicted++;
        }
        return evicted;
    }

    public void Dispose() => _timer?.Dispose();
}