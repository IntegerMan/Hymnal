# S06: Operational Benchmark Evidence — UAT

**Milestone:** M002
**Written:** 2026-05-31T23:35:13.715Z

# UAT: S06 Operational Benchmark Evidence

**UAT Type:** Operational evidence / benchmark verification

## Preconditions
- Repository is at the M002 S06 closeout state.
- `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs` exists.
- The solution can be tested with `cmd.exe /c dotnet test` in the Windows harness.

## Steps
1. Run the performance benchmark filter:
   - `cmd.exe /c dotnet test Hymnal.sln --nologo --no-restore --filter "FullyQualifiedName~WordCountPerformanceTests" --logger "console;verbosity=detailed"`
2. Confirm both benchmark tests pass.
3. Confirm the console output contains the `[S06]` lines for both measurements.
4. Run the full solution verification:
   - `cmd.exe /c dotnet test Hymnal.sln --nologo --no-restore`
5. Confirm the full suite passes.
6. Review the S06 summary artifact for the recorded timings and verdict.

## Expected Outcomes
- The 10,000-word live word-count benchmark completes in under 200 ms.
- The 100-chapter cold-start recalculation benchmark completes in under 5,000 ms.
- The benchmark output includes copyable `[S06]` timing lines.
- The full solution test suite passes without restore.
- The S06 summary captures the measured values and marks the slice complete.

## Edge Cases
- If the environment cannot run `dotnet` from bash, use `cmd.exe /c dotnet ...`.
- If a restore attempt fails in the harness, rerun the verification with `--no-restore`.
- If timings vary on slower hardware, the summary should preserve the actual measured values and note any threshold review needed for S07.
