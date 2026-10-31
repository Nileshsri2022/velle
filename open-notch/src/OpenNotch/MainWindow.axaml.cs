// OpenNotch — original code, MIT licensed.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OpenNotch.ViewModels;

namespace OpenNotch;

public partial class MainWindow : Window
{
    private DispatcherTimer? _pollTimer;
    private string? _lastSetByUs;
    private bool _polling;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PositionTopCentre();
    }

    private void PositionTopCentre()
    {
        try
        {
            var screen = Screens.Primary;
            if (screen is null) return;
            var area = screen.WorkingArea;
            Position = new PixelPoint(area.X + (area.Width - (int)Width) / 2, area.Y + 8);
        }
        catch { /* v1: centre-screen fallback is fine */ }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += async (_, _) => await PollClipboardAsync();
        _pollTimer.Start();
        if (DataContext is MainViewModel vm) vm.RefreshVisible();
    }

    private async Task PollClipboardAsync()
    {
        if (_polling || DataContext is not MainViewModel vm) return;
        _polling = true;
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null) return;
            var text = await clipboard.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text)) return;
            if (string.Equals(text, _lastSetByUs, StringComparison.Ordinal)) return;
            vm.AddFromClipboard(text);
        }
        catch { /* clipboard busy: try again next tick */ }
        finally { _polling = false; }
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnExpandClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) Vm.IsExpanded = !Vm.IsExpanded;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private async void OnRefreshClick(object? sender, RoutedEventArgs e) => await PollClipboardAsync();

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        var text = Vm?.CopySelected();
        if (text is null || Vm is null) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;
        _lastSetByUs = text;
        await clipboard.SetTextAsync(text);
        // Like in the video: show the toast, then collapse the panel.
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (_, _) => { timer.Stop(); Vm.ClearToastAndCollapse(); };
        timer.Start();
    }

    private void OnOpenLinkClick(object? sender, RoutedEventArgs e)
    {
        var text = Vm?.SelectedItem?.Text;
        if (string.IsNullOrWhiteSpace(text) || Vm is null) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = text.Trim(),
                UseShellExecute = true,
            });
            Vm.StatusText = "Opened link in your browser.";
        }
        catch (Exception ex)
        {
            Vm.StatusText = "Could not open link: " + ex.Message;
        }
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e) => Vm?.ShowSettingsNote();

    private void OnPinClick(object? sender, RoutedEventArgs e) => Vm?.TogglePinSelected();
    private void OnFilterAllClick(object? sender, RoutedEventArgs e) { if (Vm is not null) Vm.Filter = "All"; }
    private void OnFilterTextClick(object? sender, RoutedEventArgs e) { if (Vm is not null) Vm.Filter = "Text"; }
    private void OnFilterImagesClick(object? sender, RoutedEventArgs e) { if (Vm is not null) Vm.Filter = "Image"; }
    private void OnFilterLinksClick(object? sender, RoutedEventArgs e) { if (Vm is not null) Vm.Filter = "Link"; }
    private void OnDeleteClick(object? sender, RoutedEventArgs e) => Vm?.DeleteSelected();
    private void OnClearClick(object? sender, RoutedEventArgs e) => Vm?.ClearUnpinned();
}
