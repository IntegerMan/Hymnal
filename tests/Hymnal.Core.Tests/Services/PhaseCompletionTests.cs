using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class PhaseCompletionTests
{
    private static readonly DateOnly Today = new(2026, 6, 4);

    [Theory]
    [InlineData(ChapterStatus.Planned, ChapterStatus.Outlining)]
    [InlineData(ChapterStatus.Outlining, ChapterStatus.Drafting)]
    [InlineData(ChapterStatus.Drafting, ChapterStatus.Editing)]
    [InlineData(ChapterStatus.Editing, ChapterStatus.Polishing)]
    [InlineData(ChapterStatus.Polishing, ChapterStatus.Reviewing)]
    [InlineData(ChapterStatus.Reviewing, ChapterStatus.Done)]
    public void GetNextStatus_AdvancesLinearly(ChapterStatus current, ChapterStatus expected)
    {
        Assert.Equal(expected, PhaseCompletion.GetNextStatus(current));
    }

    [Fact]
    public void GetNextStatus_Done_ReturnsNull()
    {
        Assert.Null(PhaseCompletion.GetNextStatus(ChapterStatus.Done));
    }

    [Fact]
    public void CompleteAndAdvance_OutliningWithoutStart_SetsStartEndAndProgress()
    {
        var current = new PhaseData
        {
            Status = ChapterStatus.Outlining,
            Schedule = new Dictionary<string, PhaseScheduleEntry>()
        };

        var result = PhaseCompletion.CompleteAndAdvance(current, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChapterStatus.Drafting, result.Value!.Status);
        Assert.Equal("2026-06-04", result.Value.Schedule!["Outlining"].StartDate);
        Assert.Equal("2026-06-04", result.Value.Schedule["Outlining"].EndDate);
        Assert.Equal(100.0, result.Value.Schedule["Outlining"].Progress);
        Assert.False(result.Value.Schedule.ContainsKey("Drafting"));
        Assert.Null(result.Value.PhaseStartDate);
        Assert.Null(result.Value.PhaseEndDate);
    }

    [Fact]
    public void CompleteAndAdvance_OutliningWithExistingStart_PreservesStart()
    {
        var current = new PhaseData
        {
            Status = ChapterStatus.Outlining,
            Schedule = new Dictionary<string, PhaseScheduleEntry>
            {
                ["Outlining"] = new PhaseScheduleEntry
                {
                    StartDate = "2026-05-01",
                    Progress = 50.0
                }
            }
        };

        var result = PhaseCompletion.CompleteAndAdvance(current, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal("2026-05-01", result.Value!.Schedule!["Outlining"].StartDate);
        Assert.Equal("2026-06-04", result.Value.Schedule["Outlining"].EndDate);
        Assert.Equal(100.0, result.Value.Schedule["Outlining"].Progress);
    }

    [Fact]
    public void CompleteAndAdvance_Planned_AdvancesToOutliningWithoutPrefill()
    {
        var current = new PhaseData { Status = ChapterStatus.Planned };

        var result = PhaseCompletion.CompleteAndAdvance(current, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChapterStatus.Outlining, result.Value!.Status);
        Assert.True(result.Value.Schedule is null || result.Value.Schedule.Count == 0);
        Assert.Null(result.Value.PhaseStartDate);
        Assert.Null(result.Value.PhaseEndDate);
    }

    [Fact]
    public void CompleteAndAdvance_Reviewing_AdvancesToDone()
    {
        var current = new PhaseData
        {
            Status = ChapterStatus.Reviewing,
            Schedule = new Dictionary<string, PhaseScheduleEntry>
            {
                ["Reviewing"] = new PhaseScheduleEntry
                {
                    StartDate = "2026-05-01",
                    Progress = 80.0
                }
            }
        };

        var result = PhaseCompletion.CompleteAndAdvance(current, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChapterStatus.Done, result.Value!.Status);
        Assert.Equal(100.0, result.Value.Schedule!["Reviewing"].Progress);
        Assert.Equal("2026-06-04", result.Value.Schedule["Reviewing"].EndDate);
    }

    [Fact]
    public void CompleteAndAdvance_Done_ReturnsFailure()
    {
        var current = new PhaseData { Status = ChapterStatus.Done };

        var result = PhaseCompletion.CompleteAndAdvance(current, Today);

        Assert.False(result.IsSuccess);
        Assert.Contains("already done", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompleteAndAdvance_PreservesExistingNextPhaseEntry()
    {
        var current = new PhaseData
        {
            Status = ChapterStatus.Outlining,
            Schedule = new Dictionary<string, PhaseScheduleEntry>
            {
                ["Drafting"] = new PhaseScheduleEntry
                {
                    StartDate = "2026-05-15",
                    Progress = 25.0
                }
            }
        };

        var result = PhaseCompletion.CompleteAndAdvance(current, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChapterStatus.Drafting, result.Value!.Status);
        Assert.Equal("2026-05-15", result.Value.Schedule!["Drafting"].StartDate);
        Assert.Equal(25.0, result.Value.Schedule["Drafting"].Progress);
    }
}
