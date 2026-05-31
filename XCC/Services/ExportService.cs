using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using XCC.Models;

namespace XCC.Services;

public static class ExportService
{
    private static readonly XLColor HeaderBg    = XLColor.FromHtml("#1E293B");
    private static readonly XLColor RowAlt      = XLColor.FromHtml("#F1F5F9");
    private static readonly XLColor[] Podium    =
    [
        XLColor.FromHtml("#FFD700"), // 1st — gold
        XLColor.FromHtml("#C0C0C0"), // 2nd — silver
        XLColor.FromHtml("#CD7F32"), // 3rd — bronze
    ];

    public static string Export(RaceSession session)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XCC");
        Directory.CreateDirectory(dir);

        var fileName = $"{Sanitize(session.RoundName)}_{session.StartTime:yyyy-MM-dd_HH-mm}.xlsx";
        var filePath = Path.Combine(dir, fileName);

        using var wb = new XLWorkbook();
        AddResultsSheet(wb, session);
        AddHistoriqueSheet(wb, session);
        wb.SaveAs(filePath);

        return filePath;
    }

    // ── Sheet 1 : final standings ────────────────────────────────────────────

    private static void AddResultsSheet(XLWorkbook wb, RaceSession session)
    {
        var ws = wb.AddWorksheet("Résultats");

        // Title block
        var titleCell = ws.Cell("A1");
        titleCell.Value = session.RoundName;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 18;

        var finishTime = session.FinishTime ?? DateTime.Now;
        ws.Cell("A2").Value = session.StartTime.ToString("dd/MM/yyyy");
        ws.Cell("B2").Value = $"{session.StartTime:HH:mm} → {finishTime:HH:mm}";

        var elapsed = session.Elapsed;
        ws.Cell("A3").Value = $"Durée : {(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        ws.Cell("B3").Value = $"Pilotes : {session.Entries.Select(e => e.PilotNumber).Distinct().Count()}";

        // Table header
        const int hRow = 5;
        ws.Cell(hRow, 1).Value = "POSITION";
        ws.Cell(hRow, 2).Value = "NUMÉRO PILOTE";
        ws.Cell(hRow, 3).Value = "TOURS";
        ws.Cell(hRow, 4).Value = "HEURE DERNIER PASSAGE";
        StyleHeader(ws.Range(hRow, 1, hRow, 4));

        // Data rows
        var standings = ComputeStandings(session);
        for (int i = 0; i < standings.Count; i++)
        {
            int row = hRow + 1 + i;
            var s = standings[i];
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = s.PilotNumber;
            ws.Cell(row, 3).Value = s.MaxTurn;
            ws.Cell(row, 4).Value = s.LastTimestamp.ToString("HH:mm:ss");

            var range = ws.Range(row, 1, row, 4);
            if (i < 3)
            {
                range.Style.Fill.BackgroundColor = Podium[i];
                range.Style.Font.Bold = true;
                range.Style.Font.FontColor = XLColor.FromHtml("#1E293B");
            }
            else if (row % 2 == 0)
            {
                range.Style.Fill.BackgroundColor = RowAlt;
            }
        }

        ws.Column(1).Width = 12;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 10;
        ws.Column(4).Width = 26;
    }

    // ── Sheet 2 : full entry log ─────────────────────────────────────────────

    private static void AddHistoriqueSheet(XLWorkbook wb, RaceSession session)
    {
        var ws = wb.AddWorksheet("Historique");

        ws.Cell(1, 1).Value = "#";
        ws.Cell(1, 2).Value = "NUMÉRO PILOTE";
        ws.Cell(1, 3).Value = "HEURE";
        ws.Cell(1, 4).Value = "TOUR";
        ws.Cell(1, 5).Value = "TEMPS AU TOUR";
        StyleHeader(ws.Range(1, 1, 1, 5));

        // Precompute per-pilot ordered history for lap time calculation
        var pilotHistory = session.Entries
            .GroupBy(e => e.PilotNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

        int row = 2;
        foreach (var entry in session.Entries)
        {
            var history = pilotHistory[entry.PilotNumber];
            var idx = history.IndexOf(entry);
            var lapStart = idx > 0 ? history[idx - 1].Timestamp : session.StartTime;
            var lap = entry.Timestamp - lapStart;

            ws.Cell(row, 1).Value = row - 1;
            ws.Cell(row, 2).Value = entry.PilotNumber;
            ws.Cell(row, 3).Value = entry.Timestamp.ToString("HH:mm:ss");
            ws.Cell(row, 4).Value = entry.Turn;
            ws.Cell(row, 5).Value = $"{(int)lap.TotalMinutes:D2}:{lap.Seconds:D2}";

            if (row % 2 == 0)
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = RowAlt;

            row++;
        }

        ws.Column(1).Width = 6;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 8;
        ws.Column(5).Width = 16;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = 11;
        range.Style.Fill.BackgroundColor = HeaderBg;
        range.Style.Font.FontColor = XLColor.White;
    }

    private record Standing(string PilotNumber, int MaxTurn, DateTime LastTimestamp);

    private static List<Standing> ComputeStandings(RaceSession session) =>
        session.Entries
            .GroupBy(e => e.PilotNumber)
            .Select(g =>
            {
                var maxTurn = g.Max(e => e.Turn);
                var last = g.Where(e => e.Turn == maxTurn).Max(e => e.Timestamp);
                return new Standing(g.Key, maxTurn, last);
            })
            .OrderByDescending(s => s.MaxTurn)
            .ThenBy(s => s.LastTimestamp)
            .ToList();

    private static string Sanitize(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
