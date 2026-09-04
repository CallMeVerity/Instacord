using System.Net;
using Instacord.Cache;
using Instacord.Models;
using Instacord.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
namespace Instacord.Tests.Cache;

public class PostPersistJobTests
{
    private static InstagramPost Fresh(string code, string mediaUrl, MediaType type) => new()
    {
        Code = code,
        Username = "u",
        Items = new[]
        {
            new MediaItem { Type = type, MediaUrl = mediaUrl, DisplayUrl = mediaUrl }
        }
    };

    private static CacheOptions Opts() => new()
    {
        Endpoint = "https://x", Bucket = "b", PublicBaseUrl = "https://x/b",
        AccessKey = "a", SecretKey = "s", PersistMaxAttempts = 3
    };

    private static (PostPersistJob job, StubHandler handler, InMemoryObjectStore store) Build()
    {
        var handler = new StubHandler();
        var http = new HttpClient(handler);
        var store = new InMemoryObjectStore();
        var fetcher = Substitute.For<IPostFetcher>();
        var job = new PostPersistJob(store, http, fetcher, Options.Create(Opts()));
        return (job, handler, store);
    }

    [Fact]
    public async Task Initial_persist_downloads_media_and_writes_meta_with_rustfs_urls()
    {
        var (job, handler, store) = Build();
        var mediaUrl = "https://cdn.instagram.com/abc.jpg";
        handler.RespondWith(mediaUrl, HttpStatusCode.OK, "imgbytes", "image/jpeg");

        var ok = await job.RunAsync(new PersistRequest("ABC", Fresh("ABC", mediaUrl, MediaType.Image), IsRefresh: false, OnEvict: null), default);

        Assert.True(ok);
        var media = await store.GetAsync(CacheKeys.MediaKey("ABC", 1, "jpg"));
        Assert.NotNull(media);
        var meta = await store.GetAsync(CacheKeys.MetaKey("ABC"));
        Assert.NotNull(meta);
        using var reader = new StreamReader(meta!);
        var envelope = CachedPostEnvelope.Deserialize(await reader.ReadToEndAsync());
        Assert.NotNull(envelope);
        Assert.Equal(CacheKeys.PublicMediaUrl("https://x/b", "ABC", 1, "jpg"), envelope!.Post.Items[0].MediaUrl);
        Assert.True(envelope.Post.Items[0].IsCached);
    }

    [Fact]
    public async Task Initial_persist_returns_false_and_writes_no_meta_when_media_download_fails()
    {
        var (job, handler, store) = Build();
        var mediaUrl = "https://cdn.instagram.com/abc.jpg";
        handler.RespondWith(mediaUrl, HttpStatusCode.InternalServerError, "boom", "image/jpeg");

        var ok = await job.RunAsync(new PersistRequest("ABC", Fresh("ABC", mediaUrl, MediaType.Image), IsRefresh: false, OnEvict: null), default);

        Assert.False(ok);
        Assert.Null(await store.GetAsync(CacheKeys.MetaKey("ABC")));
    }

    [Fact]
    public async Task Refresh_refetches_and_overwrites_when_post_still_present()
    {
        var handler = new StubHandler();
        var http = new HttpClient(handler);
        var store = new InMemoryObjectStore();
        var fetcher = Substitute.For<IPostFetcher>();
        var fresh = Fresh("ABC", "https://cdn.instagram.com/abc.jpg", MediaType.Image);
        fetcher.FetchPostAsync("ABC", Arg.Any<CancellationToken>()).Returns(fresh);
        handler.RespondWith("https://cdn.instagram.com/abc.jpg", HttpStatusCode.OK, "img", "image/jpeg");
        var job = new PostPersistJob(store, http, fetcher, Options.Create(Opts()));

        var evicted = "";
        var ok = await job.RunAsync(new PersistRequest("ABC", FreshPost: null, IsRefresh: true, OnEvict: c => evicted = c), default);

        Assert.True(ok);
        Assert.Equal("", evicted);
        Assert.NotNull(await store.GetAsync(CacheKeys.MetaKey("ABC")));
    }

    [Fact]
    public async Task Refresh_evicts_when_post_gone_from_instagram()
    {
        var handler = new StubHandler();
        var http = new HttpClient(handler);
        var store = new InMemoryObjectStore();
        await store.PutAsync(CacheKeys.MetaKey("ABC"), new MemoryStream("old"u8.ToArray()), "application/json");
        await store.PutAsync(CacheKeys.MediaKey("ABC", 1, "jpg"), new MemoryStream("old"u8.ToArray()), "image/jpeg");
        var fetcher = Substitute.For<IPostFetcher>();
        fetcher.FetchPostAsync("ABC", Arg.Any<CancellationToken>()).Returns((InstagramPost?)null);
        var job = new PostPersistJob(store, http, fetcher, Options.Create(Opts()));

        var evicted = "";
        var ok = await job.RunAsync(new PersistRequest("ABC", FreshPost: null, IsRefresh: true, OnEvict: c => evicted = c), default);

        Assert.True(ok);
        Assert.Equal("ABC", evicted);
        Assert.Null(await store.GetAsync(CacheKeys.MetaKey("ABC")));
        Assert.Null(await store.GetAsync(CacheKeys.MediaKey("ABC", 1, "jpg")));
    }
}