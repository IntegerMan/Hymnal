---
estimated_steps: 8
estimated_files: 3
skills_used: []
---

# T03: Add sidebar rename affordances

Planner context: use skills tdd and verify-before-complete. Why: the slice demo requires rename affordances from the sidebar, not only hidden ViewModel commands. The View should collect a new title or path, call WorkspaceViewModel commands, and keep business logic out of code-behind.

Do:
1. Update `src/Hymnal/Views/SidebarView.axaml` context menus to expose `Rename…` for included present chapters and Part nodes. Do not show rename for excluded projected nodes or missing entries.
2. Update `src/Hymnal/Views/SidebarView.axaml.cs` to open a minimal rename prompt or flyout, prefilled with the current `Node.Title`, and execute the appropriate WorkspaceViewModel rename command. Code-behind may collect UI text and route commands, but all path-building, validation, filesystem mutation, Book.txt writes, and reload orchestration must remain in `WorkspaceViewModel` and Core services.
3. Ensure keyboard and mouse flows are practical: Enter confirms, Escape or Cancel closes without mutation if using a dialog; right-click context menus continue to work with existing include, exclude, remove, target, and drag handlers.
4. Add or extend smoke tests in `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` to prove the rename menu item is present for included chapters and Part nodes and hidden for excluded or missing rows. If the test framework cannot interact with the dialog, keep command routing covered by T02 and smoke-test visibility or control loading here.
5. Keep drag reorder handlers unchanged except for any event-handling conflicts introduced by the rename UI.

Done when: a user can right-click an included chapter or Part in the sidebar, choose Rename, submit a new title, and the View delegates to WorkspaceViewModel without any direct filesystem writes.

## Inputs

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

## Expected Output

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter SidebarViewSmokeTests --verbosity minimal

## Observability Impact

No new telemetry; failure visibility remains through WorkspaceViewModel notifications surfaced from Core Result errors.
