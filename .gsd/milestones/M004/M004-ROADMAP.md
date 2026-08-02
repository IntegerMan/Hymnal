# M004: Corkboard, Supplemental Docs, and Git Panel

**Vision:** Complete the Early V1 author loop by adding a Plan-mode corkboard, sidebar-managed supplemental documents, and a persistent Git commit surface so authors can review manuscript progress, keep reference material beside the manuscript, and commit progress without leaving Hymnal.

## Success Criteria

- Plan mode renders manuscript order as Part dividers plus chapter cards showing title, status, word count, target percentage when present, and phase dates when present.
- Clicking a corkboard card opens the selected chapter in the existing editor and activates Write mode.
- The sidebar DOCS section supports creating folders and files under .hymnal-data/docs, opens docs in the existing editor, and preserves content through atomic saves and workspace reopen.
- A Git toolbar group appears only when system Git and a repository are detected, displays branch and uncommitted count, and keeps the count fresh after file saves and Git operations.
- The Commit dialog stages all changes, uses the default timestamped message, supports Commit only and Commit & Push, and surfaces raw Git stderr through notifications on failure.
- Unit and manual smoke evidence cover CardViewModel projection, EditorViewModel arbitrary-file dirty transitions, ProcessGitService behavior, and a real local-repo Git workflow.

## Slices

- [x] **S01: Plan Mode Corkboard Cards** `risk:Shell-mode and projection risk: Plan mode is reserved but unused, and cards must live-project existing chapter metadata without creating a second manuscript model.` `depends:[]`
  > After this: Open a workspace, click Plan, see Part dividers plus cards for each chapter with status, word count, optional target percentage and phase dates, then click a card and verify Write mode opens that chapter.

- [x] **S02: Supplemental Docs Sidebar and Editor Path** `risk:Editor lifecycle risk: arbitrary docs need the existing dirty-state, watcher, and atomic-save behavior while `ActiveNode` is null.` `depends:[S01]`
  > After this: Create a docs file from the DOCS section, type content, save it through the editor, reopen the workspace, and verify the doc remains visible with intact content.

- [x] **S03: Git Toolbar Commit Workflow** `risk:Subprocess and watcher risk: Git detection, status counting, commit/push operations, and live refresh must work across local environments without crashing or hiding errors.` `depends:[S01,S02]`
  > After this: Open a Git-backed workspace with uncommitted changes, verify branch and change count appear, use Commit only and Commit & Push paths against a local repo, and verify the count refreshes or stderr appears in a notification.

## Boundary Map

## Boundary Map

| Boundary | Slices | Contract |
|---|---|---|
| Shell navigation | S01, S02 | Reuse `ShellMode.Plan` for Corkboard and activate `ShellMode.Write` when a doc or chapter is opened. |
| Manuscript data projection | S01 | Read existing `ChapterViewModel` status, word count, target, and phase data without duplicating persistence. |
| Editor file lifecycle | S02 | `EditorViewModel.OpenArbitraryFileAsync` sets `ActiveNode` null, updates `ActiveFilePath`, prompts for dirty state, and uses existing atomic save path. |
| Supplemental docs tree | S02 | Separate doc tree model under `.hymnal-data/docs/`; do not expand `ChapterNode` for docs. |
| System Git process | S03 | `IGitService`/`ProcessGitService` invokes system Git, returns structured success/output/error, and never swallows stderr. |
| Git status watcher | S03 | Workspace-root `FileSystemWatcher` debounces updates and disposes with the Git panel ViewModel. |
