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
    [ObservableProperty] private string _numeroCourant = "";
    [ObservableProperty] private bool _isKeypadMode = true;
    [ObservableProperty] private string _precedentNumero = "-";
    [ObservableProperty] private bool _isConfirmingEnd;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToursDisplay))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(NumberBackground))]
    private int _toursActuels = 1;

    [ObservableProperty] private int _position = 1;
    [ObservableProperty] private string _roundTimerDisplay = "00:00";
    [ObservableProperty] private string _pilotLapTimerDisplay = "00:00";

    public string RoundName { get; }
    public int NbrToursMax { get; }
    public string ToursDisplay => $"{ToursActuels}/{NbrToursMax}";

    public string StatusText
    {
        get
        {
            var left = NbrToursMax - ToursActuels;
            if (left <= 0) return "FINI";
            if (left == 1) return "DERNIER TOUR";
            return $"{left} TOURS RESTANTS";
        }
    }

    public IBrush StatusColor
    {
        get
        {
            var left = NbrToursMax - ToursActuels;
            if (left <= 0) return Brushes.Red;
            if (left == 1) return new SolidColorBrush(Color.Parse("#FF8C00"));
            return Brushes.White;
        }
    }

    public IBrush NumberBackground
    {
        get
        {
            var left = NbrToursMax - ToursActuels;
            if (left <= 0) return new SolidColorBrush(Color.Parse("#55FF0000"));
            if (left == 1) return new SolidColorBrush(Color.Parse("#44FF6600"));
            return Brushes.Transparent;
        }
    }

    private readonly Action _onEnd;
    private readonly RaceSession _session;
    private readonly DispatcherTimer _timer;
    private DateTime _currentPilotLapStart;

    public PilotViewModel(string roundName, int nbrToursMax, RaceSession session, Action onEnd)
    {
        RoundName = roundName;
        NbrToursMax = nbrToursMax;
        _session = session;
        _onEnd = onEnd;
        _currentPilotLapStart = session.StartTime;

        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public PilotViewModel() : this("U15-Qualif-1", 10, new RaceSession("U15-Qualif-1", 10), () => { })
    {
        _numeroCourant = "4567";
        _toursActuels = 9; // DERNIER TOUR for design preview
        _position = 3;
        _precedentNumero = "1234";
        _roundTimerDisplay = "12:34";
        _pilotLapTimerDisplay = "01:23";
        _isKeypadMode = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _session.StartTime;
        RoundTimerDisplay = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        if (!IsKeypadMode)
        {
            var lap = DateTime.Now - _currentPilotLapStart;
            PilotLapTimerDisplay = $"{(int)lap.TotalMinutes:D2}:{lap.Seconds:D2}";
        }
    }

    [RelayCommand]
    private void AppendDigit(string digit)
    {
        if (NumeroCourant.Length < 6) NumeroCourant += digit;
    }

    [RelayCommand]
    private void Valider()
    {
        if (IsKeypadMode)
        {
            if (string.IsNullOrEmpty(NumeroCourant)) return;
            var lastEntry = _session.Entries.LastOrDefault(e => e.PilotNumber == NumeroCourant);
            _currentPilotLapStart = lastEntry?.Timestamp ?? _session.StartTime;
            var lap = DateTime.Now - _currentPilotLapStart;
            PilotLapTimerDisplay = $"{(int)lap.TotalMinutes:D2}:{lap.Seconds:D2}";
            Position = _session.Entries.Count(e => e.Turn == ToursActuels) + 1;
            IsKeypadMode = false;
        }
        else
        {
            _session.AddEntry(NumeroCourant, ToursActuels);
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
    private void FinirRound() => IsConfirmingEnd = true;

    [RelayCommand]
    private void ConfirmerFin()
    {
        _session.Finish();
        _timer.Stop();
        _onEnd();
    }

    [RelayCommand]
    private void AnnulerFin() => IsConfirmingEnd = false;
}
