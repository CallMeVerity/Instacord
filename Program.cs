using System.Net;
using Instacord.Configuration;
using Instacord.Services;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<InstacordOptions>(builder.Configuration.GetSection("Instagram"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CookieContainer>();
builder.Services.AddHttpClient<IInstagramService, InstagramService>()
    .ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = sp.GetRequiredService<CookieContainer>(),
        AllowAutoRedirect = true,
    });

builder.Services
    .AddDiscordGateway(options => options.Intents = 0)
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddApplicationCommands()
    .ConfigureApplicationCommands(options => options.AutoRegisterCommands = true);

builder.Services.AddComponentInteractions();

var app = builder.Build();

app.AddModules(typeof(Program).Assembly);

app.Run();