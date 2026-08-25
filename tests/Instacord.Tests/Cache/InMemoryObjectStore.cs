using System.Collections.Concurrent;
using Instacord.Cache;

namespace Instacord.Tests.Cache;

public sealed class InMemoryObjectStore : IObjectStore
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new();

    public Task<Stream?> GetAsync(string key, CancellationToken ct = default)
    {
        if (_objects.TryGetValue(key, out var bytes))
            return Task.FromResult<Stream?>(new MemoryStream(bytes));
        return Task.FromResult<Stream?>(null);
    }

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        _objects[key] = ms.ToArray();
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default)
    {
        var keys = _objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(k => k).ToList();
        return Task.FromResult<IReadOnlyList<string>>(keys);
    }
}