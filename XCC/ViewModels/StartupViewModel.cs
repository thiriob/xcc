using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XCC.Models;
using XCC.Services;

namespace XCC.ViewModels;

public partial class StartupViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DemarrerCommand))]
    private string _roundName = "U15-Qualif-1";

    [ObservableProperty] private int _nombreTours = 5;

    private readonly Action<string, int> _onStart;
    private readonly Action<RaceSession>? _onSimulate;

    public string Version => BuildInfo.Version;

    public bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    public StartupViewModel(Action<string, int> onStart, Action<RaceSession>? onSimulate = null)
    {
        _onStart = onStart;
        _onSimulate = onSimulate;
    }

    public StartupViewModel() : this((_, _) => { }) { }

    [RelayCommand]
    private void Increment() => NombreTours++;

    [RelayCommand]
    private void Decrement() { if (NombreTours > 1) NombreTours--; }

    [RelayCommand(CanExecute = nameof(CanDemarrer))]
    private void Demarrer() => _onStart(RoundName, NombreTours);

    private bool CanDemarrer() => !string.IsNullOrWhiteSpace(RoundName);

    [RelayCommand]
    private void Simuler() => _onSimulate?.Invoke(SimulationService.Create(RoundName, NombreTours));

    [RelayCommand]
    private void SimulerSequence()
    {
        // Fixed sequence: pilots crossing the line in this order.
        // Pilot "1" reaches turn 5 first (entry 11); others are DNF at various lap counts.
        string[] seq = ["1","2588","7","1","2","1","2","85","1","2","1","2588","7","2","85"];
        const int numTurns = 5;
        const double lapSeconds = 90;

        var start = DateTime.Now.AddSeconds(-(seq.Length * lapSeconds + 30));
        var session = new RaceSession(RoundName, numTurns, start);

        var turns = new Dictionary<string, int>();
        for (int i = 0; i < seq.Length; i++)
        {
            var pilot = seq[i];
            turns.TryGetValue(pilot, out var t);
            turns[pilot] = ++t;
            session.AddEntry(pilot, t, start.AddSeconds((i + 1) * lapSeconds));
        }

        session.Finish(addFinalizationEntries: false);
        _onSimulate?.Invoke(session);
    }
}
