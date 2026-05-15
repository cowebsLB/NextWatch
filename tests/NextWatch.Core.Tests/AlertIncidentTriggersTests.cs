using NextWatch.Core.Alerts;
using NextWatch.Core.Domain;
using Xunit;

namespace NextWatch.Core.Tests;

public sealed class AlertIncidentTriggersTests
{
    [Theory]
    [InlineData(CheckStatus.Ok, CheckStatus.Down, true)]
    [InlineData(CheckStatus.Unknown, CheckStatus.Warn, true)]
    [InlineData(CheckStatus.Warn, CheckStatus.Down, true)]
    [InlineData(CheckStatus.Down, CheckStatus.Warn, true)]
    [InlineData(CheckStatus.Down, CheckStatus.Down, false)]
    [InlineData(CheckStatus.Warn, CheckStatus.Warn, false)]
    [InlineData(CheckStatus.Ok, CheckStatus.Ok, false)]
    [InlineData(CheckStatus.Warn, CheckStatus.Ok, false)]
    public void ShouldOpenNewIncident_matches_transition_expectations(CheckStatus prev, CheckStatus cur, bool expect)
    {
        Assert.Equal(expect, AlertIncidentTriggers.ShouldOpenNewIncident(prev, cur));
    }
}
