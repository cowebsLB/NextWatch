using NextWatch.Core.Domain;

namespace NextWatch.Core.Alerts;

public static class AlertIncidentTriggers
{
    /// <summary>
    /// Whether this check cycle should open a new alerting incident (toast/webhook/event).
    /// Avoids notifying every scheduler tick while still DOWN/WARN.
    /// </summary>
    public static bool ShouldOpenNewIncident(CheckStatus previous, CheckStatus current)
    {
        if (current is not (CheckStatus.Down or CheckStatus.Warn))
            return false;

        var wasAlerting = previous is CheckStatus.Down or CheckStatus.Warn;
        return !wasAlerting || previous != current;
    }
}
