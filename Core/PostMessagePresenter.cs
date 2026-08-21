using Instacord.Models;
using NetCord;
using NetCord.Rest;

namespace Instacord.Core;

public static class PostMessagePresenter
{
    private const int MaxComponentText = 4000;
    private const string Footer = "-# via Instacord";

    public static InteractionMessageProperties Build(PostMessage msg)
    {
        var color = new Color(msg.AccentColorRgb ?? 0xE1306C);

        var children = new List<IComponentContainerComponentProperties>();

        var galleryItems = msg.Items.Select(GalleryItem).ToArray();
        children.Add(new MediaGalleryProperties(galleryItems));

        children.Add(new ComponentSeparatorProperties());
        children.Add(new TextDisplayProperties(BuildText(msg)));

        children.Add(new ActionRowProperties(new IActionRowComponentProperties[]
        {
            new LinkButtonProperties(msg.Url, "View on Instagram"),
        }));

        children.Add(new TextDisplayProperties(Footer));

        var container = new ComponentContainerProperties(children) { AccentColor = color };

        return new InteractionMessageProperties
        {
            Flags = MessageFlags.IsComponentsV2,
            Components = new IMessageComponentProperties[] { container },
        };
    }

    private static MediaGalleryItemProperties GalleryItem(MediaItem item)
    {
        var properties = new MediaGalleryItemProperties(item.MediaUrl);
        if (!string.IsNullOrEmpty(item.AccessibilityCaption))
            properties = properties.WithDescription(item.AccessibilityCaption);
        return properties;
    }

    private static string BuildText(PostMessage msg)
    {
        var header = string.IsNullOrWhiteSpace(msg.Username) ? msg.Title : $"### @{msg.Username}";
        var caption = string.IsNullOrWhiteSpace(msg.Caption) ? "" : $"\n\n{msg.Caption}";
        var footer = $"\n\n{Footer}";

        var budget = MaxComponentText - header.Length - footer.Length;
        if (caption.Length > budget)
            caption = "\n\n" + Truncate(caption.TrimStart('\n', '\r'), budget - 2) + "…";

        return header + caption + footer;
    }

    private static string Truncate(string value, int max) =>
        max <= 0 ? "" : (value.Length <= max ? value : value[..max]);
}