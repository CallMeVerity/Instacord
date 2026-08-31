using System.Collections.Concurrent;
using Instacord.Models;
using Instacord.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Instacord.Cache;

public sealed class PostCache : IInstagramService
{
    private readonly IPostFetcher _fetcher;
    private readonly IObjectStore _store;
    private readonly MemoryCache _memory;
    private readonly PersistWorker _worker;
    private readonly CacheOptions _options;
    private readonly ILogger<PostCache> _logger;
    private readonly ConcurrentDictionary<string, Task<InstagramPost?>> _inFlight = new();

    public PostCache(
        IPostFetcher fetcher,
        IObjectStore store,
        MemoryCache memory,
        PersistWorker worker,
        IOptions<CacheOptions> options,
        ILogger<PostCache> logger)
    {
        _fetcher = fetcher;
        _store = store;
        _memory = memory;
        _worker = worker;
        _options = options.Value;
        _logger = logger;
    }

    public Task<InstagramPost?> GetPostAsync(string code, CancellationToken ct = default, bool isShare = false) =>
        GetOrFetchAsync(code, isShare, ct);

    private async Task<InstagramPost?> GetOrFetchAsync(string code, bool isShare, CancellationToken ct)
    {
        var realCode = await _fetcher.ResolveCodeAsync(code, isShare, ct);
        if (realCode is null)
            return null;

        if (_memory.TryGet(realCode, out var cached))
            return cached!;

        var stored = await TryReadStoredAsync(realCode, ct);
        if (stored is null) 
            return await FetchAndRespondAsync(realCode, ct);
        
        if (IsExpired(stored))
        {
            _memory.Remove(realCode);
        }
        else
        {
            _memory.Put(realCode, stored.Post);
            if (IsStale(stored))
                _worker.Enqueue(new PersistRequest(realCode, FreshPost: null, IsRefresh: true, OnEvict: c => _memory.Remove(c)));
            return stored.Post;
        }

        return await FetchAndRespondAsync(realCode, ct);
    }

    private async Task<CachedPostEnvelope?> TryReadStoredAsync(string code, CancellationToken ct)
    {
        try
        {
            var stream = await _store.GetAsync(CacheKeys.MetaKey(code), ct);
            if (stream is null)
                return null;
            using var reader = new StreamReader(stream);
            return CachedPostEnvelope.Deserialize(await reader.ReadToEndAsync(ct));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed reading stored post {Code}", code);
            return null;
        }
    }

    private async Task<InstagramPost?> FetchAndRespondAsync(string code, CancellationToken ct)
    {
        var task = _inFlight.GetOrAdd(code, _ => FetchOnceAsync(code, ct));
        try
        {
            return await task;
        }
        finally
        {
            _inFlight.TryRemove(code, out _);
        }
    }

    private async Task<InstagramPost?> FetchOnceAsync(string code, CancellationToken ct)
    {
        var post = await _fetcher.FetchPostAsync(code, ct);
        if (post is null)
            return null;

        _worker.Enqueue(new PersistRequest(code, post, IsRefresh: false, OnEvict: null));
        return post;
    }

    private bool IsStale(CachedPostEnvelope envelope) =>
        DateTimeOffset.UtcNow - envelope.CachedAt > TimeSpan.FromHours(_options.RefreshAgeHours);

    private bool IsExpired(CachedPostEnvelope envelope) =>
        DateTimeOffset.UtcNow - envelope.CachedAt > TimeSpan.FromDays(_options.TtlDays);
}