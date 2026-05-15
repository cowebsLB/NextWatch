using NextWatch.Core.Alerts;
using Xunit;

namespace NextWatch.Core.Tests;

public sealed class AlertRepeatScheduleTests
{
    [Fact]
    public void First_repeat_not_due_until_interval_after_fire()
    {
        var fired = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(AlertRepeatSchedule.IsRepeatDue(fired, 15, 0, fired.AddMinutes(14)));
        Assert.True(AlertRepeatSchedule.IsRepeatDue(fired, 15, 0, fired.AddMinutes(15)));
    }

    [Fact]
    public void Second_repeat_waits_another_interval()
    {
        var fired = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(AlertRepeatSchedule.IsRepeatDue(fired, 15, 1, fired.AddMinutes(29)));
        Assert.True(AlertRepeatSchedule.IsRepeatDue(fired, 15, 1, fired.AddMinutes(30)));
    }

    [Fact]
    public void Zero_repeatMinutes_falls_back_to_15()
    {
        var fired = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(AlertRepeatSchedule.IsRepeatDue(fired, 0, 0, fired.AddMinutes(14)));
        Assert.True(AlertRepeatSchedule.IsRepeatDue(fired, 0, 0, fired.AddMinutes(15)));
    }
}
