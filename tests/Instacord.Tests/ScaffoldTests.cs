using Microsoft.Extensions.Configuration;
using Instacord.Configuration;

namespace Instacord.Tests;

public class ScaffoldTests
{
    [Fact]
    public void Options_bind_from_config()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Instagram:UserAgent"] = "TestAgent/1.0",
                ["Instagram:RequestTimeoutSeconds"] = "30",
                ["Instagram:CacheSeconds"] = "120",
                ["Instagram:CookieHeader"] = "sessionid=abc",
            })
            .Build();

        var options = new InstacordOptions { UserAgent = "" };
        config.GetSection("Instagram").Bind(options);

        Assert.Equal("TestAgent/1.0", options.UserAgent);
        Assert.Equal(30, options.RequestTimeoutSeconds);
        Assert.Equal(120, options.CacheSeconds);
        Assert.Equal("sessionid=abc", options.CookieHeader);
    }
}