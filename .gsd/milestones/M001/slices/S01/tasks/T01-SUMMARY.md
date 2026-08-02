---
id: T01
parent: S01
milestone: M001
key_files:
  - Hymnal.slnx
  - src/Hymnal/Hymnal.csproj
  - src/Hymnal/Program.cs
  - src/Hymnal/App.axaml
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/ViewLocator.cs
  - src/Hymnal/ViewModels/ViewModelBase.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal.Core/Hymnal.Core.csproj
  - tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj
key_decisions:
  - Used ReactiveUI.Avalonia 12.0.1 (not Avalonia.ReactiveUI which tops at 11.x) for Avalonia 12 ReactiveUI support
  - Used Avalonia.AvaloniaEdit 12.0.0 (renamed from AvaloniaEdit) for the code editor component
  - Solution uses .slnx format (new dotnet 10 default) — both dotnet restore and build accept it
  - DynamicData pinned to 9.4.31 and System.Reactive to 6.1.0 to satisfy transitive version floor from ReactiveUI 23.2.1
duration: 
verification_result: passed
completed_at: 2026-05-28T19:48:19.293Z
blocker_discovered: false
---

# T01: Scaffolded Hymnal.slnx with Avalonia 12 + ReactiveUI on net10.0 — dotnet build exits 0

**Scaffolded Hymnal.slnx with Avalonia 12 + ReactiveUI on net10.0 — dotnet build exits 0**

## What Happened

Verified net10.0 compatibility via `dotnet new avalonia.mvvm --dry-run --framework net10.0` (accepted). Scaffolded the three-project solution: src/Hymnal (Avalonia MVVM app), src/Hymnal.Core (classlib), tests/Hymnal.Core.Tests (xunit). Created Hymnal.slnx and added all three projects. Added project references (Hymnal→Core, Tests→Core). 

Key deviations from the task plan: (1) The Avalonia 12 template generates Hymnal.slnx (new format) instead of Hymnal.sln — both `dotnet restore` and `dotnet build` accept .slnx transparently. (2) The template defaulted to CommunityToolkit.Mvvm rather than ReactiveUI; replaced with ReactiveUI.Avalonia 12.0.1 (the new package ID for Avalonia 12 — `Avalonia.ReactiveUI` only goes to v11.x). (3) AvaloniaEdit package is now `Avalonia.AvaloniaEdit` 12.0.0 (renamed). (4) Dependency resolution required DynamicData 9.4.31 (pulled by ReactiveUI 23.2.1) and System.Reactive 6.1.0 (pulled by DynamicData 9.4.31). All package versions reconciled and restore succeeds. ViewModelBase updated to extend ReactiveObject with CompositeDisposable. MainWindowViewModel updated to use RaiseAndSetIfChanged. MainWindow.axaml updated to bind Title instead of removed Greeting property. Build succeeds with 0 errors.

## Verification

dotnet build Hymnal.slnx --no-incremental exits 0 with 0 errors, 0 warnings (excluding NETSDK1057 preview SDK info message). Hymnal.Core, Hymnal.Core.Tests, and Hymnal all compiled. All expected output files present: Hymnal.slnx, src/Hymnal/Hymnal.csproj, src/Hymnal/Program.cs, src/Hymnal/App.axaml, src/Hymnal/App.axaml.cs, src/Hymnal/ViewLocator.cs, src/Hymnal.Core/Hymnal.Core.csproj, tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build Hymnal.slnx --no-incremental` | 0 | pass | 10710ms |

## Deviations

Solution file is Hymnal.slnx (not Hymnal.sln) — new dotnet 10 default format, functionally equivalent. ReactiveUI package ID is ReactiveUI.Avalonia (not Avalonia.ReactiveUI). AvaloniaEdit package ID is Avalonia.AvaloniaEdit (not AvaloniaEdit). Package versions adjusted upward to satisfy transitive dependency floors from ReactiveUI 23.2.1.

## Known Issues

none

## Files Created/Modified

- `Hymnal.slnx`
- `src/Hymnal/Hymnal.csproj`
- `src/Hymnal/Program.cs`
- `src/Hymnal/App.axaml`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewLocator.cs`
- `src/Hymnal/ViewModels/ViewModelBase.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal.Core/Hymnal.Core.csproj`
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`
