---
estimated_steps: 9
estimated_files: 5
skills_used: []
---

# T04: Add excluded styling and integration smoke coverage

Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.

Why: The slice demo is user-facing: authors must be able to recognize excluded chapters and toggle them from the sidebar. This task closes the presentation and full-regression proof without adding new structural scope.

Do:
1. Update sidebar XAML/converters/styles so excluded chapter nodes render distinctly from normal, missing, and Part nodes. Prefer a dim/italic/label treatment using existing theme resources; do not introduce a new theme overhaul.
2. Show context-menu options conditionally: excluded nodes get `Include in book`; included chapter nodes get `Exclude from book`; missing nodes keep their remove affordance. Avoid duplicate include/exclude menu items for the same row.
3. Add a source-level or Avalonia smoke test that verifies `SidebarView` declares the include action and excluded-state binding/converter. If an existing view smoke pattern can instantiate the view cheaply, use it; otherwise keep it as a focused source assertion in the test suite.
4. Run focused WorkspaceViewModel tests, the full Core test project, and app build. Use `Hymnal.slnx` for solution-level verification in normal executor shells; if gsd_exec closure later hits MEM008, record the task-session evidence.
5. Update any affected tests for existing Corkboard/Sidebar behavior if constructor injection changes require fakes.

Done when: the user-facing sidebar state is visibly distinguishable in XAML, the command menu maps to WorkspaceViewModel commands, and full verification passes without regressions.

## Inputs

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `src/Hymnal/Views/Converters/NodeKindAndMissingToForegroundConverter.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`

## Expected Output

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `src/Hymnal/Views/Converters/NodeKindAndMissingToForegroundConverter.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`

## Verification

dotnet test Hymnal.slnx --nologo

## Observability Impact

No new telemetry; proof is the UI binding plus tests that inspect sidebar state, Book.txt, exclusions manifest, and captured notification errors.
