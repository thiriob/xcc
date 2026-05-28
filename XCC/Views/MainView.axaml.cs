using System;
using Avalonia;
using Avalonia.Controls;

namespace XCC.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (e.NewSize.Height > 0)
            ContentArea.Width = Math.Min(e.NewSize.Width, e.NewSize.Height * 9.0 / 16.0);
    }
}
