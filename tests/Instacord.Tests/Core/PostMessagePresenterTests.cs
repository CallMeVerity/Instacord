using Instacord.Core;
using Instacord.Models;
using NetCord;
using NetCord.Rest;

namespace Instacord.Tests.Core;

public class PostMessagePresenterTests
{
    private const string RepoUrl = "https://git.nathan.rip/Nathan/Instacord";

    private static PostMessage Message(int items, int current = 1) => new()
    {
        Code = "ABC",
        Title = "caption",
        Url = "https://www.instagram.com/p/ABC/",
        Username = "someone",
        Caption = "caption",
        Items = Enumerable.Range(0, items).Select(i => new MediaItem
        {
            Type = MediaType.Image,
            MediaUrl = $"https://cdn.example.com/img{i}.jpg",
            DisplayUrl = $"https://cdn.example.com/img{i}.jpg",
            AccessibilityCaption = "alt text",
        }).ToList(),
        CurrentIndex = current,
    };

    private static ComponentContainerProperties Container(PostMessage msg)
    {
        var properties = PostMessagePresenter.Build(msg);
        return (ComponentContainerProperties)properties.Components!.First();
    }

    private static List<IActionRowComponentProperties> Buttons(PostMessage msg)
        => Container(msg).Components.OfType<ActionRowProperties>().Single().ToList();

    private static TextDisplayProperties Text(PostMessage msg)
        => Container(msg).Components.OfType<TextDisplayProperties>().Single();

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

    [Fact]
    public void Build_gallery_shows_only_current_item()
    {
        var gallery = Container(Message(3, current: 2))
            .Components.OfType<MediaGalleryProperties>().Single();

        var item = Assert.Single(gallery.Items);
        Assert.Equal("https://cdn.example.com/img1.jpg", item.Media.Url);
    }

    [Fact]
    public void Build_adds_prev_next_buttons_for_album()
    {
        var buttons = Buttons(Message(3, current: 2));

        Assert.Equal(4, buttons.Count);

        var prev = buttons.OfType<ButtonProperties>().Single(b => b.Label == "Prev");
        var next = buttons.OfType<ButtonProperties>().Single(b => b.Label == "Next");
        Assert.Equal("igpage:ABC:1", prev.CustomId);
        Assert.Equal("igpage:ABC:3", next.CustomId);
        Assert.False(prev.Disabled);
        Assert.False(next.Disabled);
    }

    [Fact]
    public void Build_prev_disabled_at_first_item()
    {
        var prev = Buttons(Message(3, current: 1)).OfType<ButtonProperties>().Single(b => b.Label == "Prev");

        Assert.True(prev.Disabled);
    }

    [Fact]
    public void Build_next_disabled_at_last_item()
    {
        var next = Buttons(Message(3, current: 3)).OfType<ButtonProperties>().Single(b => b.Label == "Next");

        Assert.True(next.Disabled);
    }

    [Fact]
    public void Build_no_pagination_buttons_for_single_item()
    {
        var buttons = Buttons(Message(1));

        var button = Assert.Single(buttons);
        Assert.IsType<LinkButtonProperties>(button);
    }

    [Fact]
    public void Build_counter_button_shows_position()
    {
        var counter = Buttons(Message(4, current: 2))
            .OfType<ButtonProperties>().Single(b => (b.Label ?? string.Empty).Contains('/'));

        Assert.Equal("2 / 4", counter.Label);
        Assert.True(counter.Disabled);
        Assert.False(string.IsNullOrEmpty(counter.CustomId));
    }

    [Fact]
    public void Build_every_button_has_a_nonempty_custom_id()
    {
        var buttons = Buttons(Message(4, current: 2));

        foreach (var b in buttons.OfType<ButtonProperties>())
            Assert.False(string.IsNullOrEmpty(b.CustomId));
    }

    [Fact]
    public void Build_username_header_links_to_profile()
    {
        var text = Text(Message(2)).Content;

        Assert.Contains("### [@someone](https://www.instagram.com/someone/)", text);
    }

    [Fact]
    public void Build_footer_links_to_repo_once()
    {
        var text = Text(Message(2)).Content;

        Assert.Equal(1, text.Split(RepoUrl, StringSplitOptions.None).Length - 1);
        Assert.Contains("via [Instacord](" + RepoUrl + ")", text);
    }
}