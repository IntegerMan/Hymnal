---
estimated_steps: 1
estimated_files: 8
skills_used: []
---

# T01: Scaffolded Hymnal.slnx with Avalonia 12 + ReactiveUI on net10.0 — dotnet build exits 0

Why: The research identified Avalonia 12 + .NET 10 compatibility as the only confirmed risk. We must verify dotnet new avalonia.mvvm supports net10.0 before creating any files. If it fails the fallback is net8.0, documented in a decision. Do: (1) Run dotnet new install Avalonia.Templates to get the latest templates. (2) Run dotnet new avalonia.mvvm --dry-run --framework net10.0 in a temp location to confirm the TFM is accepted. (3) Scaffold: dotnet new avalonia.mvvm -o src/Hymnal --framework net10.0 inside the worktree root; rename the namespace from Hymnal to Hymnal in all generated files. (4) Create src/Hymnal.Core/ as a classlib: dotnet new classlib -o src/Hymnal.Core --framework net10.0 --no-restore. (5) Create tests/Hymnal.Core.Tests/ as an xunit project: dotnet new xunit -o tests/Hymnal.Core.Tests --framework net10.0 --no-restore. (6) Create Hymnal.sln and add all three projects: dotnet new sln -n Hymnal && dotnet sln add src/Hymnal/Hymnal.csproj src/Hymnal.Core/Hymnal.Core.csproj tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj. (7) Add project references: Hymnal -> Hymnal.Core, Hymnal.Core.Tests -> Hymnal.Core. (8) Add required NuGet packages to src/Hymnal/Hymnal.csproj: Avalonia, Avalonia.ReactiveUI, Avalonia.Desktop, AvaloniaEdit, DynamicData, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting. Add to src/Hymnal.Core/Hymnal.Core.csproj: DynamicData, System.Reactive, Microsoft.Extensions.DependencyInjection.Abstractions. Add to tests/Hymnal.Core.Tests/: NSubstitute. (9) Run dotnet restore Hymnal.sln. Done when: dotnet build Hymnal.sln --no-incremental exits 0.

## Inputs

- None specified.

## Expected Output

- `Hymnal.sln`
- `src/Hymnal/Hymnal.csproj`
- `src/Hymnal/Program.cs`
- `src/Hymnal/App.axaml`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewLocator.cs`
- `src/Hymnal.Core/Hymnal.Core.csproj`
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`

## Verification

dotnet build Hymnal.sln --no-incremental
