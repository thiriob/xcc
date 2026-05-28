# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**XCC** is a multi-platform Avalonia UI (.NET 10) app to simplify participant counting and classification at XCC (Cross-Country Cycling) races. It targets Windows desktop primarily, with Android/iOS/Browser support via shared core.

## Build Commands

```powershell
dotnet build
dotnet run --project XCC.Desktop
dotnet run --project XCC.Android
dotnet run --project XCC.Browser
```

No test projects exist yet. When added: `dotnet test`, single class: `dotnet test --filter "ClassName=Foo"`.

## Architecture

### Bootstrap

`XCC.Desktop/Program.cs` → `AppBuilder.Configure<App>().UsePlatformDetect()` → `App.axaml.cs` → `OnFrameworkInitializationCompleted()` creates the root view with `MainViewModel` as `DataContext`.

`WithDeveloperTools()` is injected in `#if DEBUG` only.

### MVVM

- `ViewModelBase` (`XCC/ViewModels/ViewModelBase.cs`) extends `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
- `[ObservableProperty]` on a `private` field generates the public property + `PropertyChanged` notification
- `[RelayCommand]` on a method generates an `ICommand` property with optional `CanExecute` via `[RelayCommand(CanExecute = nameof(...))]`
- ViewModels must be `partial` classes for source generators to work

### View Hierarchy

```
MainWindow                     ← desktop window (800×450)
└─ MainView (UserControl)      ← shared across all platforms (design: 720×1280)
   └─ bound to MainViewModel
```

`MainWindow` simply hosts `MainView` as a child. Mobile and browser platforms use `MainView` directly without a window.

### ViewLocator

`XCC/ViewLocator.cs` resolves views from view models by reflection: replaces `ViewModel` → `View` in the full type name. The naming convention is strict — `FooViewModel` must pair with a `FooView` in the same namespace structure.

### Platform Dispatch

`App.OnFrameworkInitializationCompleted()` branches on lifetime:
- `IClassicDesktopStyleApplicationLifetime` → `new MainWindow { DataContext = new MainViewModel() }`
- `IActivityApplicationLifetime` → `MainView` via factory (Android)
- `ISingleViewApplicationLifetime` → `MainView` direct (iOS, Browser)

### Key Settings

- Compiled bindings on by default (`AvaloniaUseCompiledBindingsByDefault=true`) — always set `x:DataType` in XAML views
- All NuGet versions centrally managed in `Directory.Packages.props`; keep all `Avalonia.*` packages at the same version
- `AvaloniaUI.DiagnosticsSupport` (DevTools) included in Debug builds only
