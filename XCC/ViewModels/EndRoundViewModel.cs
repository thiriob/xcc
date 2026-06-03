using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XCC.Models;
using XCC.Services;

namespace XCC.ViewModels;

public partial class EndRoundViewModel : ViewModelBase
{
    [ObservableProperty] private string _exportStatus = "";

    public string RoundName { get; }
    public string HeureFinDisplay { get; }
    public string NombrePilotesDisplay { get; }
    public string ToursDisplay { get; }
    public string TempsTotalDisplay { get; }
    public IReadOnlyList<PlacementEntry> Standings { get; }

    private readonly Action _onNouveauRound;
    private readonly RaceSession _session;

    public EndRoundViewModel(RaceSession session, Action onNouveauRound)
    {
        _session = session;
        _onNouveauRound = onNouveauRound;

        var elapsed = session.Elapsed;
        var finishTime = session.FinishTime ?? DateTime.Now;
        var maxTurn = session.Entries.Count > 0 ? session.Entries.Max(e => e.Turn) : 0;

        RoundName = session.RoundName;
        HeureFinDisplay = $"Fin à {finishTime:HH:mm}";
        TempsTotalDisplay = $"Durée : {(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        ToursDisplay = $"Tours : {maxTurn}/{session.TurnsMax}";

        Standings = session.Entries
            .GroupBy(e => e.PilotNumber)
            .Select(g =>
            {
                var mt = g.Max(e => e.Turn);
                var last = g.Where(e => e.Turn == mt).Min(e => e.Timestamp);
                return (PilotNumber: g.Key, MaxTurn: mt, Last: last);
            })
            .OrderByDescending(s => s.MaxTurn)
            .ThenBy(s => s.Last)
            .Select((s, i) =>
            {
                var chrono = s.Last - session.StartTime;
                var chronoDisplay = $"{(int)chrono.TotalMinutes:D2}:{chrono.Seconds:D2}";
                return new PlacementEntry(i + 1, s.PilotNumber, s.MaxTurn, s.Last.ToString("HH:mm:ss"), chronoDisplay);
            })
            .ToList();

        NombrePilotesDisplay = $"Pilotes : {Standings.Count}";
    }

    public EndRoundViewModel() : this(MakeDesignSession(), () => { }) { }

    private static RaceSession MakeDesignSession()
    {
        var start = DateTime.Now.AddMinutes(-14);
        var s = new RaceSession("U15-Qualif-1", 5, start);
        var pilots = new[] { "101","105","112","108","103","119","107","115","102","118" };
        for (int turn = 1; turn <= 5; turn++)
            foreach (var (p, i) in pilots.Take(turn < 5 ? pilots.Length : 8).Select((p, i) => (p, i)))
                s.AddEntry(p, turn, start.AddSeconds(turn * 95 + i * 12));
        s.Finish();
        return s;
    }

    [RelayCommand]
    private async Task CopierClassement()
    {
        if (ClipboardProvider.Handler is null) return;
        var text = string.Join("\n", Standings.Select(s => $"{s.PilotNumber}\t{s.ChronoDisplay}"));
        await ClipboardProvider.Handler(text);
    }

    [RelayCommand]
    private async Task Exporter()
    {
        if (ExportProvider.Handler is null)
        {
            ExportStatus = "Export non disponible sur cette plateforme";
            return;
        }
        ExportStatus = "Export en cours...";
        try
        {
            var result = await ExportProvider.Handler(_session);
            ExportStatus = result ?? "Exporté";
        }
        catch (Exception ex)
        {
            ExportStatus = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void NouveauRound() => _onNouveauRound();
}
