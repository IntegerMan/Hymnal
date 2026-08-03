# S07 Research — Validation Alignment and Closure

## Depth
Light-to-targeted research. This slice is documentary closure work, not feature implementation: requirement metadata alignment, one confirmatory code inspection, and a milestone validation artifact. `gsd_milestone_status` shows **S07 is still pending with 0 planned tasks**, so the planner should create a thin execution plan rather than assume task files already exist.

## Requirement Closure Map

### R003 — primary S07 closure target
- Current state in `.gsd/REQUIREMENTS.md`: **active / Validation: unmapped**.
- Existing evidence is already sufficient for a requirement update:
  - `.gsd/milestones/M002/slices/S01/S01-SUMMARY.md` marks **R003 validated**.
  - Verification evidence there is explicit: `dotnet build src/Hymnal/Hymnal.csproj -nologo` passed, `ChapterRegistryServiceTests` passed **6/6**, and `PhaseDataServiceTests` passed **4/4**.
  - The same summary states the sidebar renders chapter status affordances and preserves UUID-backed continuity across rename/reload.
- Planner implication: execution should update the requirement record, not re-run code or tests.

### R004 — already validated, include in traceability only
- Current state in `.gsd/REQUIREMENTS.md`: already carries an S05 validation note.
- Keep it in `M002-VALIDATION.md` as a mapped requirement, but **do not spend a task re-validating it** unless the doc needs wording cleanup.
- Supporting evidence chain:
  - `.gsd/milestones/M002/slices/S02/S02-SUMMARY.md` validates the live count / targets / rollup contract.
  - `.gsd/milestones/M002/slices/S04/S04-SUMMARY.md` advances the live target/proximity wiring.
  - `.gsd/milestones/M002/slices/S05/S05-SUMMARY.md` records the final code-inspection validation.

### R014 — primary S07 closure target, but evidence is split across milestones
- Current state in `.gsd/REQUIREMENTS.md`: **active / Validation: unmapped**.
- Important nuance: **R014 is broader than S06 alone**. The requirement text includes platform/publish constraints plus performance constraints:
  - Windows + Linux first-class targets
  - self-contained .NET 10 publish
  - cold start under 5s
  - `Book.txt` parse under 2s for 100 chapters
  - word count update under 500ms
- Existing proof is split:
  - `.gsd/milestones/M001/slices/S01/S01-SUMMARY.md` advances the platform/publish side of R014.
  - `.gsd/milestones/M001/M001-VALIDATION.md` records the legacy validation rationale for **CI matrix / publish**, **cold start under 5s**, and **Book.txt parse under 2s**.
  - `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md` adds the missing measured M002 runtime evidence: **1 ms** live word-count latency for 10,000 words and **82 ms** cold-start recalculation for 100 chapters, plus **59/59** full-suite pass.
- Planner implication: the execution slice should write **one combined R014 proof note** that cites both M001 and M002 evidence. Do **not** cite only S06, or the update will under-document the platform/publish/parse parts of the requirement.

### R002 — touched by M002, but do not validate here
- Current state in `.gsd/REQUIREMENTS.md`: **active / unmapped**.
- `.gsd/milestones/M002/slices/S03/S03-SUMMARY.md` shows M002 delivered the advisory validation gutter, but R002 is still the broader editor requirement owned by M001/S03.
- Best use in S07: mention R002 in the milestone validation document as **partial support / not closed by this slice**. Avoid broadening S07 into a full editor requirement re-validation task.

## Remediation Story for Earlier Non-Pass Assessments

### S01 assessment FAIL → resolved later, no reopen needed
- `.gsd/milestones/M002/slices/S01/S01-ASSESSMENT.md` failed because `BoolToOpacityConverter` was referenced but not registered.
- The later implementation evidence shows this was resolved in the sidebar/resource work (see S02 follow-on summaries and `SidebarView.axaml` resource additions).
- Planner implication: record this as **historical assessment remediated by later slice work**, not as an open defect.

### S02 assessment PARTIAL → environment limitation, not product defect
- `.gsd/milestones/M002/slices/S02/S02-ASSESSMENT.md` is partial because the desktop UI could not be driven from the browser-only harness.
- The underlying contracts were already proved by tests, and S05 converted the milestone closeout into an explicit closure record with operator steps.
- Planner implication: write this as **harness limitation absorbed by S05 evidence**, not as a reason to reopen S02.

### S03 assessment FAIL → directly remediated by S04
- `.gsd/milestones/M002/slices/S03/S03-ASSESSMENT.md` failed due startup `NullReferenceException` in `ChapterInfoViewModel.HasTarget`.
- `.gsd/milestones/M002/slices/S04/S04-SUMMARY.md` explicitly says the OAPH wiring was replaced with plain backing fields and live chapter subscriptions, eliminating the crash.
- Current code confirms the fix is present:
  - `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` has plain backing fields for `ProximityFill` and `HasTarget`.
  - The live subscriptions are present at the current file lines reported by `rg`: `chapterVm.WhenAnyValue(x => x.HasTarget)` and `chapterVm.WhenAnyValue(x => x.ProximityFill)`.
