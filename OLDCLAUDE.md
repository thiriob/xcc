

# OLDCLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Avalonia 11 desktop GUI for CerfaExtract (a French administrative form data extraction tool). The app has two modes: **Simple** (linear wizard) and **Advanced** (detailed panels with data grids). All UI state lives in a single `MainWindowViewModel`.

## Build Commands

```powershell
dotnet build AvaloniaGUI.csproj
dotnet build AvaloniaGUI.csproj -c Release
dotnet run --project AvaloniaGUI.csproj
```

Compiled bindings are enabled by default (`AvaloniaUseCompiledBindingsByDefault`). Avalonia.Diagnostics is included in Debug builds only.

## Architecture

### Bootstrap

`Program.cs` → initializes Velopack (auto-update) → builds Avalonia app → `App.axaml.cs` creates `MainWindow` with `MainWindowViewModel` as its `DataContext`.

### MVVM

- `ViewModelBase` extends `ObservableObject` from `CommunityToolkit.Mvvm`
- `[ObservableProperty]` generates backing fields and `PropertyChanged` notifications
- `[RelayCommand]` generates commands with optional `CanExecute` predicates
- A single `MainWindowViewModel` holds all app state: file paths, file statuses, extracted data collections, and settings

### View Hierarchy

```
MainWindow
└─ MainView                  ← navigation hub; header has Simple/Advanced toggle + Settings button
   ├─ Simple                 ← linear 3-file picker + Export button (window: 700×350, non-resizable)
   └─ Advanced/
      ├─ LeftView            ← file pickers + action buttons (fixed 550 width)
      └─ RightView           ← expandable DataGrids for entries and errors
```

`MainView.axaml.cs` handles mode switching: resizes the window and toggles resizability.

### Custom Controls

- **`PickFile`** — label + text box + browse button. Properties: `Label`, `Path` (two-way), `FileType` (extension filter), `Create` (SaveFilePicker vs OpenFilePicker).
- **`FileStatus`** — status badge mapped from `StatusEnum` to localized French strings and colors (green/orange/red).

### Dialogs

All dialogs are `Window` subclasses shown with `ShowDialog(owner)`. They expose a `bool Result` property.

- `ConfirmationDialog` — Yes/No
- `InfosDialog` — OK only
- `SettingsDialog` — checkboxes bound to `Settings` properties; saves to `Config.ini` on confirm

### Configuration

`Models/Settings.cs` reads/writes `Config.ini` (INI format, `ini-parser` library). Loaded at startup, saved on window close (`OnClosed`).

```ini
[Behaviour]
OpenAdvancedOnLaunch, ExtractOnPathChanged, SavePathsOnQuit, OpenFolderAfterExport, ShowDuplicateEntries

[Paths]
ModelPath, SourcePath, OutputPath
```

### Value Converters

Registered in `App.axaml` as static resources:
- `ModelErrorConverter` — `ModelSyntaxError` → localized French error string
- `SourceErrorConverter` — `Entry` (with `ErrorType`) → localized French error string

### Theme & Colors

Dark mode (`RequestedThemeVariant="Dark"`), Fluent theme. Three app-level brushes defined in `App.axaml`:
- `LightBlueBrush` `#606875`, `MidBlueBrush` `#373c44`, `DarkBlueBrush` `#1a1c20`

`Label.h1` style defined in `MainWindow.axaml`: `FontSize=24, FontWeight=Bold`.

## Core Library Integration

The GUI project references `CerfaExtract.csproj`. `MainWindowViewModel` instantiates:

```csharp
private readonly CerfaExtractor _lib = new(new OpenXmlEditor(), new PdfiumViewerExtractor());
```

The three main operations called on `_lib`:
1. `OpenAndVerifyModel(path)` / `OpenAndVerifySource(path)` — called on each path change
2. `ExtractModelKeys()` / `ExtractDataFromPdf()` — populate the observable collections
3. `ApplyAndExportModifiedModel(outputPath)` — final export

All CerfaExtract exceptions are caught and mapped to `FileStatus.StatusEnum` values.