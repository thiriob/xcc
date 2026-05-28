using CommunityToolkit.Mvvm.ComponentModel;

namespace XCC.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentViewModel;

    public MainViewModel()
    {
        _currentViewModel = new StartupViewModel(GoToPilotView);
    }

    private void GoToPilotView(string roundName, int nbrTours)
    {
        CurrentViewModel = new PilotViewModel(roundName, nbrTours, () =>
            CurrentViewModel = new EndRoundViewModel(0, nbrTours, nbrTours, System.TimeSpan.Zero, Reset));
    }

    private void Reset()
    {
        CurrentViewModel = new StartupViewModel(GoToPilotView);
    }
}
