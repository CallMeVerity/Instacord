using Instacord.Cache;
using Instacord.Models;
using Instacord.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.Core;
namespace Instacord.Tests.Cache;

public class PostCacheTests
{
    private static InstagramPost Fresh(string code) => new()
    {
        Code = code,
        Username = "u",
        Items = new[] { new MediaItem { Type = MediaType.Image, MediaUrl = "https://cdn/" + code + ".jpg", DisplayUrl = "" } }
    };

    private static InstagramPost Stored(string code) => Fresh(code) with { Items = new[] { new MediaItem { Type = MediaType.Image, MediaUrl = "https://rustfs/" + code + "/1.jpg", DisplayUrl = "" } } };

    private static CacheOptions Opts() => new()
    {
        Endpoint = "https://x", Bucket = "b", PublicBaseUrl = "https://x/b",
        AccessKey = "a", SecretKey = "s", RefreshAgeHours = 24, TtlDays = 7, MemoryMaxPosts = 10
    };

    private static PostPersistJob JobSub() =>
        Substitute.For<PostPersistJob>(
            Substitute.For<IObjectStore>(),
            new HttpClient(),
            Substitute.For<IPostFetcher>(),
            Options.Create(Opts()));

    private sealed class Fixture
    {
        public InMemoryObjectStore Store = new();
        public IPostFetcher Fetcher = Substitute.For<IPostFetcher>();
        public MemoryCache Memory = new(10);
        public PersistWorker Worker = Substitute.For<PersistWorker>(
            JobSub(),
            Options.Create(Opts()),
            (Func<TimeSpan, CancellationToken, Task>?)null);
        public PostCache Cache = null!;

        public void Build()
        {
            Fetcher.ResolveCodeAsync(Arg.Any<string>(), false, Arg.Any<CancellationToken>())
                .Returns(c => c.Arg<string>());
            Cache = new PostCache(Fetcher, Store, Memory, Worker, Options.Create(Opts()), Substitute.For<ILogger<PostCache>>());
        }

        public void PutStored(string code, InstagramPost post, DateTimeOffset cachedAt)
        {
            var envelope = new CachedPostEnvelope { CachedAt = cachedAt, Post = post };
            using var ms = new MemoryStream(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(envelope));
            Store.PutAsync(CacheKeys.MetaKey(code), ms, "application/json").GetAwaiter().GetResult();
        }
    }

    [Fact]
    public async Task Memory_hit_returns_post_without_rustfs_or_instagram()
    {
        var f = new Fixture(); f.Build();
        f.Memory.Put("ABC", Stored("ABC"));

        var post = await f.Cache.GetPostAsync("ABC");

        Assert.NotNull(post);
        Assert.Equal("https://rustfs/ABC/1.jpg", post!.Items[0].MediaUrl);
        await f.Fetcher.DidNotReceive().FetchPostAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rustfs_hit_returns_stored_post_without_instagram()
    {
        var f = new Fixture(); f.Build();
        f.PutStored("ABC", Stored("ABC"), DateTimeOffset.UtcNow);

        var post = await f.Cache.GetPostAsync("ABC");

        Assert.NotNull(post);
        Assert.Equal("https://rustfs/ABC/1.jpg", post!.Items[0].MediaUrl);
        Assert.True(post.Items[0].IsCached);
        await f.Fetcher.DidNotReceive().FetchPostAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.True(f.Memory.TryGet("ABC", out _));
    }

    [Fact]
    public async Task Miss_fetches_instagram_returns_fresh_and_enqueues_persist()
    {
        var f = new Fixture(); f.Build();
        f.Fetcher.FetchPostAsync("ABC", Arg.Any<CancellationToken>()).Returns(Fresh("ABC"));

        var post = await f.Cache.GetPostAsync("ABC");

        Assert.NotNull(post);
        Assert.Equal("https://cdn/ABC.jpg", post!.Items[0].MediaUrl);
        f.Worker.Received(1).Enqueue(Arg.Is<PersistRequest>(r => r.Code == "ABC" && !r.IsRefresh && r.FreshPost != null));
    }

    [Fact]
    public async Task Share_resolves_to_real_code_before_cache_lookup()
    {
        var f = new Fixture(); f.Build();
        f.Fetcher.ResolveCodeAsync("shareTok", true, Arg.Any<CancellationToken>()).Returns("REAL");
        f.Fetcher.FetchPostAsync("REAL", Arg.Any<CancellationToken>()).Returns(Fresh("REAL"));

        var post = await f.Cache.GetPostAsync("shareTok", isShare: true);

        Assert.NotNull(post);
        f.Worker.Received(1).Enqueue(Arg.Is<PersistRequest>(r => r.Code == "REAL"));
    }

    [Fact]
    public async Task Refresh_enqueues_refresh_job_when_entry_older_than_N()
    {
        var f = new Fixture(); f.Build();
        var old = DateTimeOffset.UtcNow.AddHours(-48);
        f.PutStored("ABC", Stored("ABC"), old);

        var post = await f.Cache.GetPostAsync("ABC");

        Assert.NotNull(post);
        Assert.Equal("https://rustfs/ABC/1.jpg", post!.Items[0].MediaUrl);
        f.Worker.Received(1).Enqueue(Arg.Is<PersistRequest>(r => r.Code == "ABC" && r.IsRefresh && r.OnEvict != null));
    }

    [Fact]
    public async Task Expired_entry_is_treated_as_miss()
    {
        var f = new Fixture(); f.Build();
        var ancient = DateTimeOffset.UtcNow.AddDays(-30);
        f.PutStored("ABC", Stored("ABC"), ancient);
        f.Fetcher.FetchPostAsync("ABC", Arg.Any<CancellationToken>()).Returns(Fresh("ABC"));

        var post = await f.Cache.GetPostAsync("ABC");

        Assert.NotNull(post);
        Assert.Equal("https://cdn/ABC.jpg", post!.Items[0].MediaUrl);
        await f.Fetcher.Received(1).FetchPostAsync("ABC", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Null_fetch_returns_null_and_enqueues_nothing()
    {
        var f = new Fixture(); f.Build();
        f.Fetcher.FetchPostAsync("ABC", Arg.Any<CancellationToken>()).Returns((InstagramPost?)null);

        Assert.Null(await f.Cache.GetPostAsync("ABC"));
        f.Worker.DidNotReceive().Enqueue(Arg.Any<PersistRequest>());
    }
}