---
estimated_steps: 10
estimated_files: 6
skills_used: []
---

# T05: Run slice-wide regression and document verification evidence

Skills expected: verify-before-complete.

Why: This slice touches the shared Corkboard surface and canonical structural services that prior slices depend on. A focused pass is not enough; executor closure needs fresh evidence that S02 sidebar exclusion, S05 Corkboard drag/drop, and the app build still survive.

Do:
1. Run the three focused Corkboard verification commands from this slice plan and record pass/fail evidence in the task summary.
2. Run `dotnet test Hymnal.slnx --nologo --verbosity minimal` to catch regressions outside the focused Corkboard tests.
3. Run `dotnet build src/Hymnal/Hymnal.csproj --nologo` to verify the Avalonia app compiles.
4. If the normal execute-task environment hits the known MEM008 gsd_exec NuGet path failure, rerun in the native shell available to the executor and record the environment limitation plus any successful native verification.
5. If combined Avalonia/xUnit filters hang, use per-class or per-test method runs as accepted in S05, but do not claim completion without fresh passing evidence for the include/exclude and insertion tests added in this slice.
6. Summarize which R013 acceptance points were advanced and explicitly leave S08 desktop cross-surface replay as remaining work.

Done when: Focused Corkboard tests, full solution tests, and app build have fresh recorded evidence, or any environment-specific limitation is documented with equivalent focused pass evidence.

## Inputs

- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`

## Verification

dotnet test Hymnal.slnx --nologo --verbosity minimal

## Observability Impact

Closure evidence records the inspectable diagnostics and failure-state behavior future agents should rely on when S08 performs cross-surface replay.
