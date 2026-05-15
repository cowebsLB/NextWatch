namespace NextWatch.Core.Checks;

/// <summary>
/// Parses optional HTTP expected-status expressions for <see cref="HttpCheckParams"/>.
/// </summary>
public static class HttpExpectedStatuses
{
    /// <summary>
    /// Determines whether <paramref name="statusCode"/> is acceptable for the HTTP check parameters.
    /// When <see cref="HttpCheckParams.ExpectedStatuses"/> is set but contains no valid segments,
    /// falls back to legacy behaviour: codes in [200, 399].
    /// When unset and <see cref="HttpCheckParams.ExpectedStatusCode"/> is set (legacy), requires an exact match.
    /// When both are unset, accepts [200, 399].
    /// </summary>
    public static bool Accepts(HttpCheckParams p, int statusCode)
    {
        if (!string.IsNullOrWhiteSpace(p.ExpectedStatuses))
        {
            var (hadAnyValidRule, matched) = EvaluateRules(statusCode, p.ExpectedStatuses);
            if (!hadAnyValidRule)
                return statusCode >= 200 && statusCode < 400;
            return matched;
        }

        if (p.ExpectedStatusCode.HasValue)
            return statusCode == p.ExpectedStatusCode.Value;

        return statusCode >= 200 && statusCode < 400;
    }

    /// <summary>For tests: evaluates comma-separated segments only.</summary>
    public static (bool HadAnyValidRule, bool Matched) EvaluateRules(int statusCode, string expression)
    {
        var hadAnyValidRule = false;
        foreach (var part in expression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParseSegment(part, out var lo, out var hi))
                continue;

            hadAnyValidRule = true;
            if (statusCode >= lo && statusCode <= hi)
                return (true, true);
        }

        return (hadAnyValidRule, false);
    }

    private static bool TryParseSegment(string segment, out int lo, out int hi)
    {
        lo = hi = 0;
        var dash = segment.IndexOf('-');
        if (dash >= 0)
        {
            if (!int.TryParse(segment.AsSpan(0, dash), out lo))
                return false;
            if (!int.TryParse(segment.AsSpan(dash + 1), out hi))
                return false;
            if (lo > hi)
                (lo, hi) = (hi, lo);
            return lo is >= 100 and <= 599 && hi is >= 100 and <= 599;
        }

        if (!int.TryParse(segment, out var exact))
            return false;
        if (exact is < 100 or > 599)
            return false;

        lo = hi = exact;
        return true;
    }
}
