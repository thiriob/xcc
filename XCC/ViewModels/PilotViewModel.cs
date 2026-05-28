using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XCC.ViewModels;

public partial class PilotViewModel : ViewModelBase
{
    [ObservableProperty] private string _numeroCourant = "";
    [ObservableProperty] private bool _isKeypadMode = true;
    [ObservableProperty] private string _precedentNumero = "-";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToursDisplay))]
    private int _toursActuels;

    [ObservableProperty] private int _position = 1;

    public string RoundName { get; }
    public int NbrToursMax { get; }
    public string ToursDisplay => $"{ToursActuels}/{NbrToursMax}";

    private readonly Action _onEnd;
    private readonly DateTime _startTime = DateTime.Now;

    public PilotViewModel(string roundName, int nbrToursMax, Action onEnd)
    {
        RoundName = roundName;
        NbrToursMax = nbrToursMax;
        _onEnd = onEnd;
    }

    public PilotViewModel() : this("U15-Qualif-1", 10, () => { })
    {
        _numeroCourant = "4567";
        _toursActuels = 5;
        _position = 1;
        _precedentNumero = "1234";
    }

    [RelayCommand]
    private void AppendDigit(string digit)
    {
        if (NumeroCourant.Length < 6)
            NumeroCourant += digit;
    }

    [RelayCommand]
    private void Valider()
    {
        if (IsKeypadMode)
        {
            if (string.IsNullOrEmpty(NumeroCourant)) return;
            // TODO: look up pilot data from race state
            IsKeypadMode = false;
        }
        else
        {
            PrecedentNumero = NumeroCourant;
            NumeroCourant = "";
            IsKeypadMode = true;
        }
    }

    [RelayCommand]
    private void Corriger()
    {
        if (!IsKeypadMode)
            IsKeypadMode = true;
        else if (NumeroCourant.Length > 0)
            NumeroCourant = NumeroCourant[..^1];
    }

    [RelayCommand]
    private void TerminerRound() => _onEnd();
}
