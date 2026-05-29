using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using XCC.ViewModels;
using XCC.Views;

namespace XCC;

public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> _map = new()
    {
        { typeof(MainViewModel),     () => new MainView() },
        { typeof(StartupViewModel),  () => new StartupView() },
        { typeof(PilotViewModel),    () => new PilotView() },
        { typeof(EndRoundViewModel), () => new EndRoundView() },
    };

    public Control? Build(object? param)
    {
        if (param is null) return null;
        return _map.TryGetValue(param.GetType(), out var factory)
            ? factory()
            : new TextBlock { Text = "Not Found: " + param.GetType().FullName };
    }

    public bool Match(object? data) => data is ViewModelBase;
}