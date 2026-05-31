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

        int N = session.Entries.Select(e => e.PilotNumber).Distinct().Count();
        int M = session.Entries.Count;

        // Classement always reserves 30 rows so historique sits at a fixed offset,
        // and all pilot/rank cells are driven by formulas referencing the history.
        const int MaxClassementRows = 30;

        // ── Row layout ───────────────────────────────────────────────────────
        const int titleRow     = 1;
        const int metaRow      = 2;
        const int durRow       = 3;
        const int secResRow    = 5;
        const int resHeaderRow = 6;
        const int resDataStart = 7;
        const int resDataEnd   = resDataStart + MaxClassementRows - 1; // row 36
        const int secHistRow   = resDataEnd + 2;                       // row 38
        const int histHeaderRow = secHistRow + 1;                      // row 39
        const int histDataStart = histHeaderRow + 1;                   // row 40
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

        // All 30 rows are formula-driven. Pilot numbers come from the history's last
        // turn via COUNTIFS rank matching so correcting any history cell propagates
        // automatically to the classement.
        for (int i = 0; i < MaxClassementRows; i++)
        {
            int row = resDataStart + i;
            int k   = i + 1; // 1-based rank

            if (M > 0)
            {
                // Position: blank when no pilot at this rank
                ws.Cell(row, 1).FormulaA1 = $"IF(B{row}=\"\",\"\",{k})";

                // Pilot: kth pilot to finish the last turn (ascending by timestamp).
                // COUNTIFS counts, for each history row, how many last-turn entries
                // finished at or before that row's time — giving each row its rank.
                // SUMPRODUCT then picks the pilot whose rank equals k.
                // Uses only SUMPRODUCT/COUNTIFS so ClosedXML never inserts @.
                ws.Cell(row, 2).FormulaA1 =
                    $"IFERROR(SUMPRODUCT(" +
                    $"($C${histDataStart}:$C${histDataEnd}=MAX($C${histDataStart}:$C${histDataEnd}))" +
                    $"*(COUNTIFS($C${histDataStart}:$C${histDataEnd},MAX($C${histDataStart}:$C${histDataEnd})," +
                    $"$B${histDataStart}:$B${histDataEnd},\"<=\"&$B${histDataStart}:$B${histDataEnd})={k})" +
                    $"*$A${histDataStart}:$A${histDataEnd}),\"\")";

                // Tours: max turn reached by this pilot
                ws.Cell(row, 3).FormulaA1 =
                    $"IF(B{row}=\"\",\"\",IFERROR(SUMPRODUCT(MAX(" +
                    $"($A${histDataStart}:$A${histDataEnd}=B{row})" +
                    $"*$C${histDataStart}:$C${histDataEnd})),0))";

                // Dernier passage: latest timestamp for this pilot at their max turn
                ws.Cell(row, 4).FormulaA1 =
                    $"IF(B{row}=\"\",\"\",SUMPRODUCT(MAX(" +
                    $"($A${histDataStart}:$A${histDataEnd}=B{row})" +
                    $"*($C${histDataStart}:$C${histDataEnd}=C{row})" +
                    $"*$B${histDataStart}:$B${histDataEnd})))";
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
        WriteSectionHeader(ws, secHistRow, 1, 5, "HISTORIQUE");

        ws.Cell(histHeaderRow, 1).Value = "NUMÉRO PILOTE";
        ws.Cell(histHeaderRow, 2).Value = "HEURE";
        ws.Cell(histHeaderRow, 3).Value = "TOUR";
        ws.Cell(histHeaderRow, 4).Value = "POSITION";
        ws.Cell(histHeaderRow, 5).Value = "TEMPS AU TOUR";
        StyleHeader(ws.Range(histHeaderRow, 1, histHeaderRow, 5));

        var pilotHistory = session.Entries
            .GroupBy(e => e.PilotNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

        // Running position counter per turn (entries are already in finish order)
        var positionPerTurn = new Dictionary<int, int>();

        for (int i = 0; i < M; i++)
        {
            int row = histDataStart + i;
            var entry = session.Entries[i];
            var history = pilotHistory[entry.PilotNumber];
            var idx = history.IndexOf(entry);
            var lapStart = idx > 0 ? history[idx - 1].Timestamp : session.StartTime;
            var lap = entry.Timestamp - lapStart;

            positionPerTurn.TryGetValue(entry.Turn, out var pos);
            positionPerTurn[entry.Turn] = ++pos;

            SetPilot(ws.Cell(row, 1), entry.PilotNumber);

            // Real DateTime value so SUMPRODUCT(MAX(...)) comparisons work correctly
            ws.Cell(row, 2).Value = entry.Timestamp;
            ws.Cell(row, 2).Style.NumberFormat.Format = "HH:mm:ss";

            ws.Cell(row, 3).Value = entry.Turn;
            ws.Cell(row, 4).Value = pos;

            // Fraction-of-day so Excel formats it as [mm]:ss
            ws.Cell(row, 5).Value = lap.TotalDays;
            ws.Cell(row, 5).Style.NumberFormat.Format = "[mm]:ss";

            if (row % 2 == 0)
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = RowAlt;
        }

        // ── Column widths ────────────────────────────────────────────────────
        ws.Column(1).Width = 18; // numéro pilote
        ws.Column(2).Width = 12; // heure
        ws.Column(3).Width = 8;  // tour
        ws.Column(4).Width = 12; // position
        ws.Column(5).Width = 16; // temps au tour
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

    private static void SetPilot(IXLCell cell, string pilotNumber)
    {
        if (int.TryParse(pilotNumber, out var n)) cell.Value = n;
        else cell.Value = pilotNumber;
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
