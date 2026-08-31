using System.Net;
using Instacord.Cache;
using Instacord.Configuration;
using Instacord.Services;
using Microsoft.Extensions.Options;
using Minio;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<InstacordOptions>(builder.Configuration.GetSection("Instagram"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));

builder.Services.AddSingleton<CookieContainer>();
builder.Services.AddHttpClient<InstagramService>()
    .ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = sp.GetRequiredService<CookieContainer>(),
        AllowAutoRedirect = true,
    });

builder.Services.AddSingleton<IPostFetcher>(sp => sp.GetRequiredService<InstagramService>());

builder.Services.AddHttpClient<PostPersistJob>();
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<CacheOptions>>().Value;
    var secure = opts.Endpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase);
    var host = opts.Endpoint.Replace("https://", "", StringComparison.OrdinalIgnoreCase).Replace("http://", "", StringComparison.OrdinalIgnoreCase);
    return new MinioClient()
        .WithEndpoint(host)
        .WithCredentials(opts.AccessKey, opts.SecretKey)
        .WithSSL(secure)
        .Build();
});

builder.Services.AddSingleton<IObjectStore, MinioObjectStore>();
builder.Services.AddSingleton<MemoryCache>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<CacheOptions>>().Value;
    return new MemoryCache(opts.MemoryMaxPosts);
});

builder.Services.AddSingleton<PersistWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PersistWorker>());
builder.Services.AddSingleton<IInstagramService, PostCache>();
builder.Services.AddHostedService<CacheSweeper>();

builder.Services
    .AddDiscordGateway(options => options.Intents = 0)
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddApplicationCommands()
    .ConfigureApplicationCommands(options => options.AutoRegisterCommands = true);

builder.Services.AddComponentInteractions();

var app = builder.Build();

app.AddModules(typeof(Program).Assembly);

app.Run();