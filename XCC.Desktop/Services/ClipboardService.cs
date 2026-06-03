using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using XCC.Services;

namespace XCC.Desktop.Services;

public static class ClipboardService
{
    public static void Register() => ClipboardProvider.Handler = CopyAsync;

    private static async Task CopyAsync(string text)
    {
        var window = (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetValueAsync(DataFormat.Text, text);
    }
}
