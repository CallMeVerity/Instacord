using Instacord.Models;

namespace Instacord.Core;

public static class PostMessageBuilder
{
    private const int InstagramAccent = 0xE1306C;

    public static PostMessage Build(InstagramPost post, string postUrl, int? index)
    {
        IReadOnlyList<MediaItem> items = post.Items;
        if (index is { } oneBased)
        {
            if (oneBased < 1 || oneBased > post.Items.Count)
                throw new IndexOutOfRangeException("Album item index is out of range.");
            items = new[] { post.Items[oneBased - 1] };
        }

        var title = string.IsNullOrWhiteSpace(post.Caption)
            ? $"@{post.Username}"
            : post.Caption!;

        return new PostMessage
        {
            Title = title,
            Url = postUrl,
            Username = post.Username,
            Caption = post.Caption,
            Items = items,
            AccentColorRgb = InstagramAccent,
        };
    }
}