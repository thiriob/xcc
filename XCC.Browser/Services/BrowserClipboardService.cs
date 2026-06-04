using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using XCC.Services;

namespace XCC.Browser.Services;

public static partial class BrowserClipboardService
{
    [JSImport("globalThis.navigator.clipboard.writeText")]
    private static partial Task JsWriteText(string text);

    public static void Register() => ClipboardProvider.Handler = CopyAsync;

    private static Task CopyAsync(string text) => JsWriteText(text);
}
