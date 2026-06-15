using System.Collections.Generic;
using Hymnal.Core.Models;

namespace Hymnal.ViewModels;

public record HistoryChartPoint(string Date, int WordCount);

public record HistorySegment(string Label, int WordCount, ChapterStatus Status);

public record StackedHistoryPoint(string Date, IReadOnlyList<HistorySegment> Segments);
