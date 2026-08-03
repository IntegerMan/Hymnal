# S02: Sidebar excluded chapters and include toggle — UAT

**Milestone:** M005
**Written:** 2026-06-18T08:15:02.585Z

# UAT Type
Automated integration UAT with source/smoke verification; no desktop-human interaction required for this slice.

# Preconditions
- Use a temp manuscript workspace containing `Book.txt`, at least one included chapter, and at least one markdown file absent from `Book.txt` but listed in `.hymnal-data/exclusions.json`.
- WorkspaceViewModel is created with the real `BookTxtStructureService`, `ExclusionManifestService`, and orphan discovery service.

# Steps
1. Load the workspace and inspect `WorkspaceViewModel.Nodes` / `VisibleNodes`.
2. Confirm the manifest-backed orphan markdown file appears in the sidebar as a chapter node with `IsExcluded = true`.
3. Confirm an ordinary orphan markdown file that is not listed in `exclusions.json` does not appear as an excluded sidebar node.
4. Trigger the sidebar include action for the excluded node.
5. Verify the chapter is inserted back into `Book.txt` at the sidebar-derived position and removed from `.hymnal-data/exclusions.json`.
6. Reload the workspace through a fresh WorkspaceViewModel instance and confirm the chapter is now an ordinary included sidebar node.
7. Trigger the sidebar exclude action for an included present chapter.
8. Verify the chapter is removed from `Book.txt`, added to `.hymnal-data/exclusions.json`, and projected back into the sidebar as excluded after reload.
9. Trigger the remove action for a missing Book.txt entry and verify the stale entry is removed without adding it to `exclusions.json`.
10. Attempt to select an excluded node and verify the current editor selection is preserved rather than opening the excluded file in the editor.
11. Inspect the sidebar XAML/converter smoke assertions and confirm excluded rows bind to the dedicated muted/italic styling and only expose the `Include in book` context-menu action.

# Expected Outcomes
- Excluded chapters remain visible in the sidebar but are clearly styled as excluded.
- Excluded chapters are not treated as missing Book.txt entries.
- Including an excluded chapter updates both `Book.txt` and `exclusions.json` and survives reload.
- Excluding an included chapter updates both `Book.txt` and `exclusions.json` and survives reload.
- Removing a missing entry cleans up Book.txt state without creating a manifest exclusion.
- Excluded nodes do not open in the editor and do not disturb the active selection.

# Edge Cases
- Excluded chapter under an existing Part is inserted after the last active sibling in that Part when included.
- Excluded chapter outside any active Part is appended after active entries without duplicating nodes on reload.
- Failed include/exclude operations preserve current sidebar state and route user-visible errors through `INotificationService.ShowError`.

# Evidence
- `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests --verbosity minimal`
- `dotnet test Hymnal.slnx --nologo --verbosity minimal`
- `dotnet build src/Hymnal/Hymnal.csproj --nologo`
