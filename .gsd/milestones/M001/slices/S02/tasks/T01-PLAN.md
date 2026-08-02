---
estimated_steps: 10
estimated_files: 3
skills_used: []
---

# T01: BookTxtParser implemented with Part/Chapter detection; 4 unit tests pass

Why: BookTxtParser is the highest-risk piece — it must correctly distinguish Parts from Chapters by reading the first non-blank line of each .md file for `{class: part}`. The multi-part-book fixture Book.txt currently only lists `part-one/chapter-one.md` but the real manuscript shows part.md files appear inline in Book.txt; the fixture must be corrected to include `part-one/part.md` so part-detection tests are meaningful.

Do:
1. Update `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt` to list `part-one/part.md` on line 1, then `part-one/chapter-one.md` on line 2. The existing `part-one/part.md` fixture file already has `{class: part}` on line 1.
2. Create `src/Hymnal.Core/Services/BookTxtParser.cs` with a `public static class BookTxtParser` containing `public static IReadOnlyList<ChapterNode> Parse(string folderPath, IEnumerable<string> bookTxtLines)`. The method: strips blank lines from input; for each remaining line resolves the absolute path `Path.Combine(folderPath, line)`; reads the first 3 non-blank lines of the .md file (if it exists) and checks whether any begins with `{class: part}` (case-sensitive trim); marks `IsMissing = true` if the file does not exist; sets `NodeKind.Part` if `{class: part}` found, else `NodeKind.Chapter`; derives `Title` from the `# ` heading line if present, else uses the filename stem; sets `Key = RelativePath = line` (normalized with forward slashes).
3. Create `tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs` with xUnit tests:
   - `SimplBook_ParsesSingleChapter`: simple-book fixture → 1 node, Kind=Chapter, IsMissing=false
   - `MultiPartBook_ParsesTwoNodes_PartAndChapter`: multi-part-book fixture → 2 nodes; first is Kind=Part, second is Kind=Chapter
   - `BlankLines_AreIgnored`: lines with only whitespace produce no nodes
   - `MissingFile_MarkedIsMissing`: entry whose file does not exist → IsMissing=true, Kind=Chapter

Done when: `dotnet test tests/Hymnal.Core.Tests/ --filter BookTxtParserTests` exits 0 with 4 tests passing.

## Inputs

- `src/Hymnal.Core/Models/ChapterNode.cs`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/Book.txt`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/chapter-one.md`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/part-one/part.md`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/part-one/chapter-one.md`

## Expected Output

- `src/Hymnal.Core/Services/BookTxtParser.cs`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt`
- `tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/ --filter BookTxtParserTests
