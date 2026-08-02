---
id: S03
milestone: M002
status: draft
---

# S03: Chapter Info Pane and Validation Margin — Context (DRAFT)

## Goal
Wire the Chapter Info pane (F3) into a refactored right-rail shared host showing status picker, phase dates, live word count, and target for the open chapter; and add a standalone ValidationMargin for two advisory Markua patterns.

## Key Decisions Captured
- Right rail: stacked vertically (both panes open simultaneously), both collapsed by default
- Status picker: ComboBox dropdown with colour swatch
- Phase dates: Avalonia DatePicker controls
- Target: editable in Chapter Info pane
- Validation: two patterns only (blank line before {sample: true} heading; unrecognised attribute key)
