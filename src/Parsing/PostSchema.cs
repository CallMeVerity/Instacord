using System.Text.Json;
using System.Text.Json.Serialization;

namespace Instacord.Parsing;

internal sealed class PostSchema
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("carousel_media")]
    public List<MediaSchema>? CarouselMedia { get; set; }

    [JsonPropertyName("__typename")]
    public string? TypeName { get; set; }

    [JsonPropertyName("display_uri")]
    public string? DisplayUri { get; set; }

    [JsonPropertyName("image_versions2")]
    public ImageVersionsSchema? ImageVersions2 { get; set; }

    [JsonPropertyName("video_versions")]
    public List<VideoVersionSchema>? VideoVersions { get; set; }

    [JsonPropertyName("accessibility_caption")]
    public string? AccessibilityCaption { get; set; }

    [JsonPropertyName("caption")]
    public JsonElement? Caption { get; set; }

    [JsonPropertyName("taken_at")]
    public long? TakenAt { get; set; }

    [JsonPropertyName("like_count")]
    public int? LikeCount { get; set; }

    [JsonPropertyName("comment_count")]
    public int? CommentCount { get; set; }

    [JsonPropertyName("user")]
    public UserSchema? User { get; set; }

    public string? CaptionText()
    {
        if (Caption is not { } element)
            return null;
        
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Object when element.TryGetProperty("text", out var text) => text.GetString(),
            _ => null
        };
    }

    internal sealed class MediaSchema
    {
        [JsonPropertyName("__typename")]
        public string? TypeName { get; set; }

        [JsonPropertyName("display_uri")]
        public string? DisplayUri { get; set; }

        [JsonPropertyName("image_versions2")]
        public ImageVersionsSchema? ImageVersions2 { get; set; }

        [JsonPropertyName("video_versions")]
        public List<VideoVersionSchema>? VideoVersions { get; set; }

        [JsonPropertyName("accessibility_caption")]
        public string? AccessibilityCaption { get; set; }
    }

    internal sealed class ImageVersionsSchema
    {
        [JsonPropertyName("candidates")]
        public List<ImageCandidateSchema> Candidates { get; set; } = new();
    }

    internal sealed class ImageCandidateSchema
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    internal sealed class VideoVersionSchema
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    internal sealed class UserSchema
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }
    }
}