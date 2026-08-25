using Instacord.Cache;
namespace Instacord.Tests.Cache;

public class CacheOptionsTests
{
    [Fact]
    public void Defaults_are_set()
    {
        var opts = new CacheOptions { Endpoint = "https://x", Bucket = "b", PublicBaseUrl = "https://x/b", AccessKey = "a", SecretKey = "s" };
        Assert.Equal(24, opts.RefreshAgeHours);
        Assert.Equal(7, opts.TtlDays);
        Assert.Equal(500, opts.MemoryMaxPosts);
        Assert.Equal(3, opts.PersistConcurrency);
    }
}