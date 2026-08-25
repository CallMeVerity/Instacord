using System.Text.Json;
using Instacord.Cache;
using Instacord.Models;
using Microsoft.Extensions.Options;
namespace Instacord.Tests.Cache;

public class CacheSweeperTests
{
    private static CacheOptions Opts(int ttlDays = 7) => new()
    {
        Endpoint = "https://x", Bucket = "b", PublicBaseUrl = "https://x/b",
        AccessKey = "a", SecretKey = "s", TtlDays = ttlDays
    };

    private static async Task PutEnvelope(InMemoryObjectStore store, string code, DateTimeOffset cachedAt)
    {
        var envelope = new CachedPostEnvelope { CachedAt = cachedAt, Post = new() { Code = code, Username = "u", Items = Array.Empty<MediaItem>() } };
        using var ms = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(envelope));
        await store.PutAsync(CacheKeys.MetaKey(code), ms, "application/json");
        await store.PutAsync(CacheKeys.MediaKey(code, 1, "jpg"), new MemoryStream("img"u8.ToArray()), "image/jpeg");
    }

    [Fact]
    public async Task Sweeps_expired_prefixes_and_leaves_fresh()
    {
        var store = new InMemoryObjectStore();
        await PutEnvelope(store, "OLD", DateTimeOffset.UtcNow.AddDays(-30));
        await PutEnvelope(store, "NEW", DateTimeOffset.UtcNow);
        var sweeper = new CacheSweeper(store, Options.Create(Opts()));

        var evicted = await sweeper.SweepAsync(default);

        Assert.Equal(1, evicted);
        Assert.Null(await store.GetAsync(CacheKeys.MetaKey("OLD")));
        Assert.Null(await store.GetAsync(CacheKeys.MediaKey("OLD", 1, "jpg")));
        Assert.NotNull(await store.GetAsync(CacheKeys.MetaKey("NEW")));
    }

    [Fact]
    public async Task Skips_keys_without_meta_json()
    {
        var store = new InMemoryObjectStore();
        await store.PutAsync("posts/STRAY/1.jpg", new MemoryStream("x"u8.ToArray()), "image/jpeg");
        var sweeper = new CacheSweeper(store, Options.Create(Opts()));

        var evicted = await sweeper.SweepAsync(default);

        Assert.Equal(0, evicted);
        Assert.NotNull(await store.GetAsync("posts/STRAY/1.jpg"));
    }
}