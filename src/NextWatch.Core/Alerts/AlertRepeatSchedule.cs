namespace NextWatch.Core.Alerts;

public static class AlertRepeatSchedule
{
    /// <summary>
    /// Next repeat is due <paramref name="repeatMinutes"/> after the initial fire, then every
    /// <paramref name="repeatMinutes"/> thereafter (RepeatCount = number of repeats already sent).
    /// </summary>
    public static bool IsRepeatDue(DateTime firedAtUtc, int repeatMinutes, int repeatCount, DateTime nowUtc)
    {
        if (repeatMinutes <= 0)
            repeatMinutes = 15;
        var nextDue = firedAtUtc.AddMinutes(repeatMinutes * (repeatCount + 1));
        return nowUtc >= nextDue;
    }
}
