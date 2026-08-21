using Instacord.Models;

namespace Instacord.Core;

public record PostMessage
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string Username { get; init; }
    public string? Caption { get; init; }
    public required IReadOnlyList<MediaItem> Items { get; init; }
    public int? AccentColorRgb { get; init; }
}