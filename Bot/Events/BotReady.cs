using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Instacord.Bot.Events;

public class BotReady(ILogger<BotReady> logger) : IReadyGatewayHandler
{
    public ValueTask HandleAsync(ReadyEventArgs arg)
    {
        logger.LogInformation("Instacord ready; logged in as {UserName} ({BotId}).", arg.User.Username, arg.User.Id);
        return default;
    }
}