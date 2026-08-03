---
estimated_steps: 22
estimated_files: 1
skills_used: []
---

# T01: Added WordCountPerformanceTests.cs with two Stopwatch-asserted benchmarks: 0 ms/10k-words and 28 ms/100-chapter cold-start

**Why:** S06 needs automated evidence of runtime performance for the M002 Operational validation class. No performance harness exists yet; this task adds it as a single additive file inside the existing `Hymnal.Core.Tests` project with no new dependencies.

**Do:**
1. Create the directory `tests/Hymnal.Core.Tests/Performance/` (no-op if already present).
2. Write `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs` with:
   - Namespace `Hymnal.Core.Tests.Performance`; class `WordCountPerformanceTests` accepting `ITestOutputHelper` via constructor.
   - Instantiate `WordCountService _svc = new()` directly.
   - **Test 1 — `CountWords_10000Words_CompletesWithin200ms`:**
     - Build a 10,000-word deterministic string in memory: loop 1,000 times appending `"alpha bravo charlie delta echo foxtrot golf hotel india juliet\n"` into a `StringBuilder`.
     - Call `_svc.CountWords(content)` once as a warm-up (discarded) to reduce JIT noise.
     - Start `Stopwatch`, call `_svc.CountWords(content)` again, stop.
     - `output.WriteLine($"[S06] Live word-count latency: {sw.ElapsedMilliseconds} ms for 10,000 words")`
     - Assert returned count == 10_000.
     - Assert `sw.ElapsedMilliseconds < 200` with failure message including the actual value.
   - **Test 2 — `CountWords_100ChapterWorkspace_CompletesWithin5000ms`:**
     - Create `tempDir = Path.Combine(Path.GetTempPath(), $"hymnal-bench-{Guid.NewGuid():N}")`; `Directory.CreateDirectory(tempDir)`.
     - In a `try` block: write 100 chapter files (`chapter000.md` … `chapter099.md`), each containing 50 lines of the same ten-word phrase (≈500 words/file); write a `Book.txt` listing all 100 file names.
     - Start `Stopwatch`; sequentially for each chapter: `File.ReadAllText(path)` → `_svc.CountWords(text)`. Stop stopwatch after all 100.
     - `output.WriteLine($"[S06] Cold-start recalculation: {sw.ElapsedMilliseconds} ms for 100 chapters")`
     - Assert `sw.ElapsedMilliseconds < 5000` with failure message including the actual value.
     - In `finally`: `Directory.Delete(tempDir, recursive: true)`.
3. Confirm `Hymnal.Core.csproj` has no Avalonia references added (do not modify it; it should already be clean).

**Done when:** `dotnet test Hymnal.sln --nologo --filter "FullyQualifiedName~WordCountPerformanceTests"` exits 0 and the output lines contain `[S06] Live word-count latency` and `[S06] Cold-start recalculation` with actual millisecond values.

## Inputs

- `src/Hymnal.Core/Services/WordCountService.cs`
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`
- `tests/Hymnal.Core.Tests/Services/WordCountServiceTests.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs`

## Verification

dotnet test Hymnal.sln --nologo --filter "FullyQualifiedName~WordCountPerformanceTests" --logger "console;verbosity=detailed"

## Observability Impact

Test output lines prefixed `[S06]` carry the raw elapsed-ms values; visible with `--logger console;verbosity=detailed` and copyable directly into S06-SUMMARY.md.
