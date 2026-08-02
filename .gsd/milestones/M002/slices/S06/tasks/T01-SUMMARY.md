---
id: T01
parent: S06
milestone: M002
key_files:
  - tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs
key_decisions:
  - WordCountService instantiated directly (new()) — no DI needed for a pure stateless service in a perf test
  - Warm-up call before timed run to reduce JIT noise on Test 1
  - Temp directory cleaned up in finally block to avoid pollution on test failure
duration: 
verification_result: passed
completed_at: 2026-05-31T21:18:20.408Z
blocker_discovered: false
---

# T01: Added WordCountPerformanceTests.cs with two Stopwatch-asserted benchmarks: 0 ms/10k-words and 28 ms/100-chapter cold-start

**Added WordCountPerformanceTests.cs with two Stopwatch-asserted benchmarks: 0 ms/10k-words and 28 ms/100-chapter cold-start**

## What Happened

Created `tests/Hymnal.Core.Tests/Performance/` directory and wrote `WordCountPerformanceTests.cs` with two xUnit tests.

**Test 1 — CountWords_10000Words_CompletesWithin200ms:** Builds a deterministic 10,000-word string (1,000 × 10-word phrase), runs a warm-up call to reduce JIT noise, then times a second call with `Stopwatch`. Asserts count == 10,000 and elapsed < 200 ms. Emits `[S06] Live word-count latency: {ms} ms for 10,000 words` via `ITestOutputHelper`.

**Test 2 — CountWords_100ChapterWorkspace_CompletesWithin5000ms:** Creates a temp directory, writes 100 chapter files (50 lines × 10 words = 500 words each) plus a `Book.txt`, then sequentially reads and counts all 100 files under a single `Stopwatch`. Cleans up in `finally`. Asserts elapsed < 5,000 ms. Emits `[S06] Cold-start recalculation: {ms} ms for 100 chapters`.

No dependencies were added; `WordCountService` is instantiated directly (`new()`). The `Hymnal.Core.csproj` was not modified.

## Verification

Ran `dotnet test Hymnal.sln --nologo --filter "FullyQualifiedName~WordCountPerformanceTests" --logger "console;verbosity=detailed"` — exit code 0, 2/2 passed. Output contained both [S06] lines: live latency 0 ms, cold-start 28 ms.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --nologo --filter "FullyQualifiedName~WordCountPerformanceTests" --logger "console;verbosity=detailed"` | 0 | ✅ pass — 2/2 tests passed; [S06] Live word-count latency: 0 ms; [S06] Cold-start recalculation: 28 ms | 1508ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs`
