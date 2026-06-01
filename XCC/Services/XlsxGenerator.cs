using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using XCC.Models;

namespace XCC.Services;

public static class XlsxGenerator
{
    private static readonly XLColor HeaderBg  = XLColor.FromHtml("#1E293B");
    private static readonly XLColor SectionBg = XLColor.FromHtml("#334155");
    private static readonly XLColor RowAlt    = XLColor.FromHtml("#F1F5F9");
    private static readonly XLColor[] Podium  =
    [
        XLColor.FromHtml("#FFD700"),
        XLColor.FromHtml("#C0C0C0"),
        XLColor.FromHtml("#CD7F32"),
    ];

    public static byte[] Generate(RaceSession session)
    {
        using var wb = new XLWorkbook();
        AddSheet(wb, session);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static string FileName(RaceSession session) =>
        $"{Sanitize(session.RoundName)}_{session.StartTime:yyyy-MM-dd_HH-mm}.xlsx";

    private static void AddSheet(XLWorkbook wb, RaceSession session)
    {
        var ws = wb.AddWorksheet("Résultats");

        int N = session.Entries.Select(e => e.PilotNumber).Distinct().Count();
        int M = session.Entries.Count;

        const int MaxClassementRows = 30;
        const int titleRow      = 1;
        const int metaRow       = 2;
        const int durRow        = 3;
        const int secResRow     = 5;
        const int resHeaderRow  = 6;
        const int resDataStart  = 7;
        const int resDataEnd    = resDataStart + MaxClassementRows - 1;
        const int secHistRow    = resDataEnd + 2;
        const int histHeaderRow = secHistRow + 1;
        const int histDataStart = histHeaderRow + 1;
        int histDataEnd         = histDataStart + M - 1;

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

        // Column range references for the history data area
        string Ah = $"$A${histDataStart}:$A${histDataEnd}";
        string Bh = $"$B${histDataStart}:$B${histDataEnd}";
        string Ch = $"$C${histDataStart}:$C${histDataEnd}";
        // Column F (score): non-blank only for each pilot's real last-turn row.
        // Score = turn - MOD(time,1). More turns wins; within same turns, earlier time wins.
        // Finalization entries share the same turn number but have a later time so they score
        // lower — LARGE naturally picks the real crossing over any finalization duplicate.
        string Fh = $"$F${histDataStart}:$F${histDataEnd}";

        for (int i = 0; i < MaxClassementRows; i++)
        {
            int row = resDataStart + i;
            int k   = i + 1;

            if (M > 0)
            {
                ws.Cell(row, 1).FormulaA1 = $"IF(B{row}=\"\",\"\",{k})";

                // Rank k pilot: pilot whose score (col F) equals the k-th largest score.
                // Pure SUMPRODUCT — ClosedXML never adds @ to ranges inside SUMPRODUCT.
                // Requires numeric pilot numbers (standard for XCC bibs).
                ws.Cell(row, 2).FormulaA1 =
                    $"IFERROR(SUMPRODUCT(({Fh}=LARGE({Fh},{k}))*{Ah}),\"\")";

                ws.Cell(row, 3).FormulaA1 =
                    $"IF(B{row}=\"\",\"\",IFERROR(SUMPRODUCT(MAX(({Ah}=B{row})*{Ch})),0))";

                // Real crossing time: ISNUMBER(Fh) flags representative rows (score is numeric
                // there, "" elsewhere), so finalization entries are excluded from the lookup.
                ws.Cell(row, 4).FormulaA1 =
                    $"IF(B{row}=\"\",\"\",SUMPRODUCT(({Ah}=B{row})*ISNUMBER({Fh})*({Ch}=C{row})*{Bh}))";
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
        WriteSectionHeader(ws, secHistRow, 1, 7, "HISTORIQUE");

        ws.Cell(histHeaderRow, 1).Value = "NUMÉRO PILOTE";
        ws.Cell(histHeaderRow, 2).Value = "HEURE";
        ws.Cell(histHeaderRow, 3).Value = "TOUR";
        ws.Cell(histHeaderRow, 4).Value = "POSITION";
        ws.Cell(histHeaderRow, 5).Value = "TEMPS AU TOUR";
        ws.Cell(histHeaderRow, 6).Value = "SCORE";
        ws.Cell(histHeaderRow, 7).Value = "DERNIERE BOUCLE";
        StyleHeader(ws.Range(histHeaderRow, 1, histHeaderRow, 7));

        var pilotHistory = session.Entries
            .GroupBy(e => e.PilotNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

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

            ws.Cell(row, 2).Value = entry.Timestamp;
            ws.Cell(row, 2).Style.NumberFormat.Format = "HH:mm:ss";

            ws.Cell(row, 3).Value = entry.Turn;
            ws.Cell(row, 4).Value = pos;

            ws.Cell(row, 5).Value = lap.TotalDays;
            ws.Cell(row, 5).Style.NumberFormat.Format = "[mm]:ss";

            // F: score = turn - time-of-day fraction; blank when this is not the pilot's last turn.
            // Non-last-turn rows score "" so LARGE skips them entirely.
            // SUMPRODUCT(MAX()) used instead of MAXIFS to avoid ClosedXML adding the @ operator.
            string maxTurnForPilot = $"SUMPRODUCT(MAX(({Ah}=A{row})*{Ch}))";
            ws.Cell(row, 6).FormulaA1 =
                $"IF(C{row}={maxTurnForPilot},C{row}-MOD(B{row},1),\"\")";

            // G: 1 if this row is the pilot's last (or equal-last) turn, else 0
            ws.Cell(row, 7).FormulaA1 =
                $"(C{row}={maxTurnForPilot})*1";

            if (row % 2 == 0)
                ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = RowAlt;
        }

        ws.Column(1).Width = 18;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 8;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 16;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 15;
    }

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
