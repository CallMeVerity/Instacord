using Instacord.Configuration;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<InstacordOptions>(builder.Configuration.GetSection("Instagram"));
builder.Services.AddMemoryCache();

builder.Services
    .AddDiscordGateway(options => options.Intents = 0)
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddApplicationCommands()
    .ConfigureApplicationCommands(options => options.AutoRegisterCommands = true);

var app = builder.Build();

app.AddModules(typeof(Program).Assembly);

app.Run();