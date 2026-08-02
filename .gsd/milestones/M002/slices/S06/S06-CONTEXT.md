---
id: S06
milestone: M002
status: draft
---

# S06: Operational Benchmark Evidence — Context

## Goal

Produce xUnit timing tests that measure live word-count latency on a 10,000-word chapter and cold-start recalculation time across a 100-chapter workspace, with pass/fail assertions and results captured in the S06 summary artifact for re-validation.

## Why this Slice

The M002 validation round found the Operational verification class incomplete: the milestone context requires word-count updates ≤ 500ms and workspace cold-start ≤ 5s for 100 chapters, but no artifact in S01–S05 includes measured numbers. S06 closes that gap with automated test evidence that runs under `dotnet test Hymnal.sln` and leaves a durable proof record. S07 (Validation Alignment and Closure) depends on this evidence to close the Operational class before the next validation pass.

## Scope

### In Scope

- A new `tests/Hymnal.Core.Tests/Performance/` folder with a `WordCountPerformanceTests.cs` file.
- **Test 1 — Live word count latency:** Call `WordCountService.CountWords()` on a 10,000-word synthetic string; assert elapsed time < 200ms (the 300ms debounce accounts for the remaining 300ms of the 500ms budget).
- **Test 2 — Background recalculation cold-start:** Create a synthetic 100-chapter workspace in a temp directory (100 `.md` files of ~500 words each + `Book.txt`), run `WordCountService.CountWords()` over all 100 files sequentially (conservative baseline matching the background `Task.Run` workload), assert total elapsed time < 5,000ms; clean up temp dir in `finally`.
- Capture the measured timings in the S06 summary artifact for use by the validator.
- If either assertion fails: record the actual measurement in the S06 summary, add a remediation note, and flag the gap for S07 rather than fixing it inline.

### Out of Scope

- BenchmarkDotNet or a separate Hymnal.Benchmarks project.
- End-to-end UI timing (the live Avalonia app cannot run headlessly in this harness).
- Any performance fixes — S06 is evidence-gathering only; fixes belong in S07 if measurements exceed thresholds.
- Timing the 300ms debounce itself — that is a framework constant, not a measured path.
- Measuring word-count history writes, phase persistence, or DI container initialization.

## Constraints

- Tests must live inside `Hymnal.Core.Tests` and run with `dotnet test Hymnal.sln -nologo` — no new project, no separate build step.
- The 100-chapter fixture is created in `Path.GetTempPath()` and deleted in `try/finally` — nothing committed to the repo fixture folder.
- `WordCountService` must remain UI-agnostic (no Avalonia refs); the test must not require a running app.
- Timing assertions use `System.Diagnostics.Stopwatch`; no third-party timing libraries.
- `Hymnal.Core.csproj` must retain zero Avalonia package references after S06.

## Integration Points

### Consumes

- `src/Hymnal.Core/Services/WordCountService.cs` — the service under test; instantiated directly with no mocks.
- `tests/Hymnal.Core.Tests/` — existing test project where the new `Performance/` subfolder is added.

### Produces

- `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs` — two timing tests with `Stopwatch` assertions.
- `S06-SUMMARY.md` — captures the actual measured milliseconds for both tests as Observable Truths, confirming whether thresholds were met or flagging a remediation note for S07.

## Open Questions

- **Sequential vs. parallel measurement for the 100-chapter test:** The background recalc fires parallel `Task.Run` per chapter; measuring sequentially is more conservative and avoids thread-pool variability in a test harness. Sequential baseline is the plan unless measurements come in surprisingly close to 5s, in which case a parallel measurement could also be recorded for completeness.
- **Flakiness on constrained CI:** Timing assertions in test suites can fail on slow CI runners. If the 200ms / 5,000ms limits prove too tight on Windows CI, thresholds can be relaxed to 500ms / 8,000ms in S07 with a note — confirmed acceptable since the goal is evidence capture, not strict performance regression gating.
