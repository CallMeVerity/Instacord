using System.Text.Json;
using System.Text.Json.Serialization;
using Instacord.Models;

namespace Instacord.Cache;

public sealed record CachedPostEnvelope
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [JsonPropertyName("cachedAt")]
    public DateTimeOffset CachedAt { get; init; }

    [JsonPropertyName("post")]
    public required InstagramPost Post { get; init; }

    public static string Serialize(CachedPostEnvelope envelope) =>
        JsonSerializer.Serialize(envelope, Options);

    public static CachedPostEnvelope? Deserialize(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<CachedPostEnvelope>(json, Options);
            if (envelope is null)
                return null;

            return envelope with
            {
                Post = envelope.Post with
                {
                    Items = envelope.Post.Items.Select(item => item with { IsCached = true }).ToList(),
                },
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}