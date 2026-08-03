# S06: Operational Benchmark Evidence

**Goal:** Produce two xUnit timing tests that measure live word-count latency on a 10,000-word chapter and cold-start recalculation across a 100-chapter workspace, with pass/fail assertions and actual measured milliseconds captured in S06-SUMMARY.md for re-validation.
**Demo:** After this: measured evidence exists for live word-count latency on a 10,000-word chapter and cold-start timing for a 100-chapter workspace, with results captured in milestone artifacts for re-validation.

## Must-Haves

- `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs` exists with two Stopwatch-based tests.
- Test 1 asserts `CountWords` on a 10,000-word in-memory string completes in < 200 ms.
- Test 2 asserts sequential read + `CountWords` across a 100-chapter temp workspace completes in < 5,000 ms; temp directory cleaned up in `finally`.
- `dotnet test Hymnal.sln --nologo` exits 0 with all existing tests still passing.
- `S06-SUMMARY.md` exists in `.gsd/milestones/M002/slices/S06/` with the actual elapsed-ms numbers for both tests and a verdict on whether thresholds were met.

## Proof Level

- This slice proves: operational — measured elapsed-ms numbers from a real `dotnet test` run

## Integration Closure

Consumes `WordCountService.CountWords()` directly (no mocks, no Avalonia). Adds one new file to the existing `Hymnal.Core.Tests` project; no new project or package reference needed. S07 consumes the S06-SUMMARY.md artifact to close the Operational validation class.

## Verification

- Test output lines prefixed `[S06]` emit the raw millisecond readings so they appear in `dotnet test --logger console;verbosity=detailed` output and are copyable into the summary artifact.

## Tasks

- [x] **T01: Added WordCountPerformanceTests.cs with two Stopwatch-asserted benchmarks: 0 ms/10k-words and 28 ms/100-chapter cold-start** `est:30m`
  **Why:** S06 needs automated evidence of runtime performance for the M002 Operational validation class. No performance harness exists yet; this task adds it as a single additive file inside the existing `Hymnal.Core.Tests` project with no new dependencies.
  - Files: `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs`
  - Verify: dotnet test Hymnal.sln --nologo --filter "FullyQualifiedName~WordCountPerformanceTests" --logger "console;verbosity=detailed"

- [x] **T02: Ran benchmarks (59/59 pass), captured 0 ms / 23 ms timings, wrote S06-SUMMARY.md with Observable Truths table** `est:20m`
  **Why:** The slice proof level is 'operational', meaning S06 is not done until actual millisecond numbers from a real test run are recorded in an artifact that S07 can reference for the Operational validation class closure. This task produces that artifact.
  - Files: `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md`
  - Verify: dotnet test Hymnal.sln --nologo

- [x] **T03: Record no-restore verification evidence and restore blocker** `est:15m`
  Run the full solution test suite with `dotnet test Hymnal.sln --nologo --no-restore` to verify the completed performance benchmarks still pass, capture the restore blocker as an environment issue, and document the alternate verification path for downstream validation.
  - Files: `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md`
  - Verify: dotnet test Hymnal.sln --nologo --no-restore

## Files Likely Touched

- tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs
- .gsd/milestones/M002/slices/S06/S06-SUMMARY.md
