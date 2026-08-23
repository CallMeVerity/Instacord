using System.Text.RegularExpressions;

namespace Instacord.Parsing;

public static class InstagramUrlParser
{
    private static readonly Regex PostOrReel = new(
        @"^https?://(?:www\.)?instagram\.com/(?:[^/]+/)?(?:p|reels?)/(?<code>[A-Za-z0-9_-]+)",
        RegexOptions.IgnoreCase);

    private static readonly Regex Share = new(
        @"^https?://(?:www\.)?instagram\.com/share/(?<code>[A-Za-z0-9_-]+)",
        RegexOptions.IgnoreCase);

    private static readonly Regex Stories = new(
        @"^https?://(?:www\.)?instagram\.com/stories/",
        RegexOptions.IgnoreCase);

    public static InstagramUrl? TryParse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();

        if (Stories.IsMatch(trimmed))
            return null;

        var share = Share.Match(trimmed);
        if (share.Success)
            return new InstagramUrl(share.Groups["code"].Value, true);

        var post = PostOrReel.Match(trimmed);
        if (post.Success)
            return new InstagramUrl(post.Groups["code"].Value, false);

        return null;
    }
}