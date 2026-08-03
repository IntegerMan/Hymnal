---
estimated_steps: 10
estimated_files: 5
skills_used: []
---

# T01: Run automated baseline and collect R004 code-inspection evidence

**Why:** Establish a clean build and test baseline for M002 and gather the code-inspection evidence needed to satisfy R004 and the remaining milestone acceptance criteria before writing the closure record in T02.

**Do:**
1. Run `powershell.exe -NoProfile -Command "dotnet build src/Hymnal/Hymnal.csproj -nologo"` — must exit 0 with 0 errors and 0 warnings.
2. Run `powershell.exe -NoProfile -Command "dotnet test Hymnal.sln -nologo"` — must pass all 57+ tests with 0 failures.
3. Read `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`. Confirm: (a) no `ObservableAsPropertyHelper` field for ProximityFill or HasTarget; (b) both are plain backing fields updated inside a per-chapter subscription block; (c) no `ComputeProximityFill` method, stub, or local fallback calculation exists. Record the exact subscription lines as R004 evidence.
4. Read `src/Hymnal/ViewModels/ChapterViewModel.cs`. Confirm `ProximityFill` and `HasTarget` are computed from authoritative backing fields (`_wordCount`, `_target`) and are exposed as observable properties.
5. Read `src/Hymnal/ViewModels/WorkspaceViewModel.cs`. Locate the orphan-marking pass and the title-match UUID-preservation block (rename continuity). Record the method name(s) and confirm they execute before `AssignUuid`.
6. Read `src/Hymnal/Views/Editor/ValidationMargin.cs`. Confirm two advisory triggers are present: blank line immediately before a `{sample: true}` (or similar block-header) line, and a block containing an unrecognised attribute key. Record the trigger condition names/methods.
7. Read `src/Hymnal/ViewModels/EditorViewModel.cs`. Confirm `.Throttle(TimeSpan.FromMilliseconds(300))` debounce on the content stream and a `Saved` observable or equivalent that drives word-count-history appends.

**Done when:** Build and test both exit cleanly; all five code-inspection checks produce positive findings with specific file references ready for T02.

## Inputs

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/Editor/ValidationMargin.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`

## Expected Output

- Update the implementation and proof artifacts needed for this task.

## Verification

powershell.exe -NoProfile -Command "dotnet test Hymnal.sln -nologo"

## Observability Impact

None — read-only inspection.
