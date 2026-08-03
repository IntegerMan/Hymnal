---
estimated_steps: 8
estimated_files: 1
skills_used: []
---

# T02: Defensive sweep and full solution build + test verification

**Why**: Confirm that the T01 changes produce a clean build and do not regress any of the 57 existing unit tests. Also perform a brief defensive sweep of MainWindowViewModel to confirm no similar OAPH self-dependency or null-init ordering hazards exist there.

**Do**:
1. Run `dotnet build src/Hymnal/Hymnal.csproj -nologo` — confirm exit 0, 0 errors.
2. Run `dotnet test Hymnal.sln -nologo` — confirm all tests pass, 0 failures, 0 skipped.
3. Review `MainWindowViewModel.cs`: its two OAPHs (`_isAnyRightPaneOpen`, `_isBothRightPanesOpen`) depend on `ChapterInfoViewModel.IsVisible` and `NotesViewModel.IsVisible` (both plain backing-field properties) — no self-referential hazard. Confirm no code changes are needed.
4. Review `ChapterViewModel.cs` OAPH chain: `ProximityFill` depends on `Target` + `WordCount` (backing fields); `HasTarget` depends on `Target` (backing field) — both safe. No changes needed.
5. If any compilation error or test failure surfaces, fix it before marking this task complete. Document any remaining stubs or deferred items (e.g. CredentialStoreStub, ThrownExceptions on synchronous commands) for S05 reference.

**Done when**: `dotnet build` exits 0; `dotnet test Hymnal.sln -nologo` reports 57+ tests, 0 failed; MainWindowViewModel confirmed clean with no changes required.

## Inputs

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`

## Expected Output

- `src/Hymnal/ViewModels/MainWindowViewModel.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo && dotnet test Hymnal.sln -nologo
