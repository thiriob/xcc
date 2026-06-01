using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XCC.Models;

namespace XCC.ViewModels;

public partial class PilotViewModel : ViewModelBase
{
    [ObservableProperty] private string _currentNumber = "";
    [ObservableProperty] private bool _isKeypadMode = true;
    [ObservableProperty] private bool _isConfirmingEnd;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(NumberBackground))]
    private bool _raceComplete;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToursDisplay))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(NumberBackground))]
    private int _currentTurns = 1;

    [ObservableProperty] private int _position = 1;
    [ObservableProperty] private string _roundTimerDisplay = "00:00";
    [ObservableProperty] private string _pilotLapTimerDisplay = "00:00";

    private int NbrToursMax { get; }
    public string ToursDisplay => $"{CurrentTurns}/{NbrToursMax}";

    public string StatusText
    {
        get
        {
            if (RaceComplete) return "DERNIER PASSAGE";
            var left = NbrToursMax - CurrentTurns;
            if (left <= 0) return "FINI";
            if (left == 1) return "DERNIER TOUR";
            return $"{left} TOURS RESTANTS";
        }
    }

    public IBrush StatusColor
    {
        get
        {
            if (RaceComplete) return new SolidColorBrush(Color.Parse("#FF6D00"));
            var left = NbrToursMax - CurrentTurns;
            if (left <= 0) return Brushes.Red;
            if (left == 1) return new SolidColorBrush(Color.Parse("#FF8C00"));
            return Brushes.White;
        }
    }

    public IBrush NumberBackground
    {
        get
        {
            if (RaceComplete) return new SolidColorBrush(Color.Parse("#44FF6D00"));
            var left = NbrToursMax - CurrentTurns;
            if (left <= 0) return new SolidColorBrush(Color.Parse("#55FF0000"));
            if (left == 1) return new SolidColorBrush(Color.Parse("#44FF6600"));
            return Brushes.Transparent;
        }
    }

    private readonly Action _onEnd;
    private readonly RaceSession _session;
    private readonly DispatcherTimer _timer;

    public PilotViewModel(string roundName, int nbrToursMax, RaceSession session, Action onEnd)
    {
        NbrToursMax = nbrToursMax;
        _session = session;
        _onEnd = onEnd;

        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public PilotViewModel() : this("U15-Qualif-1", 10, new RaceSession("U15-Qualif-1", 10), () => { })
    {
        _currentNumber = "4567";
        _currentTurns = 4;
        _raceComplete = true; // preview DERNIER PASSAGE state
        _position = 3;
        _roundTimerDisplay = "12:34";
        _pilotLapTimerDisplay = "01:23";
        _isKeypadMode = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _session.StartTime;
        RoundTimerDisplay = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
    }

    [RelayCommand]
    private void AppendDigit(string digit)
    {
        if (CurrentNumber.Length < 6) CurrentNumber += digit;
    }

    [RelayCommand]
    private void Validate()
    {
        if (IsKeypadMode)
        {
            if (string.IsNullOrEmpty(CurrentNumber)) return;

            var previousEntry = _session.GetPreviousEntry(CurrentNumber);

            // Pilot already completed their last lap — ignore
            if (previousEntry.Turn >= NbrToursMax) return;

            CurrentTurns = previousEntry.Turn + 1;
            var newEntry = _session.AddEntry(CurrentNumber, CurrentTurns);

            // First pilot to reach the max turn triggers race-complete mode
            if (!RaceComplete && CurrentTurns >= NbrToursMax)
                RaceComplete = true;

            var lapStart = previousEntry.Timestamp;
            var lap = newEntry.Timestamp - lapStart;
            PilotLapTimerDisplay = $"{(int)lap.TotalMinutes:D2}:{lap.Seconds:D2}";
            Position = _session.Entries.Count(e => e.Turn == CurrentTurns);

            IsKeypadMode = false;
        }
        else
        {
            CurrentNumber = "";
            IsKeypadMode = true;
        }
    }

    [RelayCommand]
    private void Correct()
    {
        if (!IsKeypadMode)
            IsKeypadMode = true;
        else if (CurrentNumber.Length > 0)
            CurrentNumber = CurrentNumber[..^1];
    }

    [RelayCommand]
    private void FinishRound() => IsConfirmingEnd = true;

    [RelayCommand]
    private void ConfirmFinish()
    {
        _session.Finish();
        _timer.Stop();
        _onEnd();
    }

    [RelayCommand]
    private void CancelFinish() => IsConfirmingEnd = false;
}
