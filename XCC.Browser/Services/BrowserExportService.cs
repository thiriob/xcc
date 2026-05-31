using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using XCC.Models;
using XCC.Services;

namespace XCC.Browser.Services;

public static partial class BrowserExportService
{
    [JSImport("globalThis.xccDownload")]
    private static partial void JsDownload(string filename, string content);

    public static void Register() => ExportProvider.Handler = ExportAsync;

    private static Task<string?> ExportAsync(RaceSession session)
    {
        var csv = GenerateCsv(session);
        var filename = $"{session.RoundName}_{session.StartTime:yyyy-MM-dd_HH-mm}.csv";
        JsDownload(filename, csv);
        return Task.FromResult<string?>("Téléchargement démarré");
    }

    private static string GenerateCsv(RaceSession session)
    {
        var sb = new StringBuilder();
        var elapsed = session.Elapsed;
        var finishTime = session.FinishTime ?? DateTime.Now;

        sb.AppendLine($"ROUND;{session.RoundName}");
        sb.AppendLine($"DATE;{session.StartTime:dd/MM/yyyy}");
        sb.AppendLine($"DEBUT;{session.StartTime:HH:mm}");
        sb.AppendLine($"FIN;{finishTime:HH:mm}");
        sb.AppendLine($"DUREE;{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}");
        sb.AppendLine($"PILOTES;{session.Entries.Select(e => e.PilotNumber).Distinct().Count()}");
        sb.AppendLine();

        // Classement
        sb.AppendLine("CLASSEMENT");
        sb.AppendLine("POSITION;NUMERO PILOTE;TOURS;DERNIER PASSAGE");

        var standings = session.Entries
            .GroupBy(e => e.PilotNumber)
            .Select(g =>
            {
                var maxTurn = g.Max(e => e.Turn);
                var last = g.Where(e => e.Turn == maxTurn).Max(e => e.Timestamp);
                return (PilotNumber: g.Key, MaxTurn: maxTurn, LastTimestamp: last);
            })
            .OrderByDescending(s => s.MaxTurn)
            .ThenBy(s => s.LastTimestamp);

        int rank = 1;
        foreach (var s in standings)
            sb.AppendLine($"{rank++};{s.PilotNumber};{s.MaxTurn};{s.LastTimestamp:HH:mm:ss}");

        sb.AppendLine();

        // Historique
        sb.AppendLine("HISTORIQUE");
        sb.AppendLine("NUMERO PILOTE;HEURE;TOUR;POSITION;TEMPS AU TOUR");

        var pilotHistory = session.Entries
            .GroupBy(e => e.PilotNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

        var positionPerTurn = new Dictionary<int, int>();

        foreach (var entry in session.Entries)
        {
            var history = pilotHistory[entry.PilotNumber];
            var idx = history.IndexOf(entry);
            var lapStart = idx > 0 ? history[idx - 1].Timestamp : session.StartTime;
            var lap = entry.Timestamp - lapStart;

            positionPerTurn.TryGetValue(entry.Turn, out var pos);
            positionPerTurn[entry.Turn] = ++pos;

            sb.AppendLine($"{entry.PilotNumber};{entry.Timestamp:HH:mm:ss};{entry.Turn};{pos};{(int)lap.TotalMinutes:D2}:{lap.Seconds:D2}");
        }

        return sb.ToString();
    }
}
