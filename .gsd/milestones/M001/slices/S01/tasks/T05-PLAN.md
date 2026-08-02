---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T05: Created .github/workflows/release.yml (win-x64 + linux-x64 matrix publish on v* tags) and ci.yml (ubuntu-only build+test on main/milestone branches and PRs)

Why: R014 requires self-contained win-x64 and linux-x64 publish targets. The CI workflow validates the build matrix on every tag push so regressions are caught before release. Do: (1) Create .github/workflows/release.yml. Trigger: on push tags matching v*. Matrix: os=[windows-latest, ubuntu-latest], rid=[win-x64, linux-x64]. Steps per matrix job: (a) actions/checkout@v4, (b) actions/setup-dotnet@v4 with dotnet-version: 10.x, (c) dotnet restore Hymnal.sln, (d) dotnet build Hymnal.sln --configuration Release --no-restore, (e) dotnet test tests/Hymnal.Core.Tests/ --configuration Release --no-build --no-restore, (f) dotnet publish src/Hymnal/Hymnal.csproj --configuration Release --runtime ${{ matrix.rid }} --self-contained true --output publish/${{ matrix.rid }}. IMPORTANT: do NOT add a dotnet run step — headless CI has no display server; the app cannot launch in CI. (2) Add a .github/workflows/ci.yml for PR/branch validation (triggers: push branches [main, milestone/*], pull_request): same steps as release.yml minus the publish step; matrix can be simplified to ubuntu-latest only for speed. Done when: .github/workflows/release.yml and ci.yml exist and are valid YAML (dotnet build Hymnal.sln exits 0 is the proxy since we cannot run GH Actions locally).

## Inputs

- `Hymnal.sln`
- `src/Hymnal/Hymnal.csproj`
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`

## Expected Output

- `.github/workflows/release.yml`
- `.github/workflows/ci.yml`

## Verification

dotnet build Hymnal.sln --no-incremental
