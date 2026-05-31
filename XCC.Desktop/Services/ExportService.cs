using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using XCC.Models;
using XCC.Services;

namespace XCC.Desktop.Services;

public static class ExportService
{
    public static void Register() => ExportProvider.Handler = ExportAsync;

    private static async Task<string?> ExportAsync(RaceSession session)
    {
        var bytes = await Task.Run(() => XlsxGenerator.Generate(session));
        var fileName = XlsxGenerator.FileName(session);

        var dir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "XCC");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, fileName);

        await File.WriteAllBytesAsync(filePath, bytes);
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        return $"Sauvegardé : {fileName}";
    }
}
