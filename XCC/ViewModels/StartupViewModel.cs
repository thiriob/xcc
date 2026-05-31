using System;
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
}
