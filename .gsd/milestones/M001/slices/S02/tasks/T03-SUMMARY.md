---
id: T03
parent: S02
milestone: M001
key_files:
  - src/Hymnal.Core/Interfaces/IFolderPickerService.cs
  - src/Hymnal.Core/Infrastructure/AppSettingsStore.cs
  - src/Hymnal.Core/AssemblyInfo.cs
  - tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs
key_decisions:
  - AppSettingsStore placed in Hymnal.Core/Infrastructure instead of Hymnal/Infrastructure — it has no UI dependencies and the test project only references Hymnal.Core; adding Hymnal (Avalonia WinExe) as a test dependency would pull in heavy UI packages unnecessarily
  - InternalsVisibleTo attribute added via AssemblyInfo.cs to expose the internal test constructor overload
duration: 
verification_result: passed
completed_at: 2026-05-29T03:33:23.876Z
blocker_discovered: false
---

# T03: IFolderPickerService interface and AppSettingsStore (atomic JSON settings store) implemented with 3 passing tests

**IFolderPickerService interface and AppSettingsStore (atomic JSON settings store) implemented with 3 passing tests**

## What Happened

Created IFolderPickerService.cs in Hymnal.Core/Interfaces as a thin interface with no Avalonia dependency. Created AppSettingsStore in Hymnal.Core/Infrastructure (deviation from plan's Hymnal/Infrastructure path — see Deviations). The store uses atomic write-temp-rename pattern, camelCase JSON serialization, and supports an internal constructor overload for test isolation. Added InternalsVisibleTo("Hymnal.Core.Tests") via AssemblyInfo.cs. Created 3 tests covering round-trip string value, missing-key default, and directory-creation-on-write.

## Verification

dotnet test tests/Hymnal.Core.Tests/ --filter AppSettingsStoreTests — 3 passed, 0 failed

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/ --filter AppSettingsStoreTests` | 0 | pass | 5000ms |

## Deviations

Plan specified src/Hymnal/Infrastructure/AppSettingsStore.cs but the test project (Hymnal.Core.Tests) only references Hymnal.Core. Moving to Hymnal.Core/Infrastructure avoids adding Avalonia/ReactiveUI as test transitive dependencies while keeping the implementation accessible to tests via InternalsVisibleTo.

## Known Issues

none

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IFolderPickerService.cs`
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`
- `src/Hymnal.Core/AssemblyInfo.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs`
