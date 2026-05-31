using System.Diagnostics;
using System.Text;
using Hymnal.Core.Services;
using Xunit.Abstractions;

namespace Hymnal.Core.Tests.Performance;

public class WordCountPerformanceTests
{
    private readonly WordCountService _svc = new();
    private readonly ITestOutputHelper _output;

    public WordCountPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Measures live word-count latency on a 10,000-word in-memory chapter.
    /// A warm-up call is discarded before the timed run to reduce JIT noise.
    /// Asserts completion within 200 ms.
    /// </summary>
    [Fact]
    public void CountWords_10000Words_CompletesWithin200ms()
    {
        // Build a deterministic 10,000-word string:
        // 1,000 lines × 10 words each = 10,000 words
        const string phrase = "alpha bravo charlie delta echo foxtrot golf hotel india juliet\n";
        var sb = new StringBuilder(phrase.Length * 1000);
        for (int i = 0; i < 1000; i++)
            sb.Append(phrase);
        var content = sb.ToString();

        // Warm-up call — discard result to reduce JIT noise
        _ = _svc.CountWords(content);

        // Timed run
        var sw = Stopwatch.StartNew();
        var count = _svc.CountWords(content);
        sw.Stop();

        _output.WriteLine(
            $"[S06] Live word-count latency: {sw.ElapsedMilliseconds} ms for 10,000 words");

        Assert.Equal(10_000, count);
        Assert.True(
            sw.ElapsedMilliseconds < 200,
            $"Expected < 200 ms but took {sw.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Measures cold-start recalculation across a 100-chapter workspace.
    /// Writes temp files, reads + counts each sequentially, then cleans up.
    /// Asserts completion within 5,000 ms.
    /// </summary>
    [Fact]
    public void CountWords_100ChapterWorkspace_CompletesWithin5000ms()
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            $"hymnal-bench-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Each chapter: 50 lines × 10 words = 500 words/file
            const string line = "alpha bravo charlie delta echo foxtrot golf hotel india juliet\n";
            var chapterContent = string.Concat(Enumerable.Repeat(line, 50));

            var bookLines = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                var fileName = $"chapter{i:D3}.md";
                File.WriteAllText(Path.Combine(tempDir, fileName), chapterContent);
                bookLines.AppendLine(fileName);
            }
            File.WriteAllText(Path.Combine(tempDir, "Book.txt"), bookLines.ToString());

            // Timed sequential read + count across all 100 chapters
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                var path = Path.Combine(tempDir, $"chapter{i:D3}.md");
                var text = File.ReadAllText(path);
                _ = _svc.CountWords(text);
            }
            sw.Stop();

            _output.WriteLine(
                $"[S06] Cold-start recalculation: {sw.ElapsedMilliseconds} ms for 100 chapters");

            Assert.True(
                sw.ElapsedMilliseconds < 5000,
                $"Expected < 5000 ms but took {sw.ElapsedMilliseconds} ms");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
