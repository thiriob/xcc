using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using XCC.ViewModels;

namespace XCC.Views;

public partial class PilotView : UserControl
{
    public PilotView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
            topLevel.KeyDown += OnKeyDown;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
            topLevel.KeyDown -= OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not PilotViewModel vm) return;
        if (vm.IsConfirmingEnd) return;

        switch (e.Key)
        {
            case Key.D0 or Key.NumPad0: vm.AppendDigitCommand.Execute("0"); break;
            case Key.D1 or Key.NumPad1: vm.AppendDigitCommand.Execute("1"); break;
            case Key.D2 or Key.NumPad2: vm.AppendDigitCommand.Execute("2"); break;
            case Key.D3 or Key.NumPad3: vm.AppendDigitCommand.Execute("3"); break;
            case Key.D4 or Key.NumPad4: vm.AppendDigitCommand.Execute("4"); break;
            case Key.D5 or Key.NumPad5: vm.AppendDigitCommand.Execute("5"); break;
            case Key.D6 or Key.NumPad6: vm.AppendDigitCommand.Execute("6"); break;
            case Key.D7 or Key.NumPad7: vm.AppendDigitCommand.Execute("7"); break;
            case Key.D8 or Key.NumPad8: vm.AppendDigitCommand.Execute("8"); break;
            case Key.D9 or Key.NumPad9: vm.AppendDigitCommand.Execute("9"); break;
            case Key.Back:              vm.CorrigerCommand.Execute(null);   break;
            case Key.Enter:             vm.ValiderCommand.Execute(null);    break;
            default: return;
        }

        e.Handled = true;
    }
}
