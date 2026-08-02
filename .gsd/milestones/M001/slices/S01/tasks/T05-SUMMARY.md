---
id: T05
parent: S01
milestone: M001
key_files:
  - .github/workflows/release.yml
  - .github/workflows/ci.yml
key_decisions:
  - ci.yml uses ubuntu-latest only (no matrix) for PR/branch builds to reduce CI minutes — win-x64 publish matrix is reserved for the release workflow on tag push
  - release.yml uploads each platform artifact as a named upload-artifact step so release assets are available for download without a separate release job
  - No dotnet run step in either workflow — Avalonia desktop app requires a display server unavailable in headless CI
duration: 
verification_result: passed
completed_at: 2026-05-28T20:10:31.161Z
blocker_discovered: false
---

# T05: Created .github/workflows/release.yml (win-x64 + linux-x64 matrix publish on v* tags) and ci.yml (ubuntu-only build+test on main/milestone branches and PRs)

**Created .github/workflows/release.yml (win-x64 + linux-x64 matrix publish on v* tags) and ci.yml (ubuntu-only build+test on main/milestone branches and PRs)**

## What Happened

Created the .github/workflows directory and wrote two workflow files. release.yml triggers on push to v* tags and runs a 2×2 matrix (windows-latest/win-x64 and ubuntu-latest/linux-x64): checkout → setup-dotnet 10.x → restore → build Release → test → self-contained publish → upload-artifact. ci.yml triggers on push to main and milestone/* branches and on pull_request; runs ubuntu-latest only for speed with the same steps minus publish. No dotnet run step was included per the task plan (headless CI has no display server). YAML structure was validated via node; dotnet build Hymnal.sln --no-incremental exits 0 confirming the solution the workflows target is buildable.

## Verification

dotnet build Hymnal.sln --no-incremental — exit 0, 0 errors. Node-based YAML structural check on both files — no tabs, name/on/jobs keys present.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build Hymnal.sln --no-incremental` | 0 | pass | 10630ms |
| 2 | `node -e YAML structure check on release.yml and ci.yml` | 0 | pass | 120ms |

## Deviations

Added actions/upload-artifact@v4 step to release.yml (not in task plan but standard practice for a publish step — artifacts would otherwise be silently discarded).

## Known Issues

Workflows cannot be exercised locally; correctness of the GH Actions YAML is validated by structural checks only. Full end-to-end run requires a GitHub remote with Actions enabled.

## Files Created/Modified

- `.github/workflows/release.yml`
- `.github/workflows/ci.yml`
