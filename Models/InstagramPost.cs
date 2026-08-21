namespace Instacord.Models;

public record InstagramPost
{
    public required string Code { get; init; }
    public required string Username { get; init; }
    public string? FullName { get; init; }
    public string? Caption { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public int? LikeCount { get; init; }
    public int? CommentCount { get; init; }
    public required IReadOnlyList<MediaItem> Items { get; init; }
}