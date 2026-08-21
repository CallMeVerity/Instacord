using System.Text.Json;
using Instacord.Parsing;

namespace Instacord.Tests.Parsing;

public class PostSchemaTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserializes_carousel_media_item()
    {
        var json = """
        {
          "__typename": "XIGPolarisImageMedia",
          "code": "DcRxRmwMfqd",
          "display_uri": "https://cdn.example.com/cover.jpg",
          "image_versions2": {
            "candidates": [
              { "url": "https://cdn.example.com/small.jpg", "width": 320 },
              { "url": "https://cdn.example.com/big.jpg", "width": 1290 }
            ]
          },
          "accessibility_caption": "Photo by someone"
        }
        """;

        var media = JsonSerializer.Deserialize<PostSchema.MediaSchema>(json, Options)!;

        Assert.Equal("XIGPolarisImageMedia", media.TypeName);
        Assert.Equal("https://cdn.example.com/cover.jpg", media.DisplayUri);
        Assert.NotNull(media.ImageVersions2);
        Assert.Equal(2, media.ImageVersions2!.Candidates.Count);
        Assert.Equal("https://cdn.example.com/big.jpg", media.ImageVersions2.Candidates[1].Url);
    }

    [Fact]
    public void Deserializes_video_item()
    {
        var json = """
        {
          "__typename": "XIGPolarisVideoMedia",
          "display_uri": "https://cdn.example.com/poster.jpg",
          "video_versions": [
            { "url": "https://cdn.example.com/v480.mp4", "width": 480 },
            { "url": "https://cdn.example.com/v720.mp4", "width": 720 }
          ]
        }
        """;

        var media = JsonSerializer.Deserialize<PostSchema.MediaSchema>(json, Options)!;

        Assert.Equal("XIGPolarisVideoMedia", media.TypeName);
        Assert.NotNull(media.VideoVersions);
        Assert.Equal(2, media.VideoVersions!.Count);
        Assert.Equal("https://cdn.example.com/v720.mp4", media.VideoVersions[1].Url);
    }
}