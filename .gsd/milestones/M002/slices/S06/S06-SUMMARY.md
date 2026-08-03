---
id: S06
parent: M002
milestone: M002
provides:
  - Durable operational benchmark evidence for S07
  - Measured pass/fail data for live word-count latency and workspace cold-start timing
requires:
  []
affects:
  []
key_files:
  - tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs
  - .gsd/milestones/M002/slices/S06/S06-SUMMARY.md
key_decisions:
  - Instantiate `WordCountService` directly in the perf test because it is stateless and pure for this scenario.
  - Use a warm-up call before the timed live-count run to reduce JIT noise.
  - Clean up the temp workspace in a `finally` block so benchmark failures do not leave pollution behind.
  - Emit `[S06]`-prefixed timing lines so the raw measurements are copyable from `dotnet test` output.
patterns_established:
  - Benchmark-style xUnit tests can serve as durable operational evidence when they emit explicit timing markers and assert both correctness and latency.
  - For stateless services, direct construction in performance tests keeps the benchmark focused on runtime behavior rather than DI overhead.
observability_surfaces:
  - `[S06] Live word-count latency: …` console output
  - `[S06] Cold-start recalculation: …` console output
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-31T23:35:13.714Z
blocker_discovered: false
---

# S06: Operational Benchmark Evidence

**Added and verified Stopwatch-based benchmark tests that measure live word-count latency and 100-chapter cold-start recalculation, with durable millisecond evidence captured for milestone validation.**

## What Happened

S06 added a new `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs` benchmark file that exercises the live `WordCountService` directly with no DI or Avalonia dependencies. The first benchmark builds a deterministic 10,000-word in-memory chapter, performs a warm-up call to reduce JIT noise, then times a second `CountWords` call with `Stopwatch` and asserts the result completes within 200 ms. The second benchmark creates a temp workspace containing 100 synthetic chapter files plus `Book.txt`, sequentially reads and counts each file under one `Stopwatch`, asserts the cold-start baseline completes within 5,000 ms, and deletes the temp directory in a `finally` block.

For closeout verification, the perf filter was rerun under `cmd.exe /c dotnet test Hymnal.sln --nologo --no-restore --filter "FullyQualifiedName~WordCountPerformanceTests" --logger "console;verbosity=detailed"`, confirming 2/2 benchmark tests passed and emitting the raw `[S06]` timing lines. The current closeout run measured 1 ms for the 10,000-word latency test and 82 ms for the 100-chapter cold-start test, both well below their thresholds. The full solution was also verified with `cmd.exe /c dotnet test Hymnal.sln --nologo --no-restore`, which passed 59/59 tests. The slice now has durable benchmark evidence suitable for S07's validation-alignment pass.

## Verification

Verification passed. Closeout evidence: `cmd.exe /c dotnet test Hymnal.sln --nologo --no-restore --filter "FullyQualifiedName~WordCountPerformanceTests" --logger "console;verbosity=detailed"` passed 2/2 and emitted `[S06] Live word-count latency: 1 ms for 10,000 words` plus `[S06] Cold-start recalculation: 82 ms for 100 chapters`. Full-suite evidence: `cmd.exe /c dotnet test Hymnal.sln --nologo --no-restore` passed 59/59 tests. Prior benchmark evidence in the task summaries also recorded the same two checks passing under detailed logging.

## Requirements Advanced

None.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

The benchmark numbers are environment-sensitive and should be interpreted as observed evidence rather than a fixed performance guarantee across all hardware.

## Follow-ups

S07 will use this evidence to close the Operational validation class and decide whether any threshold relaxation or remediation is needed.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs` — Added two Stopwatch-based performance benchmarks for live word-count latency and 100-chapter cold-start recalculation.
- `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md` — Rendered the closeout summary with observed benchmark timings and verification evidence.
