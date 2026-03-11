namespace Tunora.Core.Tests;

// Tests for the schedule overlap logic used by ScheduleService.
// The algorithm converts times to minutes and handles midnight-crossing schedules.
public class ScheduleOverlapTests
{
    // Mirrors the private static TimeRangesOverlap logic in ScheduleService
    private static bool TimeRangesOverlap(TimeOnly s1, TimeOnly e1, TimeOnly s2, TimeOnly e2)
    {
        int a0 = s1.Hour * 60 + s1.Minute, a1 = e1.Hour * 60 + e1.Minute;
        int b0 = s2.Hour * 60 + s2.Minute, b1 = e2.Hour * 60 + e2.Minute;
        if (a1 <= a0) a1 += 1440;   // midnight crossing
        if (b1 <= b0) b1 += 1440;
        return (a0 < b1 && b0 < a1) || (a0 < b1 + 1440 && b0 + 1440 < a1);
    }

    [Fact]
    public void NonOverlapping_ReturnsFalse()
    {
        // 09:00–12:00 vs 13:00–17:00 — no overlap
        var result = TimeRangesOverlap(
            new TimeOnly(9, 0), new TimeOnly(12, 0),
            new TimeOnly(13, 0), new TimeOnly(17, 0));

        Assert.False(result);
    }

    [Fact]
    public void Overlapping_ReturnsTrue()
    {
        // 09:00–14:00 vs 12:00–17:00 — overlap 12:00–14:00
        var result = TimeRangesOverlap(
            new TimeOnly(9, 0), new TimeOnly(14, 0),
            new TimeOnly(12, 0), new TimeOnly(17, 0));

        Assert.True(result);
    }

    [Fact]
    public void AdjacentSlots_ReturnsFalse()
    {
        // 09:00–12:00 exactly adjacent to 12:00–17:00 — no overlap
        var result = TimeRangesOverlap(
            new TimeOnly(9, 0), new TimeOnly(12, 0),
            new TimeOnly(12, 0), new TimeOnly(17, 0));

        Assert.False(result);
    }

    [Fact]
    public void MidnightCrossing_OverlapsWithMorning()
    {
        // 22:00–02:00 (crosses midnight) vs 01:00–05:00
        var result = TimeRangesOverlap(
            new TimeOnly(22, 0), new TimeOnly(2, 0),
            new TimeOnly(1, 0), new TimeOnly(5, 0));

        Assert.True(result);
    }

    [Fact]
    public void MidnightCrossing_NoOverlapWithAfternoon()
    {
        // 22:00–02:00 (crosses midnight) vs 10:00–18:00
        var result = TimeRangesOverlap(
            new TimeOnly(22, 0), new TimeOnly(2, 0),
            new TimeOnly(10, 0), new TimeOnly(18, 0));

        Assert.False(result);
    }

    [Fact]
    public void ContainedSlot_ReturnsTrue()
    {
        // 08:00–20:00 fully contains 10:00–12:00
        var result = TimeRangesOverlap(
            new TimeOnly(8, 0), new TimeOnly(20, 0),
            new TimeOnly(10, 0), new TimeOnly(12, 0));

        Assert.True(result);
    }
}
