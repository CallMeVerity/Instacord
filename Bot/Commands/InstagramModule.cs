using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Instacord.Core;
using Instacord.Parsing;
using Instacord.Services;

namespace Instacord.Bot.Commands;

public class InstagramModule(IInstagramService api) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ig", "Embed an Instagram post or reel",
        Contexts = [InteractionContextType.Guild, InteractionContextType.BotDMChannel, InteractionContextType.DMChannel],
        IntegrationTypes = [ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall])]
    public async Task Ig(
        [SlashCommandParameter(Name = "url", Description = "Instagram post or reel link")] string url,
        [SlashCommandParameter(Name = "index", Description = "Single album item, 1-based")] int? index = null)
    {
        var parsed = InstagramUrlParser.TryParse(url);
        if (parsed is null)
        {
            await RespondEphemeral("Send an Instagram post or reel link.");
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        var post = await api.GetPostAsync(parsed.Code, default, parsed.IsShare);
        if (post is null)
        {
            await ModifyEphemeral("Instagram blocked the request or that post is not available. Try again in a moment.");
            return;
        }

        PostMessage message;
        try
        {
            message = PostMessageBuilder.Build(post, $"https://www.instagram.com/p/{post.Code}/", index);
        }
        catch (IndexOutOfRangeException)
        {
            await ModifyEphemeral("That item does not exist in this post.");
            return;
        }

        var properties = PostMessagePresenter.Build(message);
        await Context.Client.Rest.ModifyInteractionResponseAsync(
            Context.Interaction.ApplicationId,
            Context.Interaction.Token,
            m =>
            {
                m.Components = properties.Components;
                m.Flags = properties.Flags;
            });
    }

    private async Task RespondEphemeral(string text)
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
            new InteractionMessageProperties { Content = text, Flags = MessageFlags.Ephemeral }));
    }

    private async Task ModifyEphemeral(string text)
    {
        await Context.Client.Rest.ModifyInteractionResponseAsync(
            Context.Interaction.ApplicationId,
            Context.Interaction.Token,
            m => m.Content = text);
    }
}