---
id: T03
parent: S03
milestone: M002
key_files:
  - src/Hymnal/Views/Editor/ValidationMargin.cs
  - src/Hymnal/Views/EditorView.axaml.cs
key_decisions:
  - AbstractMargin absent in AvaloniaEdit 12.0.0 — used IBackgroundRenderer + BackgroundRenderers.Add() fallback as specified in task plan; documented with compiler comment and captured as MEM033
  - Refresh called from OnEditorTextChanged (pull model) rather than self-subscribing to VisualLinesChanged — simpler and avoids the timing issues of subscribing inside a background renderer lifecycle
duration: 
verification_result: passed
completed_at: 2026-05-31T03:42:07.240Z
blocker_discovered: false
---

# T03: Created ValidationMargin (IBackgroundRenderer fallback) that draws advisory amber dots for two Markua patterns and wired it into EditorView

**Created ValidationMargin (IBackgroundRenderer fallback) that draws advisory amber dots for two Markua patterns and wired it into EditorView**

## What Happened

AbstractMargin is not present in Avalonia.AvaloniaEdit 12.0.0, confirmed by a quick compile probe. Used the documented fallback: IBackgroundRenderer registered via TextArea.TextView.BackgroundRenderers.Add().

Created src/Hymnal/Views/Editor/ValidationMargin.cs implementing IBackgroundRenderer:
- Pattern 1: blank line immediately before a {sample: true} attribute block — marks the blank line as advisory.
- Pattern 2: any attribute block on a line containing an unrecognised Markua key (checked against a 20-key hard-coded valid set) — marks that line as advisory.
- Draw() iterates TextView.VisualLines, subtracts ScrollOffset.Y for viewport-relative y-coordinates, and paints an amber (#FCD34D) filled ellipse (radius 4) at x=6 centered vertically in each advisory line.
- Refresh(IDocument, TextView) rebuilds _advisoryLines then calls InvalidateVisual() — called from OnEditorTextChanged.
- All exceptions swallowed silently at every level (per-line scan, per-line render, top-level).

Modified src/Hymnal/Views/EditorView.axaml.cs:
- Added _validationMargin field.
- In OnAttachedToVisualTree: instantiate and register (wrapped in try/catch).
- In OnEditorTextChanged: call _validationMargin.Refresh(document, textView) after VM text update.
- In OnDetachedFromVisualTree: remove from BackgroundRenderers and null the field before disposing composites.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo → Build succeeded, 0 errors. dotnet test Hymnal.sln -nologo → Passed: 57, Failed: 0, Skipped: 0.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 7590ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 57 passed, 0 failed | 12000ms |

## Deviations

AbstractMargin fallback was anticipated by the task plan and executed as specified — not a deviation. Refresh is triggered from OnEditorTextChanged rather than a VisualLinesChanged subscription; the task plan noted 'if the margin self-subscribes via VisualLinesChanged, no extra wiring is needed' as an option — pull model from TextChanged is equivalent and simpler.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/Editor/ValidationMargin.cs`
- `src/Hymnal/Views/EditorView.axaml.cs`
