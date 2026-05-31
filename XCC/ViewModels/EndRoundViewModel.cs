using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using XCC.Models;

namespace XCC.ViewModels;

public partial class EndRoundViewModel : ViewModelBase
{
    public string RoundName { get; }
    public string HeureFinDisplay { get; }
    public string NombrePilotesDisplay { get; }
    public string ToursDisplay { get; }
    public string TempsTotalDisplay { get; }
    public string Top1Display { get; }
    public string Top2Display { get; }
    public string Top3Display { get; }

    private readonly Action _onNouveauRound;
    private readonly RaceSession _session;

    public EndRoundViewModel(RaceSession session, Action onNouveauRound)
    {
        _session = session;
        _onNouveauRound = onNouveauRound;

        var elapsed = session.Elapsed;
        var finishTime = session.FinishTime ?? DateTime.Now;
        var uniquePilots = session.Entries.Select(e => e.PilotNumber).Distinct().Count();
        var maxTurn = session.Entries.Count > 0 ? session.Entries.Max(e => e.Turn) : 0;
        var top = session.Entries.Select(e => e.PilotNumber).Distinct().Take(3).ToList();

        RoundName = session.RoundName;
        HeureFinDisplay = $"Fin à {finishTime:HH:mm}";
        NombrePilotesDisplay = $"Pilotes : {uniquePilots}";
        ToursDisplay = $"Tours : {maxTurn}/{session.TurnsMax}";
        TempsTotalDisplay = $"Durée : {(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        Top1Display = top.Count > 0 ? top[0] : "-";
        Top2Display = top.Count > 1 ? top[1] : "-";
        Top3Display = top.Count > 2 ? top[2] : "-";
    }

    public EndRoundViewModel() : this(MakeDesignSession(), () => { }) { }

    private static RaceSession MakeDesignSession()
    {
        var s = new RaceSession("U15-Qualif-1", 5, DateTime.Now.AddMinutes(-12).AddSeconds(-34));
        s.AddEntry("1234", 5);
        s.AddEntry("5678", 5);
        s.AddEntry("9012", 4);
        s.Finish();
        return s;
    }

    [RelayCommand]
    private void Exporter() { } // TODO: export session entries as CSV

    [RelayCommand]
    private void NouveauRound() => _onNouveauRound();
}
