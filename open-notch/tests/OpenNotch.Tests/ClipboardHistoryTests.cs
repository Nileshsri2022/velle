// OpenNotch — original code, MIT licensed. TDD tests for v1 clipboard core.
using OpenNotch.Core.Models;
using OpenNotch.Core.Services;
using Xunit;

namespace OpenNotch.Tests;

public class ClipboardHistoryTests
{
    [Fact]
    public void Add_IgnoresNullAndWhitespace()
    {
        var h = new ClipboardHistory();
        h.Add(null); h.Add(""); h.Add("   ");
        Assert.Empty(h.Items);
    }

    [Fact]
    public void Add_IgnoresConsecutiveDuplicate()
    {
        var h = new ClipboardHistory();
        h.Add("hello"); h.Add("hello");
        Assert.Single(h.Items);
    }

    [Fact]
    public void Add_NewestFirst_AndSearchIsCaseInsensitive()
    {
        var h = new ClipboardHistory();
        h.Add("first"); h.Add("Second Thing");
        Assert.Equal("Second Thing", h.Items[0].Text);
        Assert.Single(h.Search("second"));
        Assert.Single(h.Search("SECOND"));
    }

    [Fact]
    public void Cap_TrimsOldestUnpinned_AndKeepsPinned()
    {
        var h = new ClipboardHistory(maxItems: 3);
        h.Add("a"); h.Add("b");
        var pinned = h.Items[1]; // "a", the oldest
        h.Pin(pinned.Id);
        h.Add("c"); h.Add("d");
        // 3 items max, pinned "a" survives, oldest unpinned "b" is trimmed.
        Assert.Equal(3, h.Items.Count);
        Assert.Contains(h.Items, i => i.Text == "a" && i.Pinned);
        Assert.DoesNotContain(h.Items, i => i.Text == "b");
    }

    [Fact]
    public void Search_PinnedFirst_ThenNewest()
    {
        var h = new ClipboardHistory();
        h.Add("apple pie"); h.Add("apple tart");
        h.Pin(h.Items[1].Id); // "apple pie"
        var results = h.Search("apple");
        Assert.Equal("apple pie", results[0].Text);
    }

    [Fact]
    public void Remove_AndClearUnpinned_KeepPinned()
    {
        var h = new ClipboardHistory();
        h.Add("keep"); h.Add("drop");
        h.Pin(h.Items[1].Id);
        h.Remove(h.Items[0].Id);
        Assert.Single(h.Items);
        h.ClearUnpinned();
        Assert.Single(h.Items);
        Assert.Equal("keep", h.Items[0].Text);
        h.ClearAll();
        Assert.Empty(h.Items);
    }

    [Fact]
    public void Pin_Unpin_Toggles()
    {
        var h = new ClipboardHistory();
        h.Add("x");
        var id = h.Items[0].Id;
        h.Pin(id); Assert.True(h.Items[0].Pinned);
        h.Unpin(id); Assert.False(h.Items[0].Pinned);
    }
}
