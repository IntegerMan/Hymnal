---
id: S06
parent: M002
milestone: M002
status: researched
depth: targeted
---

# S06: Operational Benchmark Evidence

## Summary

One additive xUnit test file is enough to close this slice. The production timing seams already exist: `EditorViewModel` applies a fixed 300 ms debounce before calling `WordCountService.CountWords()`, and `WorkspaceViewModel` performs background recalculation by reading chapter files and calling the same service inside `Task.Run`. There is no existing performance harness or `BenchmarkDotNet` project, but `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj` already includes xUnit and the .NET test SDK, so S06 can stay inside the current test project with no new dependencies.

## Requirement Lens

- Primary support: the milestone Operational completion criteria behind **R004** evidence, specifically proving that live word-count work stays within the 500 ms end-user budget and 100-chapter cold-start recalculation stays within 5 s.
- Secondary impact: none on **R003** or **R002** behavior. This slice is evidence capture only, not feature work.
- Existing validation note: R004 already has code-inspection validation from S05; S06 adds the missing measured runtime evidence.

## Recommendation

- Add `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs`.
- Keep the tests UI-free: instantiate `WordCountService` directly.
- For live latency, benchmark only the hot compute segment on a 10,000-word in-memory string. Warm once before timing to reduce JIT noise, record the measured milliseconds, and assert `< 200 ms` so the existing 300 ms debounce still leaves the full user-facing path under 500 ms.
- For cold start, create a temp 100-chapter workspace, then measure only the read+count loop (`File.ReadAllTextAsync` + `CountWords`) across all chapter files. Assert `< 5,000 ms`.
- Follow the **verify-before-complete** rule: S06 is not done until the current-session test run has emitted actual milliseconds and those exact numbers are copied into the slice summary artifact.
- If a benchmark fails, do **not** optimize inline in S06. Preserve the measured numbers, note the miss, and hand remediation to S07.

## Implementation Landscape

### `src/Hymnal.Core/Services/WordCountService.cs`
- `CountWords(string? content)` is a straight O(n) tokenizer.
- It splits the whole input by newline, skips any trimmed line starting with `{`, and splits remaining lines on whitespace.
- This is allocation-heavy (`Split` per line) but simple and deterministic, which makes it a good target for measured evidence.

### `src/Hymnal/ViewModels/EditorViewModel.cs`
- Live word count is wired as:
  - `WhenAnyValue(x => x.Text)`
  - `.Throttle(TimeSpan.FromMilliseconds(300), TaskPoolScheduler.Default)`
  - `.Select(t => _wordCountService.CountWords(t))`
  - `.ObserveOn(AvaloniaScheduler.Instance)`
- S06 should benchmark **only** `CountWords`, not the ReactiveUI throttle or UI-thread marshal.

### `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- Workspace load launches a background task per chapter.
- Each task reads the chapter text from disk, calls `_wordCountService.CountWords(content)`, then marshals the update back to the UI thread.
- A UI-free benchmark can faithfully mirror the expensive part of this path without instantiating Avalonia or the view-model graph.

