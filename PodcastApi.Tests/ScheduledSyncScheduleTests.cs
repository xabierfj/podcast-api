using PodcastApi.Sync;

namespace PodcastApi.Tests;

public class ScheduledSyncScheduleTests
{
    private static readonly DayOfWeek[] Tuesdays = [DayOfWeek.Tuesday];

    // 2026-09-15 is a Tuesday; 2026-09-16 a Wednesday.
    private static DateTime Tue(int hour, int minute = 0) => new(2026, 9, 15, hour, minute, 0);

    [Fact]
    public void FromMidTuesday_ReturnsNextSlotSameDay()
    {
        var next = ScheduledSyncService.NextRunLocal(Tue(10, 30), Tuesdays, 3);
        Assert.Equal(new DateTime(2026, 9, 15, 12, 0, 0), next);
    }

    [Fact]
    public void SlotsAreAlignedToMidnight_NotToCallTime()
    {
        // 10:30 + 3h would be 13:30; slots are 0,3,6,9,12,...
        var next = ScheduledSyncService.NextRunLocal(Tue(10, 30), Tuesdays, 3);
        Assert.Equal(0, next.Minute);
        Assert.Equal(0, next.Hour % 3);
    }

    [Fact]
    public void ExactlyOnASlot_MovesToTheFollowingOne()
    {
        // Strictly-after, so a run at 12:00 does not immediately re-trigger.
        var next = ScheduledSyncService.NextRunLocal(Tue(12, 0), Tuesdays, 3);
        Assert.Equal(new DateTime(2026, 9, 15, 15, 0, 0), next);
    }

    [Fact]
    public void AfterLastSlotOfTuesday_JumpsAWholeWeek()
    {
        var next = ScheduledSyncService.NextRunLocal(Tue(21, 1), Tuesdays, 3);
        Assert.Equal(new DateTime(2026, 9, 22, 0, 0, 0), next);
        Assert.Equal(DayOfWeek.Tuesday, next.DayOfWeek);
    }

    [Fact]
    public void FromWednesday_WaitsForNextTuesday()
    {
        var wednesday = new DateTime(2026, 9, 16, 8, 0, 0);
        var next = ScheduledSyncService.NextRunLocal(wednesday, Tuesdays, 3);
        Assert.Equal(new DateTime(2026, 9, 22, 0, 0, 0), next);
    }

    [Fact]
    public void NeverReturnsANonPublishDay()
    {
        var cursor = new DateTime(2026, 9, 16, 8, 0, 0);
        for (var i = 0; i < 50; i++)
        {
            cursor = ScheduledSyncService.NextRunLocal(cursor, Tuesdays, 3);
            Assert.Equal(DayOfWeek.Tuesday, cursor.DayOfWeek);
        }
    }

    [Fact]
    public void MultipleDays_PicksTheNearestOne()
    {
        DayOfWeek[] days = [DayOfWeek.Tuesday, DayOfWeek.Wednesday];
        var next = ScheduledSyncService.NextRunLocal(Tue(23, 0), days, 3);
        Assert.Equal(new DateTime(2026, 9, 16, 0, 0, 0), next);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    public void AlwaysAdvances(int intervalHours)
    {
        var now = Tue(10, 30);
        var next = ScheduledSyncService.NextRunLocal(now, Tuesdays, intervalHours);
        Assert.True(next > now);
    }

    [Fact]
    public void RejectsEmptyDayList()
    {
        Assert.Throws<ArgumentException>(
            () => ScheduledSyncService.NextRunLocal(Tue(10), [], 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(-1)]
    public void RejectsOutOfRangeInterval(int intervalHours)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ScheduledSyncService.NextRunLocal(Tue(10), Tuesdays, intervalHours));
    }
}
