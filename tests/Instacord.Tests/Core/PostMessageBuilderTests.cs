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

    [Fact]
    public void Build_defaults_show_caption_false()
    {
        var message = PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", null);

        Assert.False(message.ShowCaption);
    }

    [Fact]
    public void Build_sets_show_caption_from_param()
    {
        var message = PostMessageBuilder.Build(Post(3), "https://www.instagram.com/p/ABC/", null, true);

        Assert.True(message.ShowCaption);
    }

    [Fact]
    public void Build_carries_like_and_comment_counts()
    {
        var post = Post(1);
        post = post with { LikeCount = 1234, CommentCount = 56 };

        var message = PostMessageBuilder.Build(post, "https://www.instagram.com/p/ABC/", null);

        Assert.Equal(1234, message.LikeCount);
        Assert.Equal(56, message.CommentCount);
    }

    [Fact]
    public void Build_defaults_counts_null_when_post_has_none()
    {
        var message = PostMessageBuilder.Build(Post(1), "https://www.instagram.com/p/ABC/", null);

        Assert.Null(message.LikeCount);
        Assert.Null(message.CommentCount);
    }
}