- Planner implication: S07 should record **S03 limitation closed by S04** and does not need a reopen.

## Confirmed Code/Artifact Landscape

### `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
Use this as the single confirmatory inspection target.
- The current file has plain `double _proximityFill` / `bool _hasTarget` backing fields, not OAPHs.
- `OnActiveNodeChanged` resets `HasTarget = false; ProximityFill = 0.0` when no node is active.
- The per-chapter subscription block now includes live subscriptions from `ChapterViewModel` for both `HasTarget` and `ProximityFill`.
- `SyncFromChapterVm` also copies both values immediately on chapter switch.
- This is enough to explicitly rebut S03's old "stubbed 0.0" limitation in the validation document.

### `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
This file holds the strongest code-inspection evidence for rename continuity and post-save word-count persistence.
- Registry hydration path first reconciles orphans, then performs **title-aware orphan rename matching**, then calls `AssignUuid` for active nodes.
- The relevant block is around the current `rg` hits near lines **441-470**.
- The save pipeline subscribes to `EditorViewModel.Saved`, recomputes count from editor text, updates the active `ChapterViewModel`, refreshes totals, and appends word-count history.
- Use this file as the implementation anchor when writing the R003/R004/R014 traceability table.

### `src/Hymnal/Views/Editor/ValidationMargin.cs` + `src/Hymnal/Views/EditorView.axaml.cs`
These files are enough to explain M002's contribution to R002 without claiming full R002 validation.
- `ValidationMargin` implements the two advisory patterns:
  1. blank line before `{sample: true}` block
  2. unrecognised Markua attribute key
- All exceptions are swallowed silently.
- `EditorView.axaml.cs` registers the renderer through `TextArea.TextView.BackgroundRenderers.Add(...)`, matching the AvaloniaEdit fallback constraint.

## Natural Seams for a Thin S07 Execution Plan

1. **T01 — Evidence collation + requirement note drafting**
   - Inputs: `REQUIREMENTS.md`, S01/S04/S05/S06 summaries, M001 validation artifact, current `ChapterInfoViewModel.cs`.
   - Output: exact wording for R003 and R014 validation notes, plus a remediation matrix for S01/S02/S03.

2. **T02 — Write `M002-VALIDATION.md`**
   - Content should include:
     - requirement traceability rows for **R003 / R004 / R014**
     - completion-class summary (**Contract / Integration / Operational / UAT**) 
     - explicit remediation story for **S01 / S02 / S03**
     - explicit note that `ChapterInfoViewModel.ProximityFill` is no longer stubbed
     - explicit note that M002 only partially advances R002

3. **T03 — Apply metadata updates and attempt validation pass**
   - Update requirement records for **R003** and **R014**.
   - If the workflow includes `gsd_validate_milestone`, run it only after the document and requirement notes exist.

## First Proof / Highest-Risk Detail
The highest-risk mistake is **under-scoping R014**. Its proof must combine **M001 platform/publish/parse evidence** with **M002 S06 measured latency/cold-start evidence**. A note that cites only S06 would leave the traceability story incomplete and invite another validation round.

## Verification Strategy for S07 Execution
No source-code verification reruns are necessary if the slice remains documentary.

Use structural checks instead:
- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` still shows live `HasTarget` / `ProximityFill` subscriptions and no local stub/OAPH.
- `.gsd/REQUIREMENTS.md` no longer shows `Validation: unmapped` for **R003** and **R014**.
- `.gsd/milestones/M002/M002-VALIDATION.md` exists and includes:
  - requirement table for **R003 / R004 / R014**
  - remediation rows for **S01 / S02 / S03**
  - desktop-harness limitation note
  - explicit statement that S04 closed the old S03 proximity-fill defect

## Known Gotchas
- Memory query surfaced a reusable validator mismatch: desktop milestones can still be downgraded because the validator expects browser-style runtime evidence even when the product is Avalonia desktop. Treat a `needs-attention` result from a later validation pass as a **possible validator/tooling mismatch**, not automatically as a product defect.
- Keep all writes tool-driven. Requirement updates must go through the GSD requirement tool; the milestone artifact should be written through the GSD summary/artifact path rather than hand-editing files.

## Skill Discovery
Directly relevant installed skill coverage is thin for Avalonia/ReactiveUI-specific closure work.

Promising external Avalonia skills discovered (not installed):
- `npx skills add sickn33/antigravity-awesome-skills@avalonia-viewmodels-zafiro`
- `npx skills add markpitt/claude-skills@avalonia`

Search result notes:
- `npx skills find "Avalonia"` returned several Avalonia-specific options, with the two above among the strongest fits.
- `npx skills find "ReactiveUI"` returned **no skills found**.
