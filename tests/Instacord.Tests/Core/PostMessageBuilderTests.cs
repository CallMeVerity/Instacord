using Instacord.Core;
using Instacord.Models;

namespace Instacord.Tests.Core;

public class PostMessageBuilderTests
{
    private static InstagramPost Post(int items) => new()
    {
        Code = "ABC",
        Username = "someone",
        Caption = "hello",
        Items = Enumerable.Range(0, items).Select(_ => new MediaItem
        {
            Type = MediaType.Image,
            MediaUrl = "https://cdn.example.com/img.jpg",
            DisplayUrl = "https://cdn.example.com/img.jpg",
        }).ToList(),
    };

    [Fact]
    public void Build_uses_all_items_when_no_index()
    {
        var message = PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", null);

        Assert.Equal(3, message.Items.Count);
        Assert.Equal("someone", message.Username);
        Assert.Equal("hello", message.Caption);
        Assert.Equal("https://www.instagram.com/p/ABC/", message.Url);
    }

    [Fact]
    public void Build_filters_to_one_item_when_index_given()
    {
        var message = PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", 2);

        Assert.Single(message.Items);
    }

    [Fact]
    public void Build_throws_when_index_out_of_range()
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
            PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", 5));
    }
}