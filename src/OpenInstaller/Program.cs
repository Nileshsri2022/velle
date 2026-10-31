// OpenInstaller — original code, MIT licensed.
// Entry point: a plain Avalonia desktop application. No code, assets, or
// branding from any other installer application is used here.
using Avalonia;

namespace OpenInstaller;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
