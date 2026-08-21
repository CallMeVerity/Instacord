namespace Instacord.Models;

public record MediaItem
{
    public required MediaType Type { get; init; }
    public required string MediaUrl { get; init; }
    public required string DisplayUrl { get; init; }
    public string? AccessibilityCaption { get; init; }
}