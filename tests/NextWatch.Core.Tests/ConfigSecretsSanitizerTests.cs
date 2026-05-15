using NextWatch.Core.Services;
using Xunit;

namespace NextWatch.Core.Tests;

public class ConfigSecretsSanitizerTests
{
  [Fact]
  public void SanitizeParameters_RemovesCommunityValue()
  {
    const string input = """{"Community":"mySecret","Port":161}""";
    var result = ConfigSecretsSanitizer.SanitizeParameters(input);
    Assert.NotNull(result);
    Assert.DoesNotContain("mySecret", result, StringComparison.Ordinal);
    Assert.DoesNotContain("community", result, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("161", result, StringComparison.Ordinal);
  }

  [Fact]
  public void SanitizeParameters_LeavesNonSecretFields()
  {
    const string input = """{"Url":"http://localhost","ExpectedStatusCode":200}""";
    var result = ConfigSecretsSanitizer.SanitizeParameters(input);
    Assert.Equal(input, result);
  }
}
