using System;
using Avalonia;
using XCC.Desktop.Services;

namespace XCC.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ExportService.Register();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
