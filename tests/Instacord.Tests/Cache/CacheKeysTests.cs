using Instacord.Cache;
namespace Instacord.Tests.Cache;

public class CacheKeysTests
{
    [Fact]
    public void MetaKey_is_posts_code_meta_json()
    {
        Assert.Equal("posts/ABC123/meta.json", CacheKeys.MetaKey("ABC123"));
    }

    [Fact]
    public void MediaKey_uses_index_and_extension()
    {
        Assert.Equal("posts/ABC123/1.jpg", CacheKeys.MediaKey("ABC123", 1, "jpg"));
        Assert.Equal("posts/ABC123/3.mp4", CacheKeys.MediaKey("ABC123", 3, "mp4"));
    }

    [Fact]
    public void PublicMediaUrl_combines_base_code_index_ext()
    {
        var url = CacheKeys.PublicMediaUrl("https://rustfs.nathan.rip/instacord", "ABC123", 2, "mp4");
        Assert.Equal("https://rustfs.nathan.rip/instacord/posts/ABC123/2.mp4", url);
    }

    [Fact]
    public void PublicMediaUrl_trims_trailing_slash_on_base()
    {
        var url = CacheKeys.PublicMediaUrl("https://rustfs.nathan.rip/instacord/", "ABC", 1, "jpg");
        Assert.Equal("https://rustfs.nathan.rip/instacord/posts/ABC/1.jpg", url);
    }

    [Fact]
    public void CodePrefix_is_posts_code_slash()
    {
        Assert.Equal("posts/ABC123/", CacheKeys.CodePrefix("ABC123"));
    }
}