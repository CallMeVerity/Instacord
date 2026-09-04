using Instacord.Core;
using Instacord.Models;
using NetCord;
using NetCord.Rest;

namespace Instacord.Tests.Core;

public class PostMessagePresenterTests
{
    private const string RepoUrl = "https://git.nathan.rip/Nathan/Instacord";

    private static PostMessage Message(int items, int current = 1, bool showCaption = false) => new()
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
        ShowCaption = showCaption,
    };

    private static ComponentContainerProperties Container(PostMessage msg)
    {
        var properties = PostMessagePresenter.Build(msg);
        return (ComponentContainerProperties)properties.Components!.First();
    }

    private static List<IActionRowComponentProperties> Buttons(PostMessage msg)
        => Container(msg).Components.OfType<ActionRowProperties>().Single().ToList();

    private static TextDisplayProperties HeaderText(PostMessage msg)
        => Container(msg).Components.OfType<TextDisplayProperties>().First(t => t.Content != PostMessagePresenter.Footer);

    private static TextDisplayProperties FooterText(PostMessage msg)
        => Container(msg).Components.OfType<TextDisplayProperties>().Single(t => t.Content == PostMessagePresenter.Footer);

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
        Assert.Equal("https://cdn.example.com/img1.jpg", item.Media!.Url);
    }

    [Fact]
    public void Build_adds_prev_next_buttons_for_album()
    {
        var buttons = Buttons(Message(3, current: 2));

        Assert.Equal(5, buttons.Count);

        var prev = buttons.OfType<ButtonProperties>().Single(b => b.Label == "Prev");
        var next = buttons.OfType<ButtonProperties>().Single(b => b.Label == "Next");
        Assert.Equal("igpage:ABC:1:0", prev.CustomId);
        Assert.Equal("igpage:ABC:3:0", next.CustomId);
        Assert.False(prev.Disabled);
        Assert.False(next.Disabled);
    }

    [Fact]
    public void Build_adds_refresh_button_for_album()
    {
        var refresh = Buttons(Message(3, current: 2))
            .OfType<ButtonProperties>()
            .Single(b => b.Label == "Refresh");

        Assert.Equal("igrefresh:ABC:2:0", refresh.CustomId);
        Assert.False(refresh.Disabled);
    }

    [Fact]
    public void Build_refresh_id_preserves_index_and_caption_flag()
    {
        var refresh = Buttons(Message(4, current: 3, showCaption: true))
            .OfType<ButtonProperties>()
            .Single(b => b.Label == "Refresh");

        Assert.Equal("igrefresh:ABC:3:1", refresh.CustomId);
    }

    [Fact]
    public void Build_refresh_button_is_last_interactive_button()
    {
        var row = Container(Message(3, current: 2)).Components.OfType<ActionRowProperties>().Single().ToList();

        Assert.IsType<ButtonProperties>(row[^2]);
        Assert.Equal("Refresh", ((ButtonProperties)row[^2]).Label);
        Assert.IsType<LinkButtonProperties>(row[^1]);
    }

    [Fact]
    public void Build_pagination_ids_carry_caption_flag()
    {
        var buttons = Buttons(Message(3, current: 2, showCaption: true));

        var prev = buttons.OfType<ButtonProperties>().Single(b => b.Label == "Prev");
        var next = buttons.OfType<ButtonProperties>().Single(b => b.Label == "Next");
        Assert.Equal("igpage:ABC:1:1", prev.CustomId);
        Assert.Equal("igpage:ABC:3:1", next.CustomId);
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

        Assert.Equal(2, buttons.Count);

        var refresh = Assert.IsType<ButtonProperties>(buttons[0]);
        Assert.Equal("igrefresh:ABC:1:0", refresh.CustomId);
        Assert.Equal("Refresh", refresh.Label);
        Assert.False(refresh.Disabled);

        Assert.IsType<LinkButtonProperties>(buttons[1]);
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
    public void Build_refresh_button_shown_for_single_item()
    {
        var refresh = Buttons(Message(1))
            .OfType<ButtonProperties>()
            .Single(b => b.Label == "Refresh");

        Assert.Equal("igrefresh:ABC:1:0", refresh.CustomId);
    }

    [Fact]
    public void Build_username_header_links_to_profile()
    {
        var text = HeaderText(Message(2));

        Assert.Contains("### [@someone](https://www.instagram.com/someone/)", text.Content);
    }

    [Fact]
    public void Build_omits_caption_by_default()
    {
        var text = HeaderText(Message(2));

        Assert.DoesNotContain("caption", text.Content);
    }

    [Fact]
    public void Build_includes_caption_when_show_caption()
    {
        var text = HeaderText(Message(2, showCaption: true));

        Assert.Contains("caption", text.Content);
    }

    [Fact]
    public void Build_footer_is_below_buttons_and_links_to_repo_once()
    {
        var msg = Message(2);
        var children = Container(msg).Components.ToList();

        var actionRowIndex = children.FindIndex(c => c is ActionRowProperties);
        var footerIndex = children.FindIndex(c => c is TextDisplayProperties t && t.Content == PostMessagePresenter.Footer);

        Assert.True(footerIndex > actionRowIndex);
        Assert.Equal(PostMessagePresenter.Footer, FooterText(msg).Content);
        Assert.DoesNotContain(RepoUrl, HeaderText(msg).Content);
    }

    [Fact]
    public void Build_omits_stats_when_no_counts()
    {
        var text = HeaderText(Message(2));

        Assert.DoesNotContain("likes", text.Content);
        Assert.DoesNotContain("comments", text.Content);
    }

    [Fact]
    public void Build_renders_like_and_comment_counts()
    {
        var msg = Message(2) with { LikeCount = 1234, CommentCount = 56 };

        var text = HeaderText(msg);

        Assert.Contains("1.2k likes", text.Content);
        Assert.Contains("56 comments", text.Content);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1_000, "1k")]
    [InlineData(1_200, "1.2k")]
    [InlineData(12_345, "12.3k")]
    [InlineData(1_000_000, "1M")]
    [InlineData(1_230_000, "1.2M")]
    public void Build_formats_counts_compactly(int count, string expected)
    {
        var msg = Message(1) with { LikeCount = count };

        Assert.Contains($"{expected} likes", HeaderText(msg).Content);
    }
}