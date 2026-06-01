using Avalonia.Media;

namespace XCC.ViewModels;

public record PlacementEntry(int Position, string PilotNumber, int MaxTurn, string LastTime)
{
    public IBrush RowBackground => Position switch
    {
        1 => new SolidColorBrush(Color.Parse("#44FFD700")),
        2 => new SolidColorBrush(Color.Parse("#33C0C0C0")),
        3 => new SolidColorBrush(Color.Parse("#44CD7F32")),
        _ => Brushes.Transparent
    };

    public IBrush PositionColor => Position switch
    {
        1 => new SolidColorBrush(Color.Parse("#FFD700")),
        2 => new SolidColorBrush(Color.Parse("#C0C0C0")),
        3 => new SolidColorBrush(Color.Parse("#CD7F32")),
        _ => Brushes.White
    };
}
