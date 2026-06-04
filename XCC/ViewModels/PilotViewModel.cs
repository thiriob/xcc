using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XCC.Models;

namespace XCC.ViewModels;

public partial class PilotViewModel : ViewModelBase
{
    [ObservableProperty] private string _currentNumber = "";
    [ObservableProperty] private bool _isConfirmingEnd;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastPilot))]
    [NotifyPropertyChangedFor(nameof(ShowInfo))]
    private string _lastPilotNumber = "";
    [ObservableProperty] private string _lastPilotInfo = "";
    public bool HasLastPilot => !string.IsNullOrEmpty(LastPilotNumber);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberBackground))]
    [NotifyPropertyChangedFor(nameof(ShowInfo))]
    [NotifyPropertyChangedFor(nameof(ShowRaceComplete))]
    private bool _raceComplete;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInfo))]
    [NotifyPropertyChangedFor(nameof(ShowRaceComplete))]
    [NotifyPropertyChangedFor(nameof(ShowRejected))]
    private string _rejectedMessage = "";

    public bool ShowInfo => !RaceComplete && HasLastPilot && string.IsNullOrEmpty(RejectedMessage);
    public bool ShowRaceComplete => RaceComplete && string.IsNullOrEmpty(RejectedMessage);
    public bool ShowRejected => !string.IsNullOrEmpty(RejectedMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberBackground))]
    private int _currentTurns = 1;

    private int NbrToursMax { get; }

    public IBrush NumberBackground
    {
        get
        {
            if (RaceComplete) return new SolidColorBrush(Color.Parse("#55FF0000"));
            var left = NbrToursMax - CurrentTurns;
            if (left <= 0) return new SolidColorBrush(Color.Parse("#55FF0000"));
            if (left == 1) return new SolidColorBrush(Color.Parse("#44FF6600"));
            return Brushes.Transparent;
        }
    }

    private readonly Action _onEnd;
    private readonly RaceSession _session;

    public PilotViewModel(string roundName, int nbrToursMax, RaceSession session, Action onEnd)
    {
        NbrToursMax = nbrToursMax;
        _session = session;
        _onEnd = onEnd;
    }

    public PilotViewModel() : this("U15-Qualif-1", 10, new RaceSession("U15-Qualif-1", 10), () => { })
    {
        _currentNumber = "4567";
        _currentTurns = 4;
        _raceComplete = true;
        LastPilotNumber = "42";
        _lastPilotInfo = "Tour 4/10 · 6 restants";
    }

    [RelayCommand]
    private void AppendDigit(string digit)
    {
        if (CurrentNumber.Length < 6) CurrentNumber += digit;
    }

    [RelayCommand]
    private void Validate()
    {
        if (string.IsNullOrEmpty(CurrentNumber)) return;

        var previousEntry = _session.GetPreviousEntry(CurrentNumber);

        if (previousEntry.Turn >= NbrToursMax)
        {
            RejectedMessage = $"Pilote {CurrentNumber} a déjà fini";
            CurrentNumber = "";
            _ = ClearRejectedMessageAsync();
            return;
        }

        RejectedMessage = "";
        CurrentTurns = previousEntry.Turn + 1;
        _session.AddEntry(CurrentNumber, CurrentTurns);

        if (!RaceComplete && CurrentTurns >= NbrToursMax)
            RaceComplete = true;

        LastPilotNumber = CurrentNumber;
        var left = NbrToursMax - CurrentTurns;
        LastPilotInfo = left <= 0
            ? $"Tour {CurrentTurns}/{NbrToursMax} · Terminé"
            : left == 1
                ? $"Tour {CurrentTurns}/{NbrToursMax} · Dernier tour"
                : $"Tour {CurrentTurns}/{NbrToursMax} · {left} restants";

        CurrentNumber = "";
    }

    [RelayCommand]
    private void Correct()
    {
        if (CurrentNumber.Length > 0)
            CurrentNumber = CurrentNumber[..^1];
    }

    [RelayCommand]
    private void FinishRound() => IsConfirmingEnd = true;

    [RelayCommand]
    private void ConfirmFinish()
    {
        _session.Finish();
        _onEnd();
    }

    [RelayCommand]
    private void CancelFinish() => IsConfirmingEnd = false;

    private async Task ClearRejectedMessageAsync()
    {
        await Task.Delay(2000);
        RejectedMessage = "";
    }
}
