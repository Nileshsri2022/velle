// OpenInstaller — original code, MIT licensed.
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenInstaller.Services;
using OpenInstaller.ViewModels;

namespace OpenInstaller;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var winget = new WingetService();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(winget),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
