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
        Items = Enumerable.Range(0, items).Select(i => new MediaItem
        {
            Type = MediaType.Image,
            MediaUrl = $"https://cdn.example.com/img{i}.jpg",
            DisplayUrl = $"https://cdn.example.com/img{i}.jpg",
        }).ToList(),
    };

    [Fact]
    public void Build_keeps_all_items_and_starts_at_one_when_no_index()
    {
        var message = PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", null);

        Assert.Equal(3, message.Items.Count);
        Assert.Equal(1, message.CurrentIndex);
        Assert.Equal("ABC", message.Code);
        Assert.Equal("someone", message.Username);
        Assert.Equal("hello", message.Caption);
        Assert.Equal("https://www.instagram.com/p/ABC/", message.Url);
    }

    [Fact]
    public void Build_starts_at_index_when_given()
    {
        var message = PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", 2);

        Assert.Equal(3, message.Items.Count);
        Assert.Equal(2, message.CurrentIndex);
    }

    [Fact]
    public void Build_throws_when_index_out_of_range()
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
            PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", 5));
    }
}