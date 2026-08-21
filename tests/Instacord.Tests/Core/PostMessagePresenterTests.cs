using Instacord.Core;
using Instacord.Models;
using NetCord;
using NetCord.Rest;

namespace Instacord.Tests.Core;

public class PostMessagePresenterTests
{
    private static PostMessage Message(int items) => new()
    {
        Title = "caption",
        Url = "https://www.instagram.com/p/ABC/",
        Username = "someone",
        Caption = "caption",
        Items = Enumerable.Range(0, items).Select(_ => new MediaItem
        {
            Type = MediaType.Image,
            MediaUrl = "https://cdn.example.com/img.jpg",
            DisplayUrl = "https://cdn.example.com/img.jpg",
            AccessibilityCaption = "alt text",
        }).ToList(),
    };

    [Fact]
    public void Build_sets_components_v2_flag()
    {
        var properties = PostMessagePresenter.Build(Message(1));

        Assert.True(properties.Flags.GetValueOrDefault().HasFlag(MessageFlags.IsComponentsV2));
        Assert.NotNull(properties.Components);
    }

    [Fact]
    public void Build_produces_one_container()
    {
        var properties = PostMessagePresenter.Build(Message(2));

        Assert.Single(properties.Components!);
    }
}