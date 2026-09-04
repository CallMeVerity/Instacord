using Instacord.Core;
using Instacord.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Instacord.Bot.Commands;

public class InstagramPaginationModule(IInstagramService api) : ComponentInteractionModule<ComponentInteractionContext>
{
    [ComponentInteraction("igpage")]
    public Task Page(string code, int index, int withCaption) => RedrawAsync(code, index, withCaption);

    [ComponentInteraction("igrefresh")]
    public Task Refresh(string code, int index, int withCaption) => RedrawAsync(code, index, withCaption);

    // Instagram CDN links expire, so this always goes through the cache-first service: if the
    // post has been persisted, the embed gets rebuilt with cached object-store media URLs.
    private async Task RedrawAsync(string code, int index, int withCaption)
    {
        var post = await api.GetPostAsync(code);
        if (post is null)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(
                new InteractionMessageProperties { Content = "This post is no longer available.", Flags = MessageFlags.Ephemeral }));
            return;
        }

        var current = Math.Clamp(index, 1, post.Items.Count);
        var showCaption = withCaption != 0;
        var message = PostMessageBuilder.Build(post, $"https://www.instagram.com/p/{post.Code}/", current, showCaption);
        var properties = PostMessagePresenter.Build(message);

        await Context.Interaction.SendResponseAsync(InteractionCallback.ModifyMessage(m =>
        {
            m.Components = properties.Components;
        }));
    }
}