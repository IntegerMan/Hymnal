---
estimated_steps: 15
estimated_files: 1
skills_used: []
---

# T02: Ran benchmarks (59/59 pass), captured 0 ms / 23 ms timings, wrote S06-SUMMARY.md with Observable Truths table

**Why:** The slice proof level is 'operational', meaning S06 is not done until actual millisecond numbers from a real test run are recorded in an artifact that S07 can reference for the Operational validation class closure. This task produces that artifact.

**Do:**
1. Run `dotnet test Hymnal.sln --nologo --logger "console;verbosity=detailed"` (full suite). Capture the complete output — look for lines matching `[S06] Live word-count latency:` and `[S06] Cold-start recalculation:` in the test output.
2. Note the exact elapsed-ms figures from both `[S06]` output lines.
3. Note the total test count and pass/fail status (baseline from S05 was 57 passed).
4. Write `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md` following this structure:
   - YAML front matter: `id: S06`, `milestone: M002`, `status: complete`, `verification_result: passed` (or `failed` if a threshold was exceeded).
   - **What Happened** prose: one paragraph describing what was measured and how.
   - **Observable Truths** table with columns `Measurement | Threshold | Actual ms | Verdict`:
     - Row 1: Live word-count latency | < 200 ms | `{actual}` ms | PASS or FAIL
     - Row 2: 100-chapter cold-start | < 5,000 ms | `{actual}` ms | PASS or FAIL
   - **Verification** section: paste the `dotnet test` pass/fail line (e.g. `X passed, 0 failed`) as evidence.
   - **Deviations / Remediation** section: if any threshold was exceeded, note the actual value and flag it for S07; if both passed write 'None'.
   - **Follow-ups** section: 'S07 may relax thresholds to 500ms / 8,000ms if CI environments prove slower; otherwise no action needed.'

**Done when:** `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md` exists, contains the Observable Truths table with real numbers (not placeholders), and `dotnet test Hymnal.sln --nologo` exits 0.

## Inputs

- `tests/Hymnal.Core.Tests/Performance/WordCountPerformanceTests.cs`
- `.gsd/milestones/M002/slices/S05/S05-SUMMARY.md`

## Expected Output

- `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md`

## Verification

dotnet test Hymnal.sln --nologo
