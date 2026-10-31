// OpenNotch — original code, MIT licensed. UI state only; clipboard
// history logic lives in OpenNotch.Core (tested, no UI dependency).
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenNotch.Core.Models;
using OpenNotch.Core.Services;

namespace OpenNotch.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ClipboardHistory _history = new();
    private string _searchText = "";
    private ClipboardItem? _selected;
    private bool _isExpanded = true;
    private string _currentTab = "Clipboard";
    private string _filter = "All";
    private string _statusText = "OpenNotch is ready. Copy something, then press Refresh clipboard.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ClipboardItem> VisibleItems { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set { if (_searchText != value) { _searchText = value; OnChanged(); RefreshVisible(); } }
    }

    public ClipboardItem? SelectedItem
    {
        get => _selected;
        set { _selected = value; OnChanged(); OnChanged(nameof(SelectedPreview)); OnChanged(nameof(SelectedMetaText)); OnChanged(nameof(PinButtonText)); }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnChanged(); OnChanged(nameof(ExpandButtonText)); }
    }

    public string ExpandButtonText => IsExpanded ? "Collapse ▲" : "Expand ▼";
    public string PinButtonText => SelectedItem?.Pinned == true ? "Unpin" : "Pin";
    public string SelectedPreview => SelectedItem?.Text ?? "Select an item to preview it here.";
    public string SelectedMetaText => SelectedItem is null ? "" : SelectedItem.MetaText;
    public string Filter
    {
        get => _filter;
        set { if (_filter != value) { _filter = value; OnChanged(); RefreshVisible(); } }
    }
    public string FilterCountsText
    {
        get
        {
            var all = _history.Items.Count;
            var text = _history.Items.Count(i => i.Kind == ClipboardKind.Text);
            var link = _history.Items.Count(i => i.Kind == ClipboardKind.Link);
            var image = _history.Items.Count(i => i.Kind == ClipboardKind.Image);
            return $"All {all}  •  Text {text}  •  Images {image}  •  Links {link}";
        }
    }
    public string CountText => $"Clipboard: {_history.Items.Count} item(s)  •  Pinned: {_history.Items.Count(i => i.Pinned)}";
    public string CurrentTab { get => _currentTab; set { _currentTab = value; OnChanged(); } }
    public string StatusText { get => _statusText; set { _statusText = value; OnChanged(); } }

    public bool AddFromClipboard(string? text)
    {
        var added = _history.Add(text);
        if (added) { RefreshVisible(); StatusText = "Added to clipboard history."; }
        return added;
    }

    public string? CopySelected()
    {
        if (SelectedItem is null) { StatusText = "Select an item first."; return null; }
        StatusText = "Copied back to the clipboard.";
        return SelectedItem.Text;
    }

    public void TogglePinSelected()
    {
        if (SelectedItem is null) return;
        if (SelectedItem.Pinned) _history.Unpin(SelectedItem.Id); else _history.Pin(SelectedItem.Id);
        RefreshVisible(); OnChanged(nameof(PinButtonText));
    }

    public void DeleteSelected()
    {
        if (SelectedItem is null) return;
        _history.Remove(SelectedItem.Id); SelectedItem = null; RefreshVisible();
    }

    public void ClearUnpinned()
    {
        _history.ClearUnpinned(); SelectedItem = null; RefreshVisible();
        StatusText = "Cleared unpinned items. Pinned items are kept.";
    }

    public void RefreshVisible()
    {
        ClipboardKind? kind = Filter switch
        {
            "Text" => ClipboardKind.Text,
            "Link" => ClipboardKind.Link,
            "Image" => ClipboardKind.Image,
            _ => null,
        };
        var results = _history.Search(SearchText, kind);
        if (Filter == "Image" && results.Count == 0)
            StatusText = "Images are not stored yet — coming in Step 2. Text and Links are working now.";
        VisibleItems.Clear();
        foreach (var item in results) VisibleItems.Add(item);
        if (SelectedItem is not null && !VisibleItems.Contains(SelectedItem)) SelectedItem = null;
        OnChanged(nameof(CountText));
        OnChanged(nameof(FilterCountsText));
    }

    private void OnChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
