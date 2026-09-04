using Instacord.Cache;
using Instacord.Models;
namespace Instacord.Tests.Cache;

public class CachedPostEnvelopeTests
{
    private static InstagramPost Post() => new()
    {
        Code = "ABC",
        Username = "u",
        Caption = "hi",
        Items = new[]
        {
            new MediaItem { Type = MediaType.Image, MediaUrl = "https://x/a.jpg", DisplayUrl = "https://x/a.jpg" }
        }
    };

    [Fact]
    public void Roundtrips_post_and_cachedAt()
    {
        var envelope = new CachedPostEnvelope
        {
            CachedAt = DateTimeOffset.Parse("2026-08-25T12:00:00Z"),
            Post = Post()
        };

        var json = CachedPostEnvelope.Serialize(envelope);
        var back = CachedPostEnvelope.Deserialize(json);

        Assert.NotNull(back);
        Assert.Equal("ABC", back!.Post.Code);
        Assert.Equal("hi", back.Post.Caption);
        Assert.Single(back.Post.Items);
        Assert.Equal(MediaType.Image, back.Post.Items[0].Type);
        Assert.Equal(envelope.CachedAt, back.CachedAt);
    }

    [Fact]
    public void Deserialize_marks_items_as_cached()
    {
        const string json = """
            {"cachedAt":"2026-08-25T12:00:00Z","post":{"code":"ABC","username":"u","items":[{"type":0,"mediaUrl":"https://rustfs/ABC/1.jpg","displayUrl":""}]}}
            """;

        var envelope = CachedPostEnvelope.Deserialize(json);

        Assert.NotNull(envelope);
        Assert.True(envelope!.Post.Items[0].IsCached);
    }

    [Fact]
    public void Deserialize_returns_null_for_invalid_json()
    {
        Assert.Null(CachedPostEnvelope.Deserialize("not json"));
    }
}