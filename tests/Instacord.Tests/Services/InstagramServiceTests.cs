using System.Net;
using Instacord.Configuration;
using Instacord.Services;
using Instacord.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Instacord.Tests.Services;

public class InstagramServiceTests
{
    private static (InstagramService service, StubHandler handler) Build()
    {
        var handler = new StubHandler();
        var services = new ServiceCollection();
        services.AddSingleton<CookieContainer>();
        services.AddHttpClient<InstagramService>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddSingleton(Options.Create(new InstacordOptions
        {
            UserAgent = "TestAgent/1.0",
            RequestTimeoutSeconds = 5,
            CookieHeader = "",
        }));
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<InstagramService>();
        return (service, handler);
    }

    [Fact]
    public async Task FetchPostAsync_returns_post_from_album_html()
    {
        var (service, handler) = Build();
        handler.RespondWith("https://www.instagram.com/p/DcRxVgRjOHs/", HttpStatusCode.OK, FixtureLoader.Load("album.html"));

        var post = await service.FetchPostAsync("DcRxVgRjOHs");

        Assert.NotNull(post);
        Assert.NotEmpty(post!.Items);
    }

    [Fact]
    public async Task FetchPostAsync_returns_null_on_wall()
    {
        var (service, handler) = Build();
        handler.RespondWith("https://www.instagram.com/p/missing/", HttpStatusCode.OK, FixtureLoader.Load("wall.html"));

        Assert.Null(await service.FetchPostAsync("missing"));
    }

    [Fact]
    public async Task ResolveCodeAsync_resolves_share_redirect()
    {
        var (service, handler) = Build();
        handler.RedirectFrom("https://www.instagram.com/share/AbCdEf/", "https://www.instagram.com/p/Resolved/");
        handler.RespondWith("https://www.instagram.com/p/Resolved/", HttpStatusCode.OK, FixtureLoader.Load("album.html"));

        var code = await service.ResolveCodeAsync("AbCdEf", isShare: true);

        Assert.NotNull(code);
        var post = await service.FetchPostAsync(code!);
        Assert.NotNull(post);
        Assert.Equal("DcRxVgRjOHs", post!.Code);
    }

    [Fact]
    public async Task ResolveCodeAsync_passes_through_non_share_code()
    {
        var (service, _) = Build();
        var code = await service.ResolveCodeAsync("ABC123", isShare: false);
        Assert.Equal("ABC123", code);
    }
}