### `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`
- Already references `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, and `coverlet.collector`.
- No `BenchmarkDotNet` dependency exists, which aligns with the slice constraint to avoid a separate benchmark project.

### `tests/Hymnal.Core.Tests/Services/WordCountServiceTests.cs`
- Existing test style is simple xUnit with direct `WordCountService` construction.
- File-system tests in the project use temp directories plus `try/finally` cleanup.
- S06 should follow that style instead of introducing a new fixture framework.

## Benchmark Design

### 1) Live word-count latency on 10,000 words

Recommended test shape:
- Build one deterministic 10,000-word string entirely in memory.
- Call `CountWords` once before timing as a warm-up.
- Start `Stopwatch`, call `CountWords`, stop `Stopwatch`.
- Assert both correctness and performance:
  - returned count is `10_000`
  - elapsed is `< 200 ms`
- Emit the measured milliseconds to test output so the summary writer can quote the real number.

Why this is the right seam:
- It directly measures the compute work behind the live counter.
- It avoids filesystem, UI thread, and scheduler noise.
- The remaining 300 ms in the 500 ms milestone budget is already structurally consumed by the debounce constant in `EditorViewModel`.

### 2) Cold-start recalculation across a 100-chapter workspace

Recommended test shape:
- Create a temp workspace under `Path.GetTempPath()`.
- Write:
  - `Book.txt`
  - 100 chapter files such as `chapter-001.md` ... `chapter-100.md`
- Keep file creation **outside** the measured window.
- Time only the conservative baseline loop:
  - `await File.ReadAllTextAsync(path)`
  - `_svc.CountWords(content)`
- Assert:
  - 100 files processed
  - total counted words match the synthetic corpus
  - elapsed is `< 5,000 ms`
- Delete the temp workspace in `finally`.

Why sequential is acceptable:
- The production path fans out via `Task.Run`, so sequential timing is a conservative lower-complexity baseline.
- It avoids thread-pool variability and makes failures easier to interpret.
- If the sequential result lands uncomfortably close to 5 s, S07 can add a secondary parallel measurement, but S06 does not need that complexity up front.

## Natural Seams

1. **Add benchmark test file**
   - Create `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs`.
   - Add local helpers for synthetic text generation and temp workspace creation.

2. **Run targeted benchmarks**
   - Execute only the new test class and capture the measured milliseconds.

3. **Write slice summary with evidence**
   - Record the actual timing numbers, thresholds, and pass/fail verdict in `S06-SUMMARY.md`.

## First Proof

The first proof is a passing targeted `dotnet test` run that prints concrete timing numbers for both benchmarks. Without current-run milliseconds in tool output, the slice has not delivered its core evidence.

## Risks and Watch-outs

- **Timing flakiness:** cold machines and CI can distort one-off stopwatch results. A single warm-up call before timing is the cheapest stabilizer.
- **Measurement scope drift:** do not include temp file creation in the cold-start timer.
- **Over-asserting global performance:** these tests provide milestone evidence on this harness, not universal regression guarantees across every future environment.
- **Allocation cost:** `WordCountService` uses `Split`, so large inputs will allocate. That is fine for S06 evidence, but it explains why future optimization work, if needed, would likely start in this service.
- **Harness gotcha (MEM008):** avoid `gsd_exec` for `dotnet test` on this project because WSL/Windows path resolution can break NuGet/package path handling. Use direct `bash` invocation of `dotnet.exe` instead.

## Skill Discovery

Installed skills already relevant:
- `test` — useful for generating/running the new xUnit tests.
- `tdd` — useful if the executor wants a red/green loop rather than direct implementation.
- `verify-before-complete` — directly relevant to the evidence-first closure pattern for S06/S07.

Promising external skills found (not installed):
- `github/awesome-copilot@csharp-xunit` — 9K installs; strongest xUnit-specific match for C# test structure.
- `dotnet/skills@analyzing-dotnet-performance` — 707 installs; strongest directly relevant .NET performance-analysis match if S07 needs deeper remediation.

Possible install commands if the user wants them later:
- `npx skills add github/awesome-copilot@csharp-xunit`
- `npx skills add dotnet/skills@analyzing-dotnet-performance`

## Verification Plan

Targeted run:
- `"/c/Program Files/dotnet/dotnet.exe" test Hymnal.sln -nologo --filter WordCountPerformanceTests`

Full regression pass after adding the tests:
- `"/c/Program Files/dotnet/dotnet.exe" test Hymnal.sln -nologo`

Optional boundary check if any project-file edit occurs:
- `rg -n "PackageReference.*Avalonia" src/Hymnal.Core/Hymnal.Core.csproj`

## Planner Notes

- This is an additive, test-only slice unless the measured evidence fails badly enough to require reopening scope in S07.
- Keep helper code private to the benchmark test file unless more performance tests appear later.
- The summary artifact must quote the exact milliseconds from the same run used for the pass/fail claim.
- The simplest successful plan is: add one test file, run one targeted command, then write one evidence-rich summary.
