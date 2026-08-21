using Instacord.Models;
using Instacord.Parsing;
using Instacord.Tests.Fixtures;

namespace Instacord.Tests.Parsing;

public class PostParserTests
{
    [Fact]
    public void Parses_album_into_multiple_items()
    {
        var html = FixtureLoader.Load("album.html");
        var post = PostParser.Parse(html);

        Assert.NotNull(post);
        Assert.NotEmpty(post!.Items);
        Assert.All(post.Items, item => Assert.False(string.IsNullOrEmpty(item.MediaUrl)));
        Assert.Contains(post.Items, item => item.Type == MediaType.Image || item.Type == MediaType.Video);
        Assert.False(string.IsNullOrEmpty(post.Username));
    }

    [Fact]
    public void Parses_reel_into_single_video_item()
    {
        var html = FixtureLoader.Load("reel.html");
        var post = PostParser.Parse(html);

        Assert.NotNull(post);
        Assert.Single(post!.Items);
        Assert.Equal(MediaType.Video, post.Items[0].Type);
        Assert.False(string.IsNullOrEmpty(post.Items[0].MediaUrl));
    }

    [Fact]
    public void Throws_on_wall_html()
    {
        var html = FixtureLoader.Load("wall.html");

        Assert.Throws<InstagramParseException>(() => PostParser.Parse(html));
    }
}