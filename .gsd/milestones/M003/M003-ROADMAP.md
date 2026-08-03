# M003: Gantt View and Project Management

**Vision:** Provide a visual project management view that gives the author an immediate sense of schedule, progress, and upcoming deadlines, fully integrated with the manuscript metadata.

## Success Criteria

- Author can view a timeline of manuscript progress.
- Author can edit phase dates without leaving the Gantt view.
- Part nodes display accurate rollup summaries of their child chapters.

## Slices

- [x] **S01: Gantt Canvas Renderer Foundation** `risk:high` `depends:[]`
  > After this: A new 'Gantt' tab/view is available, rendering a read-only time axis and phase boxes for all chapters based on their saved `PhaseData`.

- [x] **S02: Part Rollup Rows and Progress Fill** `risk:medium` `depends:[S01]`
  > After this: The Gantt view now includes Part nodes, showing summary phase boxes that span the min/max dates of their child chapters, along with progress fill.

- [x] **S03: Inline Date Editing** `risk:low` `depends:[S01,S02]`
  > After this: Users can click on phase dates in the Gantt view to edit them inline, persisting changes back to the registry.

## Boundary Map

### S01 → S02

Produces:
- `GanttViewModel` exposing an `ObservableCollection<GanttRowViewModel>` derived from `ManuscriptModel`.
- `GanttCanvas` custom Avalonia control that renders `GanttRowViewModel`s.

Consumes:
- `ManuscriptModel` and `IChapterRegistryService` from previous milestones.

### S02 → S03

Produces:
- Part rollups included in the `ObservableCollection<GanttRowViewModel>` with aggregated start/end dates.
- Rendering logic for summary boxes.

Consumes:
- `GanttRowViewModel` collection from S01.
