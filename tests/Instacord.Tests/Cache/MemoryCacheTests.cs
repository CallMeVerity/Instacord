using Instacord.Cache;
using Instacord.Models;
namespace Instacord.Tests.Cache;

public class MemoryCacheTests
{
    private static InstagramPost Post(string code) => new()
    {
        Code = code,
        Username = "u",
        Items = Array.Empty<MediaItem>()
    };

    [Fact]
    public void Put_then_TryGet_returns_post()
    {
        var cache = new MemoryCache(2);
        cache.Put("A", Post("A"));
        Assert.True(cache.TryGet("A", out var got));
        Assert.Equal("A", got!.Code);
    }

    [Fact]
    public void TryGet_missing_returns_false()
    {
        var cache = new MemoryCache(2);
        Assert.False(cache.TryGet("X", out var got));
        Assert.Null(got);
    }

    [Fact]
    public void Evicts_least_recently_used_at_capacity()
    {
        var cache = new MemoryCache(2);
        cache.Put("A", Post("A"));
        cache.Put("B", Post("B"));
        cache.TryGet("A", out _);
        cache.Put("C", Post("C"));

        Assert.False(cache.TryGet("B", out _));
        Assert.True(cache.TryGet("A", out _));
        Assert.True(cache.TryGet("C", out _));
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Put_overwrites_existing_without_growing()
    {
        var cache = new MemoryCache(2);
        cache.Put("A", Post("A"));
        cache.Put("A", Post("A"));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Remove_drops_entry()
    {
        var cache = new MemoryCache(2);
        cache.Put("A", Post("A"));
        cache.Remove("A");
        Assert.False(cache.TryGet("A", out _));
        Assert.Equal(0, cache.Count);
    }
}