using Instacord.Models;

namespace Instacord.Core;

public static class PostMessageBuilder
{
    private const int InstagramAccent = 0xE1306C;

    public static PostMessage Build(InstagramPost post, string postUrl, int? index, bool showCaption = false)
    {
        var total = post.Items.Count;
        var current = index ?? 1;
        if (current < 1 || current > total)
            throw new IndexOutOfRangeException("Album item index is out of range.");

        var title = string.IsNullOrWhiteSpace(post.Caption)
            ? $"@{post.Username}"
            : post.Caption!;

        return new PostMessage
        {
            Code = post.Code,
            Title = title,
            Url = postUrl,
            Username = post.Username,
            Caption = post.Caption,
            Items = post.Items,
            CurrentIndex = current,
            ShowCaption = showCaption,
            AccentColorRgb = InstagramAccent,
        };
    }
}