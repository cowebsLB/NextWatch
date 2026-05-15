using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;
using NextWatch.Core.Scheduling;
using Xunit;

namespace NextWatch.Core.Tests;

public class HysteresisTests
{
    [Fact]
    public void ApplyHysteresis_RequiresConsecutiveFailuresBeforeDown()
    {
        var check = new CheckDefinition
        {
            DownThreshold = 3,
            LastStatus = CheckStatus.Ok
        };

        var s1 = CheckSchedulerService.ApplyHysteresis(check, CheckStatus.Down);
        Assert.Equal(CheckStatus.Ok, s1);
        var s2 = CheckSchedulerService.ApplyHysteresis(check, CheckStatus.Down);
        Assert.Equal(CheckStatus.Ok, s2);
        var s3 = CheckSchedulerService.ApplyHysteresis(check, CheckStatus.Down);
        Assert.Equal(CheckStatus.Down, s3);
    }
}
