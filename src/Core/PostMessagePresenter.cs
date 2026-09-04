using Instacord.Models;
using NetCord;
using NetCord.Rest;

namespace Instacord.Core;

public static class PostMessagePresenter
{
    private const int MaxComponentText = 4000;
    internal const string Footer = "-# via [Instacord](https://github.com/CallMeVerity/Instacord)";
    private const string PaginationPrefix = "igpage";
    private const string RefreshPrefix = "igrefresh";

    public static InteractionMessageProperties Build(PostMessage msg)
    {
        var color = new Color(msg.AccentColorRgb ?? 0xE1306C);

        var children = new List<IComponentContainerComponentProperties>();

        var current = msg.Items[msg.CurrentIndex - 1];
        children.Add(new MediaGalleryProperties([GalleryItem(current)]));

        children.Add(new ComponentSeparatorProperties());
        children.Add(new TextDisplayProperties(BuildText(msg)));

        children.Add(BuildActionRow(msg));
        children.Add(new TextDisplayProperties(Footer));

        var container = new ComponentContainerProperties(children) { AccentColor = color };

        return new InteractionMessageProperties
        {
            Flags = MessageFlags.IsComponentsV2,
            Components = [container],
        };
    }

    private static ActionRowProperties BuildActionRow(PostMessage msg)
    {
        var buttons = new List<IActionRowComponentProperties>();
        var flag = msg.ShowCaption ? 1 : 0;

        if (msg.Items.Count > 1)
        {
            buttons.Add(new ButtonProperties(
                PaginationId(msg.Code, msg.CurrentIndex - 1, flag),
                "Prev",
                ButtonStyle.Secondary)
            {
                Disabled = msg.CurrentIndex <= 1,
            });

            buttons.Add(new ButtonProperties(
                PaginationPrefix + ":count",
                $"{msg.CurrentIndex} / {msg.Items.Count}",
                ButtonStyle.Secondary)
            {
                Disabled = true,
            });

            buttons.Add(new ButtonProperties(
                PaginationId(msg.Code, msg.CurrentIndex + 1, flag),
                "Next",
                ButtonStyle.Secondary)
            {
                Disabled = msg.CurrentIndex >= msg.Items.Count,
            });
        }

        buttons.Add(new LinkButtonProperties(msg.Url, "View on Instagram"));

        if (!msg.Items[msg.CurrentIndex - 1].IsCached)
        {
            buttons.Add(new ButtonProperties(
                RefreshId(msg.Code, msg.CurrentIndex, flag),
                "Refresh",
                ButtonStyle.Secondary));
        }

        return new ActionRowProperties(buttons);
    }

    private static string PaginationId(string code, int index, int flag) =>
        $"{PaginationPrefix}:{code}:{index}:{flag}";

    private static string RefreshId(string code, int index, int flag) =>
        $"{RefreshPrefix}:{code}:{index}:{flag}";

    private static MediaGalleryItemProperties GalleryItem(MediaItem item)
    {
        var properties = new MediaGalleryItemProperties(item.MediaUrl);
        if (!string.IsNullOrEmpty(item.AccessibilityCaption))
            properties = properties.WithDescription(item.AccessibilityCaption);
        return properties;
    }

    private static string BuildText(PostMessage msg)
    {
        var header = BuildHeader(msg);
        var stats = BuildStats(msg);
        var text = stats is null ? header : $"{header}\n{stats}";

        if (!msg.ShowCaption || string.IsNullOrWhiteSpace(msg.Caption))
            return text;

        var caption = $"\n\n{msg.Caption}";
        var budget = MaxComponentText - text.Length;
        if (caption.Length > budget)
            caption = "\n\n" + Truncate(msg.Caption!.TrimStart('\n', '\r'), budget - 2) + "…";

        return text + caption;
    }

    private static string BuildHeader(PostMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.Username))
            return msg.Title;

        return $"### [@{msg.Username}](https://www.instagram.com/{msg.Username}/)";
    }

    private static string? BuildStats(PostMessage msg)
    {
        var parts = new List<string>(2);
        if (msg.LikeCount is { } likes)
            parts.Add($"{FormatCount(likes)} likes");
        if (msg.CommentCount is { } comments)
            parts.Add($"{FormatCount(comments)} comments");
        return parts.Count > 0 ? $"-# {string.Join(" · ", parts)}" : null;
    }

    private static string FormatCount(int value) => value switch
    {
        < 1_000 => value.ToString(),
        < 1_000_000 => value / 1_000.0 % 1 == 0 ? $"{value / 1_000}k" : $"{value / 1_000.0:F1}k",
        _ => value / 1_000_000.0 % 1 == 0 ? $"{value / 1_000_000}M" : $"{value / 1_000_000.0:F1}M",
    };

    private static string Truncate(string value, int max) =>
        max <= 0 ? "" : (value.Length <= max ? value : value[..max]);
}
