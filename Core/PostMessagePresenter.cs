using Instacord.Models;
using NetCord;
using NetCord.Rest;

namespace Instacord.Core;

public static class PostMessagePresenter
{
    private const int MaxComponentText = 4000;
    private const string Footer = "-# via [Instacord](https://git.nathan.rip/Nathan/Instacord)";
    private const string PaginationPrefix = "igpage";

    public static InteractionMessageProperties Build(PostMessage msg)
    {
        var color = new Color(msg.AccentColorRgb ?? 0xE1306C);

        var children = new List<IComponentContainerComponentProperties>();

        var current = msg.Items[msg.CurrentIndex - 1];
        children.Add(new MediaGalleryProperties(new[] { GalleryItem(current) }));

        children.Add(new ComponentSeparatorProperties());
        children.Add(new TextDisplayProperties(BuildText(msg)));

        children.Add(BuildActionRow(msg));

        var container = new ComponentContainerProperties(children) { AccentColor = color };

        return new InteractionMessageProperties
        {
            Flags = MessageFlags.IsComponentsV2,
            Components = new IMessageComponentProperties[] { container },
        };
    }

    private static ActionRowProperties BuildActionRow(PostMessage msg)
    {
        var buttons = new List<IActionRowComponentProperties>();

        if (msg.Items.Count > 1)
        {
            buttons.Add(new ButtonProperties(
                PaginationId(msg.Code, msg.CurrentIndex - 1),
                "Prev",
                ButtonStyle.Secondary)
            {
                Disabled = msg.CurrentIndex <= 1,
            });

            buttons.Add(new ButtonProperties(
                "",
                $"{msg.CurrentIndex} / {msg.Items.Count}",
                ButtonStyle.Secondary)
            {
                Disabled = true,
            });

            buttons.Add(new ButtonProperties(
                PaginationId(msg.Code, msg.CurrentIndex + 1),
                "Next",
                ButtonStyle.Secondary)
            {
                Disabled = msg.CurrentIndex >= msg.Items.Count,
            });
        }

        buttons.Add(new LinkButtonProperties(msg.Url, "View on Instagram"));

        return new ActionRowProperties(buttons);
    }

    private static string PaginationId(string code, int index) => $"{PaginationPrefix}:{code}:{index}";

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
        var caption = string.IsNullOrWhiteSpace(msg.Caption) ? "" : $"\n\n{msg.Caption}";
        var footer = $"\n\n{Footer}";

        var budget = MaxComponentText - header.Length - footer.Length;
        if (caption.Length > budget)
            caption = "\n\n" + Truncate(caption.TrimStart('\n', '\r'), budget - 2) + "…";

        return header + caption + footer;
    }

    private static string BuildHeader(PostMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.Username))
            return msg.Title;

        return $"### [@{msg.Username}](https://www.instagram.com/{msg.Username}/)";
    }

    private static string Truncate(string value, int max) =>
        max <= 0 ? "" : (value.Length <= max ? value : value[..max]);
}