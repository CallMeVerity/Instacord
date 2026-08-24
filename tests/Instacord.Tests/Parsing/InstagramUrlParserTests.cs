using Instacord.Parsing;

namespace Instacord.Tests.Parsing;

public class InstagramUrlParserTests
{
    [Theory]
    [InlineData("https://www.instagram.com/p/DcRxVgRjOHs/", "DcRxVgRjOHs", false)]
    [InlineData("https://instagram.com/p/DcRxVgRjOHs", "DcRxVgRjOHs", false)]
    [InlineData("http://www.instagram.com/p/DcRxVgRjOHs/?igsh=abc", "DcRxVgRjOHs", false)]
    [InlineData("https://www.instagram.com/reel/DcIDuk4BALD/", "DcIDuk4BALD", false)]
    [InlineData("https://www.instagram.com/reels/DcIDuk4BALD/", "DcIDuk4BALD", false)]
    [InlineData("https://www.instagram.com/someuser/p/DcRxVgRjOHs/", "DcRxVgRjOHs", false)]
    [InlineData("https://www.instagram.com/share/AbCdEf123/", "AbCdEf123", true)]
    public void Parses_known_paths(string input, string expectedCode, bool expectedIsShare)
    {
        var url = InstagramUrlParser.TryParse(input);

        Assert.NotNull(url);
        Assert.Equal(expectedCode, url!.Code);
        Assert.Equal(expectedIsShare, url.IsShare);
    }

    [Theory]
    [InlineData("https://www.kkinstagram.com/reel/DcNq_YMS0Jo")]
    [InlineData("https://www.facebook.com/p/DcRxVgRjOHs")]
    [InlineData("https://www.instagram.com/stories/user/12345")]
    [InlineData("https://www.instagram.com/explore/")]
    [InlineData("not a url")]
    public void Rejects_unsupported(string input)
    {
        Assert.Null(InstagramUrlParser.TryParse(input));
    }
}