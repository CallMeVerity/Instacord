using Instacord.Core;
using Instacord.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Instacord.Bot.Commands;

public class InstagramPaginationModule(IInstagramService api) : ComponentInteractionModule<ComponentInteractionContext>
{
    [ComponentInteraction("igpage")]
    public async Task Page(string code, int index)
    {
        var post = await api.GetPostAsync(code);
        if (post is null)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
                new InteractionMessageProperties { Content = "This post is no longer available.", Flags = MessageFlags.Ephemeral }));
            return;
        }

        var current = Math.Clamp(index, 1, post.Items.Count);
        var message = PostMessageBuilder.Build(post, $"https://www.instagram.com/p/{post.Code}/", current);
        var properties = PostMessagePresenter.Build(message);

        await Context.Interaction.SendResponseAsync(InteractionCallback.ModifyMessage(m =>
        {
            m.Components = properties.Components;
        }));
    }
}