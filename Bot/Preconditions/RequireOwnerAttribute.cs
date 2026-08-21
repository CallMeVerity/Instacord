using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetCord.Services;
using Instacord.Configuration;

namespace Instacord.Bot.Preconditions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireOwnerAttribute<TContext> : PreconditionAttribute<TContext> where TContext : IUserContext
{
    public override ValueTask<PreconditionResult> EnsureCanExecuteAsync(TContext context, IServiceProvider? serviceProvider)
    {
        var owner = serviceProvider?.GetRequiredService<IOptions<InstacordOptions>>().Value.OwnerUserId ?? 0;
        return OwnerGate.IsOwner(context.User.Id, owner)
            ? new(PreconditionResult.Success)
            : new(PreconditionResult.Fail("This bot is private."));
    }
}