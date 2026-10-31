// OpenInstaller — original code, MIT licensed.
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenInstaller.ViewModels;

namespace OpenInstaller;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (ViewModel is not null)
        {
            _ = ViewModel.InitializeAsync();
        }
    }

    private async void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.SearchAsync();
        }
    }

    private async void OnInstallClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.InstallSelectedAsync();
        }
    }

    private async void OnUpdateClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.UpdateSelectedAsync();
        }
    }

    private async void OnLaunchClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.LaunchSelectedAsync();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.CancelInstall();
    }

    private async void OnRetryClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.RetryAsync();
        }
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.CheckSelectedAsync();
        }
    }
}
