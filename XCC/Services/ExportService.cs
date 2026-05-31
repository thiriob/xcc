using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using XCC.Models;

namespace XCC.Services;

public static class ExportService
{
    private static readonly XLColor HeaderBg  = XLColor.FromHtml("#1E293B");
    private static readonly XLColor SectionBg = XLColor.FromHtml("#334155");
    private static readonly XLColor RowAlt    = XLColor.FromHtml("#F1F5F9");
    private static readonly XLColor[] Podium  =
    [
        XLColor.FromHtml("#FFD700"), // 1st
        XLColor.FromHtml("#C0C0C0"), // 2nd
        XLColor.FromHtml("#CD7F32"), // 3rd
    ];

    public static string Export(RaceSession session)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XCC");
        Directory.CreateDirectory(dir);

        var fileName = $"{Sanitize(session.RoundName)}_{session.StartTime:yyyy-MM-dd_HH-mm}.xlsx";
        var filePath = Path.Combine(dir, fileName);

        using var wb = new XLWorkbook();
        AddSheet(wb, session);
        wb.SaveAs(filePath);

        return filePath;
    }

    private static void AddSheet(XLWorkbook wb, RaceSession session)
    {
        var ws = wb.AddWorksheet("Résultats");

        var standings = ComputeStandings(session);
        int N = standings.Count;
        int M = session.Entries.Count;

        // ── Row layout ───────────────────────────────────────────────────────
        const int titleRow     = 1;
        const int metaRow      = 2;
        const int durRow       = 3;
        const int secResRow    = 5;
        const int resHeaderRow = 6;
        int resDataStart       = 7;
        int resDataEnd         = resDataStart + Math.Max(N - 1, 0);
        int secHistRow         = resDataEnd + 2;
        int histHeaderRow      = secHistRow + 1;
        int histDataStart      = histHeaderRow + 1;
        int histDataEnd        = histDataStart + M - 1;

        // ── Title block ──────────────────────────────────────────────────────
        ws.Cell(titleRow, 1).Value = session.RoundName;
        ws.Cell(titleRow, 1).Style.Font.Bold = true;
        ws.Cell(titleRow, 1).Style.Font.FontSize = 18;
        ws.Range(titleRow, 1, titleRow, 5).Merge();

        var finishTime = session.FinishTime ?? DateTime.Now;
        ws.Cell(metaRow, 1).Value = session.StartTime.ToString("dd/MM/yyyy");
        ws.Cell(metaRow, 2).Value = $"{session.StartTime:HH:mm} → {finishTime:HH:mm}";

        var elapsed = session.Elapsed;
        ws.Cell(durRow, 1).Value = $"Durée : {(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        ws.Cell(durRow, 2).Value = $"Pilotes : {N}";

        // ── Classement section ───────────────────────────────────────────────
        WriteSectionHeader(ws, secResRow, 1, 4, "CLASSEMENT");

        ws.Cell(resHeaderRow, 1).Value = "POSITION";
        ws.Cell(resHeaderRow, 2).Value = "NUMÉRO PILOTE";
        ws.Cell(resHeaderRow, 3).Value = "TOURS";
        ws.Cell(resHeaderRow, 4).Value = "DERNIER PASSAGE";
        StyleHeader(ws.Range(resHeaderRow, 1, resHeaderRow, 4));

        for (int i = 0; i < N; i++)
        {
            int row = resDataStart + i;
            ws.Cell(row, 1).Value = i + 1;
            SetPilot(ws.Cell(row, 2), standings[i].PilotNumber);

            if (M > 0)
            {
                // SUMPRODUCT(MAX(...)) forces array evaluation without dynamic-array
                // functions, so ClosedXML never adds the @ implicit-intersection prefix.

                // Tours: max turn in history for this pilot (col C = tour)
                ws.Cell(row, 3).FormulaA1 =
                    $"IFERROR(SUMPRODUCT(MAX(($A${histDataStart}:$A${histDataEnd}=B{row})" +
                    $"*$C${histDataStart}:$C${histDataEnd})),0)";

                // Dernier passage: latest timestamp for this pilot at their max turn (col B = heure)
                ws.Cell(row, 4).FormulaA1 =
                    $"SUMPRODUCT(MAX(($A${histDataStart}:$A${histDataEnd}=B{row})" +
                    $"*($C${histDataStart}:$C${histDataEnd}=C{row})" +
                    $"*$B${histDataStart}:$B${histDataEnd}))";
                ws.Cell(row, 4).Style.NumberFormat.Format = "HH:mm:ss";
            }

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

        // ── Historique section ───────────────────────────────────────────────
        WriteSectionHeader(ws, secHistRow, 1, 4, "HISTORIQUE");

        ws.Cell(histHeaderRow, 1).Value = "NUMÉRO PILOTE";
        ws.Cell(histHeaderRow, 2).Value = "HEURE";
        ws.Cell(histHeaderRow, 3).Value = "TOUR";
        ws.Cell(histHeaderRow, 4).Value = "TEMPS AU TOUR";
        StyleHeader(ws.Range(histHeaderRow, 1, histHeaderRow, 4));

        var pilotHistory = session.Entries
            .GroupBy(e => e.PilotNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

        for (int i = 0; i < M; i++)
        {
            int row = histDataStart + i;
            var entry = session.Entries[i];
            var history = pilotHistory[entry.PilotNumber];
            var idx = history.IndexOf(entry);
            var lapStart = idx > 0 ? history[idx - 1].Timestamp : session.StartTime;
            var lap = entry.Timestamp - lapStart;

            SetPilot(ws.Cell(row, 1), entry.PilotNumber);

            // Real DateTime value so SUMPRODUCT(MAX(...)) comparisons work correctly
            ws.Cell(row, 2).Value = entry.Timestamp;
            ws.Cell(row, 2).Style.NumberFormat.Format = "HH:mm:ss";

            ws.Cell(row, 3).Value = entry.Turn;

            // Fraction-of-day so Excel formats it as [mm]:ss
            ws.Cell(row, 4).Value = lap.TotalDays;
            ws.Cell(row, 4).Style.NumberFormat.Format = "[mm]:ss";

            if (row % 2 == 0)
                ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = RowAlt;
        }

        // ── Column widths ────────────────────────────────────────────────────
        ws.Column(1).Width = 18; // pilot number / position
        ws.Column(2).Width = 14; // heure / tours
        ws.Column(3).Width = 10; // tour / dernier passage
        ws.Column(4).Width = 22; // temps au tour / dernier passage
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void WriteSectionHeader(IXLWorksheet ws, int row, int colFrom, int colTo, string label)
    {
        ws.Range(row, colFrom, row, colTo).Merge();
        ws.Cell(row, colFrom).Value = label;
        ws.Cell(row, colFrom).Style.Font.Bold = true;
        ws.Cell(row, colFrom).Style.Font.FontColor = XLColor.White;
        ws.Cell(row, colFrom).Style.Fill.BackgroundColor = SectionBg;
    }

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

    private static void SetPilot(IXLCell cell, string pilotNumber)
    {
        if (int.TryParse(pilotNumber, out var n)) cell.Value = n;
        else cell.Value = pilotNumber;
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
