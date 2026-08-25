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
            return JsonSerializer.Deserialize<CachedPostEnvelope>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}