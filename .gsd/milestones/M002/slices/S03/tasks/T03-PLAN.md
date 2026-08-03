---
estimated_steps: 23
estimated_files: 2
skills_used: []
---

# T03: ValidationMargin: AbstractMargin advisory gutter for two Markua patterns

**Why:** The ValidationMargin surfaces advisory Markua issues as a gutter dot without ever blocking the editor or throwing. It must extend `AbstractMargin` (AvaloniaEdit) and be registered in `EditorView.axaml.cs` against `PART_Editor.TextArea.LeftMargins`.

**AbstractMargin API note (untested in this codebase):** `AvaloniaEdit.Rendering.AbstractMargin` is the standard extension point for custom left-margin renderers. It exposes `TextView` and `RenderSize` and calls `MeasureOverride`/`ArrangeOverride` / `OnRender`. If build fails with AbstractMargin, fall back to implementing `IBackgroundRenderer` with a `Draw(TextView, DrawingContext)` method and registering via `PART_Editor.TextArea.TextView.BackgroundRenderers.Add()` instead. Use a compiler-directive comment to document the fallback.

**Steps:**
1. Create `src/Hymnal/Views/Editor/` directory (new subdirectory under Views).
2. Create `src/Hymnal/Views/Editor/ValidationMargin.cs`:
   - Class: `internal sealed class ValidationMargin : AbstractMargin` (or `IBackgroundRenderer` fallback)
   - Namespace: `Hymnal.Views`
   - Fields: `_advisoryLinePen` (amber Pen, `#FCD34D`, StrokeThickness 1.5); `_advisoryBrush` (SolidColorBrush amber); `_dotRadius = 4.0`; list of advisory line numbers `_advisoryLines = new HashSet<int>()`
   - `Refresh(IDocument document)` method: scans all lines for the two patterns, rebuilds `_advisoryLines`, then calls `InvalidateMeasure()` / `InvalidateArrange()` / triggers a redraw
   - Pattern 1 detection — **blank line before {sample: true}**: iterate lines; if line N is blank and line N+1 matches regex `^\{[^}]*sample\s*:\s*true[^}]*\}` (case-insensitive), mark line N as advisory
   - Pattern 2 detection — **unrecognised Markua attribute key**: scan each line for attribute blocks matching regex `\{([^}]+)\}`; for each key=value pair, check if the key is in a hard-coded valid-key set (`{"sample", "id", "class", "width", "height", "alt", "caption", "title", "type", "format", "line-numbers", "crop", "start-line", "end-line", "lang", "target", "aside", "blurb", "nonum", "pagebreak"}`); any unrecognised key marks that line as advisory
   - `MeasureOverride`: return `new Size(_dotRadius * 2 + 4, 0)` so the margin gets width
   - `OnRender(DrawingContext drawingContext)`: iterate visible lines in `TextView.VisualLines`; for each whose document line number is in `_advisoryLines`, draw a filled circle centered vertically in the line at x=margin-center
   - Subscribe to `TextView.VisualLinesChanged` in `OnTextViewChanged` override; call `Refresh(TextView.Document)` when fired; swallow all exceptions
   - All rendering and scanning code in try/catch that swallows silently
3. Modify `src/Hymnal/Views/EditorView.axaml.cs`:
   - Add a field `private ValidationMargin? _validationMargin;`
   - In `OnAttachedToVisualTree`, after `LoadMarkuaHighlighting()`: instantiate `_validationMargin = new ValidationMargin();` and add to `PART_Editor.TextArea.LeftMargins` (or fallback `BackgroundRenderers` list if using IBackgroundRenderer). Wrap in try/catch that calls `notifications.ShowInfo()` only in debug; silently swallow in release.
   - Subscribe to `PART_Editor.TextChanged` (after initial load) to call `_validationMargin.Refresh(PART_Editor.Document)` — or if the margin self-subscribes via VisualLinesChanged, no extra wiring is needed
   - In `OnDetachedFromVisualTree`, if using LeftMargins: `PART_Editor.TextArea.LeftMargins.Remove(_validationMargin)`
4. Run `dotnet build src/Hymnal/Hymnal.csproj -nologo`. If AbstractMargin import causes build errors (missing members), switch to `IBackgroundRenderer` pattern per the fallback note above.
5. Run `dotnet test Hymnal.sln -nologo` — all 57 tests must pass (ValidationMargin is UI-only; no new tests needed).

**Done when:** `dotnet build src/Hymnal/Hymnal.csproj -nologo` exits 0; `dotnet test Hymnal.sln -nologo` passes 57 tests, 0 failures.

## Inputs

- `src/Hymnal/Views/EditorView.axaml.cs`
- `src/Hymnal/Views/EditorView.axaml`

## Expected Output

- `src/Hymnal/Views/Editor/ValidationMargin.cs`
- `src/Hymnal/Views/EditorView.axaml.cs`

## Verification

dotnet test Hymnal.sln -nologo
