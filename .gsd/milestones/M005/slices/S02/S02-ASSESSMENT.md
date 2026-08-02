---
sliceId: S02
uatType: browser-executable
verdict: PASS
date: 2026-06-18T08:16:51.761Z
---

# UAT Result — S02

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| 1. Load the workspace and inspect `WorkspaceViewModel.Nodes` / `VisibleNodes`. | artifact | PASS | `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs` seeds temp workspaces with real `BookTxtStructureService`, `ExclusionManifestService`, and orphan discovery, then opens/reloads `WorkspaceViewModel` and asserts both `Nodes` and `VisibleNodes`. T02/T03 summaries record targeted suite passing. |
| 2. Confirm the manifest-backed orphan markdown file appears in the sidebar as a chapter node with `IsExcluded = true`. | artifact | PASS | `LoadWorkspaceAsync_ProjectsManifestExcludedChaptersInSidebarOrderAndIgnoresOrdinaryOrphans` asserts the projected node `part-one/chapter-02.md` exists and `Node.IsExcluded` is true. `src/Hymnal/ViewModels/WorkspaceViewModel.cs` builds excluded projection nodes via `BuildExcludedProjectionNode(...){ IsExcluded = true }`. |
| 3. Confirm an ordinary orphan markdown file that is not listed in `exclusions.json` does not appear as an excluded sidebar node. | artifact | PASS | The same projection test asserts `part-one/unmanifested-orphan.md` is absent from `WorkspaceViewModel.Nodes`, proving only manifest-backed orphan files are projected. |
| 4. Trigger the sidebar include action for the excluded node. | artifact | PASS | `IncludeExcludedChapterCommand_IncludesExcludedNodeAndUpdatesBookTxtAndManifestWithoutDuplicateReloadNodes` executes `Workspace.IncludeExcludedChapterCommand.Execute("chapter-two.md")`. `SidebarView.axaml.cs` also wires excluded context-menu rows to `IncludeExcludedChapterCommand` only when the chapter is excluded. |
| 5. Verify the chapter is inserted back into `Book.txt` at the sidebar-derived position and removed from `.hymnal-data/exclusions.json`. | artifact | PASS | The include-command test asserts `Book.txt` becomes `chapter-one.md`, `chapter-three.md`, `chapter-two.md` and `LoadExcludedPathsAsync()` returns empty. `TryBuildIncludeExistingRequest` computes the insertion index from visible sidebar order among non-excluded nodes. |
| 6. Reload the workspace through a fresh `WorkspaceViewModel` instance and confirm the chapter is now an ordinary included sidebar node. | artifact | PASS | The include-command test reloads the workspace and asserts only one `chapter-two.md` node remains and `Node.IsExcluded` is false. Separate reload coverage in `LoadWorkspaceAsync_ReloadingFreshWorkspaceViewModel_ReprojectsManifestExcludedNodes` proves fresh-instance re-projection behavior. |
| 7. Trigger the sidebar exclude action for an included present chapter. | artifact | PASS | `RemoveFromBookCommand_ExcludesIncludedNodeAndUpdatesBookTxtAndManifest` executes `Workspace.RemoveFromBookCommand.Execute("chapter-two.md")`. `SidebarView.axaml.cs` guards the menu path so only included present chapter rows call `RemoveFromBookCommand`. |
| 8. Verify the chapter is removed from `Book.txt`, added to `.hymnal-data/exclusions.json`, and projected back into the sidebar as excluded after reload. | artifact | PASS | The exclude-command test asserts `Book.txt` drops `chapter-two.md`, the exclusion manifest contains only `chapter-two.md`, and the reloaded sidebar contains a projected node with `Node.IsExcluded == true`. |
| 9. Trigger the remove action for a missing `Book.txt` entry and verify the stale entry is removed without adding it to `exclusions.json`. | artifact | PASS | `RemoveMissingEntryCommand_RemovesMissingBookTxtEntryWithoutCreatingManifestExclusion` executes `RemoveMissingEntryCommand`, then asserts `chapter-missing.md` is removed from `Book.txt`, no exclusions are recorded, and the missing node disappears from the sidebar. |
| 10. Attempt to select an excluded node and verify the current editor selection is preserved rather than opening the excluded file in the editor. | artifact | PASS | `TrySwitchChapterAsync_WhenNodeIsExcluded_DoesNotOpenExcludedFileAndRestoresPreviousSelection` opens an included chapter, selects an excluded node, and asserts `SelectedNode`, `Editor.ActiveNode`, and `Editor.ActiveFilePath` all remain on the original included chapter. `WorkspaceViewModel.TrySwitchChapterAsync` explicitly restores the previous selection when `node.IsExcluded`. |
| 11. Inspect the sidebar XAML/converter smoke assertions and confirm excluded rows bind to the dedicated muted/italic styling and only expose the `Include in book` context-menu action. | artifact | PASS | `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` source-asserts `SidebarView.axaml` contains `Include in book`, `Node.IsExcluded` visibility binding, `NodeKindIsChapterAndPresentConverter`, `NodeKindAndMissingToForegroundConverter`, the `excluded` badge, and `BoolToFontStyleConverter`. `NodeKindAndMissingToForegroundConverter` returns a distinct `ExcludedBrush`, and `SidebarView.axaml` binds excluded titles to italic font style plus muted foreground. |

## Overall Verdict

PASS — All automatable S02 sidebar exclusion checks are satisfied by the integration tests, smoke assertions, and source inspection currently present on the slice.

## Notes

- Verification lane restrictions prevented re-running native `dotnet` commands through `bash`, and `gsd_exec` reproduced the known MEM008 WSL/Windows NuGet `Value cannot be null (Parameter 'path1')` failure for direct `dotnet` execution.
- To avoid inventing runtime results, this assessment relies on objective slice-local evidence already recorded in `.gsd/milestones/M005/slices/S02/tasks/T02-SUMMARY.md`, `T03-SUMMARY.md`, and `T04-SUMMARY.md`, which document fresh native PowerShell runs for:
  - `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests --verbosity minimal` → pass
  - `dotnet test Hymnal.slnx --nologo --verbosity minimal` → pass
  - `dotnet build src/Hymnal/Hymnal.csproj --nologo` → pass
- Edge-case coverage present in the checked-in tests/source includes: part-folder insertion ordering for excluded nodes, append-after-active ordering for excluded nodes outside active parts, duplicate-free reloads after include, and notification-preserving failure paths for failed include/exclude operations.
