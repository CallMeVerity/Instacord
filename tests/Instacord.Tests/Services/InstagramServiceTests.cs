using System.Net;
using Instacord.Configuration;
using Instacord.Services;
using Instacord.Tests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Instacord.Tests.Services;

public class InstagramServiceTests
{
    private static (InstagramService service, StubHandler handler) Build(int cacheSeconds = 900)
    {
        var handler = new StubHandler();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton<CookieContainer>();
        services.AddHttpClient<InstagramService>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddSingleton(Options.Create(new InstacordOptions
        {
            UserAgent = "TestAgent/1.0",
            RequestTimeoutSeconds = 5,
            CacheSeconds = cacheSeconds,
            CookieHeader = "",
            OwnerUserId = 1,
        }));
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<InstagramService>();
        return (service, handler);
    }

    [Fact]
    public async Task GetPostAsync_returns_post_from_album_html()
    {
        var (service, handler) = Build();
        handler.RespondWith("https://www.instagram.com/p/DcRxVgRjOHs/", HttpStatusCode.OK, FixtureLoader.Load("album.html"));

        var post = await service.GetPostAsync("DcRxVgRjOHs");

        Assert.NotNull(post);
        Assert.NotEmpty(post!.Items);
    }

    [Fact]
    public async Task GetPostAsync_returns_null_on_wall()
    {
        var (service, handler) = Build();
        handler.RespondWith("https://www.instagram.com/p/missing/", HttpStatusCode.OK, FixtureLoader.Load("wall.html"));

        Assert.Null(await service.GetPostAsync("missing"));
    }

    [Fact]
    public async Task GetPostAsync_uses_cache_on_second_call()
    {
        var (service, handler) = Build();
        const string postUrl = "https://www.instagram.com/p/DcRxVgRjOHs/";
        handler.RespondWith(postUrl, HttpStatusCode.OK, FixtureLoader.Load("album.html"));

        await service.GetPostAsync("DcRxVgRjOHs");
        await service.GetPostAsync("DcRxVgRjOHs");

        Assert.Equal(1, handler.RequestedUris.Count(u => u == postUrl));
    }

    [Fact]
    public async Task GetPostAsync_resolves_share_redirect()
    {
        var (service, handler) = Build();
        handler.RedirectFrom("https://www.instagram.com/share/AbCdEf/", "https://www.instagram.com/p/Resolved/");
        handler.RespondWith("https://www.instagram.com/p/Resolved/", HttpStatusCode.OK, FixtureLoader.Load("album.html"));

        var post = await service.GetPostAsync("AbCdEf", default, isShare: true);

        Assert.NotNull(post);
        Assert.Equal("DcRxVgRjOHs", post!.Code);
    }
}