using System;
using CommunityToolkit.Mvvm.Input;

namespace XCC.ViewModels;

public partial class EndRoundViewModel : ViewModelBase
{
    public string NombrePilotesDisplay { get; }
    public string ToursDisplay { get; }
    public string TempsTotalDisplay { get; }

    private readonly Action _onNouveauRound;

    public EndRoundViewModel(int nombrePilotes, int toursActuels, int toursMax, TimeSpan tempsTotal, Action onNouveauRound)
    {
        NombrePilotesDisplay = $"Nombre pilotes : {nombrePilotes}";
        ToursDisplay = $"Tours : {toursActuels}/{toursMax}";
        TempsTotalDisplay = $"Temps total : {(int)tempsTotal.TotalMinutes:D2}:{tempsTotal.Seconds:D2}";
        _onNouveauRound = onNouveauRound;
    }

    public EndRoundViewModel() : this(20, 10, 10, new TimeSpan(0, 10, 0), () => { }) { }

    [RelayCommand]
    private void Exporter() { } // TODO: export results to file

    [RelayCommand]
    private void NouveauRound() => _onNouveauRound();
}