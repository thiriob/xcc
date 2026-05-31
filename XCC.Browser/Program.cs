using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using XCC;
using XCC.Browser.Services;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        BrowserExportService.Register();
        return BuildAvaloniaApp()
            .WithInterFont()
#if DEBUG
            .WithDeveloperTools()
#endif
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
