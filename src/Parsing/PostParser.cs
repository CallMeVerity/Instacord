using System.Text.Json;
using System.Text.RegularExpressions;
using Instacord.Models;

namespace Instacord.Parsing;

public static class PostParser
{
    private static readonly Regex Blob = new(
        """<script type="application/json"[^>]*data-sjs>(.*?)</script>""",
        RegexOptions.Singleline);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string MediaKey = "xig_polaris_media";
    private const string GatedKey = "if_not_gated_logged_out";

    public static InstagramPost Parse(string html)
    {
        var schema = ExtractPostSchema(html) ?? throw new InstagramParseException("No Instagram post media found in the response.");
        return Map(schema);
    }

    private static PostSchema? ExtractPostSchema(string html)
    {
        foreach (Match match in Blob.Matches(html))
        {
            var body = match.Groups[1].Value;
            if (!body.Contains(MediaKey) || !body.Contains(GatedKey))
                continue;

            var keyIndex = body.IndexOf(MediaKey, StringComparison.Ordinal);
            var objStart = FindObjectStartAfter(body, keyIndex);
            if (objStart < 0)
                continue;

            var objEnd = FindEnclosingObjectEnd(body, objStart);
            if (objEnd < 0)
                continue;

            var chunk = body.Substring(objStart, objEnd - objStart + 1);

            PostSchema? schema = null;
            try
            {
                using var doc = JsonDocument.Parse(chunk);
                if (doc.RootElement.TryGetProperty(GatedKey, out var gated))
                    schema = gated.Deserialize<PostSchema>(JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (schema is not null && HasMedia(schema))
                return schema;
        }

        return null;
    }

    private static bool HasMedia(PostSchema schema) =>
        (schema.CarouselMedia is { Count: > 0 }) || schema.ImageVersions2 is not null || schema.VideoVersions is { Count: > 0 };

    private static InstagramPost Map(PostSchema schema)
    {
        var items = new List<MediaItem>();

        if (schema.CarouselMedia is { Count: > 0 } carousel)
        {
            items.AddRange(carousel.Select(MapMedia).OfType<MediaItem>());
        }
        else
        {
            var item = MapMedia(new PostSchema.MediaSchema
            {
                TypeName = schema.TypeName,
                DisplayUri = schema.DisplayUri,
                ImageVersions2 = schema.ImageVersions2,
                VideoVersions = schema.VideoVersions,
                AccessibilityCaption = schema.AccessibilityCaption,
            });
            
            if (item is not null)
                items.Add(item);
        }

        if (items.Count == 0)
            throw new InstagramParseException("Post had no usable media items.");

        return new InstagramPost
        {
            Code = schema.Code ?? "",
            Username = schema.User?.Username ?? "",
            FullName = schema.User?.FullName,
            Caption = schema.CaptionText(),
            CreatedAt = schema.TakenAt is { } takenAt
                ? DateTimeOffset.FromUnixTimeSeconds(takenAt)
                : null,
            LikeCount = schema.LikeCount,
            CommentCount = schema.CommentCount,
            Items = items,
        };
    }

    private static MediaItem? MapMedia(PostSchema.MediaSchema media)
    {
        var isVideo = media.VideoVersions is { Count: > 0 } || media.TypeName == "XIGPolarisVideoMedia";
        var type = isVideo ? MediaType.Video : MediaType.Image;

        if (isVideo)
        {
            var video = PickLargestVideo(media.VideoVersions);
            
            if (video is null)
                return null;
            
            return new MediaItem
            {
                Type = type,
                MediaUrl = video.Url,
                DisplayUrl = media.DisplayUri ?? video.Url,
                AccessibilityCaption = media.AccessibilityCaption,
            };
        }

        var image = PickLargestImage(media.ImageVersions2?.Candidates);
        if (image is null)
            return null;
        return new MediaItem
        {
            Type = type,
            MediaUrl = image.Url,
            DisplayUrl = media.DisplayUri ?? image.Url,
            AccessibilityCaption = media.AccessibilityCaption,
        };
    }

    private static PostSchema.VideoVersionSchema? PickLargestVideo(List<PostSchema.VideoVersionSchema>? candidates)
    {
        if (candidates is null || candidates.Count == 0)
            return null;
        
        PostSchema.VideoVersionSchema? best = null;
        var bestWidth = -1;
        foreach (var candidate in candidates.Where(candidate => candidate.Width > bestWidth))
        {
            bestWidth = candidate.Width;
            best = candidate;
        }
        return best;
    }

    private static PostSchema.ImageCandidateSchema? PickLargestImage(List<PostSchema.ImageCandidateSchema>? candidates)
    {
        if (candidates is null || candidates.Count == 0)
            return null;
        PostSchema.ImageCandidateSchema? best = null;
        var bestWidth = -1;
        foreach (var candidate in candidates.Where(candidate => candidate.Width > bestWidth))
        {
            bestWidth = candidate.Width;
            best = candidate;
        }
        return best;
    }

    private static int FindObjectStartAfter(string body, int index)
    {
        for (var i = index; i < body.Length; i++)
        {
            if (body[i] == '{')
                return i;
        }
        return -1;
    }

    private static int FindEnclosingObjectEnd(string body, int start)
    {
        var depth = 0;
        for (var i = start; i < body.Length; i++)
        {
            var c = body[i];
            switch (c)
            {
                case '{':
                    depth++;
                    break;
                case '}':
                {
                    depth--;
                    if (depth == 0)
                        return i;
                    break;
                }
            }
        }
        return -1;
    }
}