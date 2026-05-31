using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using XCC.Models;
using XCC.Services;

namespace XCC.Browser.Services;

public static partial class BrowserExportService
{
    [JSImport("globalThis.xccDownloadXlsx")]
    private static partial void JsDownload(string filename, byte[] content);

    public static void Register() => ExportProvider.Handler = ExportAsync;

    private static async Task<string?> ExportAsync(RaceSession session)
    {
        var bytes = await Task.Run(() => XlsxGenerator.Generate(session));
        var fileName = XlsxGenerator.FileName(session);
        JsDownload(fileName, bytes);
        return "Téléchargement démarré";
    }
}
