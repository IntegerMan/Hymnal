---
id: T01
parent: S02
milestone: M001
key_files:
  - src/Hymnal.Core/Services/BookTxtParser.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs
  - tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt
  - tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj
key_decisions:
  - Fixture files copied via CopyToOutputDirectory so tests can use AppContext.BaseDirectory to locate them
  - BookTxtParser reads only first 3 non-blank lines of each .md to minimize I/O while still finding {class: part} and heading
duration: 
verification_result: passed
completed_at: 2026-05-29T03:28:44.768Z
blocker_discovered: false
---

# T01: BookTxtParser implemented with Part/Chapter detection; 4 unit tests pass

**BookTxtParser implemented with Part/Chapter detection; 4 unit tests pass**

## What Happened

Created `src/Hymnal.Core/Services/BookTxtParser.cs` — a static class with a `Parse(folderPath, bookTxtLines)` method that strips blank lines, resolves absolute paths, reads the first 3 non-blank lines of each .md file to detect `{class: part}`, derives title from the `# ` heading line, marks missing files with `IsMissing=true`, and sets `NodeKind.Part` or `NodeKind.Chapter` accordingly. Updated `multi-part-book/Book.txt` to list `part-one/part.md` on line 1 then `part-one/chapter-one.md` on line 2. Added `<None Include="Fixtures\**\*" CopyToOutputDirectory="PreserveNewest" />` to the test csproj so fixture files are available at `AppContext.BaseDirectory` during test runs. Created `tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs` with 4 xUnit tests covering simple book, multi-part book, blank line filtering, and missing file detection.

## Verification

Ran `dotnet test tests/Hymnal.Core.Tests/ --filter BookTxtParserTests` — 4 passed, 0 failed, exit code 0.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/ --filter BookTxtParserTests` | 0 | pass | 15000ms |

## Deviations

Added CopyToOutputDirectory glob to test csproj — not explicitly in plan but required for AppContext.BaseDirectory-based fixture paths to work.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Services/BookTxtParser.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt`
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`
