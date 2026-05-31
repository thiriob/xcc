using CommunityToolkit.Mvvm.ComponentModel;
using XCC.Models;

namespace XCC.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentViewModel;

    public MainViewModel()
    {
        _currentViewModel = new StartupViewModel(GoToPilotView
#if DEBUG
            , session => CurrentViewModel = new EndRoundViewModel(session, Reset)
#endif
        );
    }

    private void GoToPilotView(string roundName, int nbrTours)
    {
        var session = new RaceSession(roundName, nbrTours);
        CurrentViewModel = new PilotViewModel(roundName, nbrTours, session, () =>
            CurrentViewModel = new EndRoundViewModel(session, Reset));
    }

    private void Reset()
    {
        CurrentViewModel = new StartupViewModel(GoToPilotView);
    }
}
