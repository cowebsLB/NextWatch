using NextWatch.Core.Checks;
using Xunit;

namespace NextWatch.Core.Tests;

public class HttpExpectedStatusesTests
{
    [Fact]
    public void Accepts_CombinedRangeAndAuth_Allows302And401And403()
    {
        var p = new HttpCheckParams { ExpectedStatuses = "200-399,401,403" };
        Assert.True(HttpExpectedStatuses.Accepts(p, 302));
        Assert.True(HttpExpectedStatuses.Accepts(p, 401));
        Assert.True(HttpExpectedStatuses.Accepts(p, 403));
        Assert.False(HttpExpectedStatuses.Accepts(p, 500));
    }

    [Fact]
    public void Accepts_DefaultMissingCodes_Allows200Through399()
    {
        var p = new HttpCheckParams();
        Assert.True(HttpExpectedStatuses.Accepts(p, 200));
        Assert.True(HttpExpectedStatuses.Accepts(p, 302));
        Assert.False(HttpExpectedStatuses.Accepts(p, 401));
        Assert.False(HttpExpectedStatuses.Accepts(p, 500));
    }

    [Fact]
    public void Accepts_LegacyExact200_Rejects401()
    {
        var p = new HttpCheckParams { ExpectedStatusCode = 200 };
        Assert.True(HttpExpectedStatuses.Accepts(p, 200));
        Assert.False(HttpExpectedStatuses.Accepts(p, 401));
    }

    [Fact]
    public void Accepts_ExpectedStatuses_Allows401()
    {
        var p = new HttpCheckParams { ExpectedStatuses = "401" };
        Assert.True(HttpExpectedStatuses.Accepts(p, 401));
        Assert.False(HttpExpectedStatuses.Accepts(p, 200));
    }

    [Fact]
    public void Accepts_Range301To302()
    {
        var p = new HttpCheckParams { ExpectedStatuses = "301-302" };
        Assert.True(HttpExpectedStatuses.Accepts(p, 301));
        Assert.True(HttpExpectedStatuses.Accepts(p, 302));
        Assert.False(HttpExpectedStatuses.Accepts(p, 200));
    }

    [Fact]
    public void Accepts_InvalidSegmentsOnly_FallsBackTo200399()
    {
        var p = new HttpCheckParams { ExpectedStatuses = "bogus,nope" };
        Assert.True(HttpExpectedStatuses.Accepts(p, 302));
        Assert.False(HttpExpectedStatuses.Accepts(p, 401));
    }

    [Fact]
    public void EvaluateRules_MixedValidInvalid_IgnoresInvalid()
    {
        var (had, matched) = HttpExpectedStatuses.EvaluateRules(401, "junk,401");
        Assert.True(had);
        Assert.True(matched);
    }
}
