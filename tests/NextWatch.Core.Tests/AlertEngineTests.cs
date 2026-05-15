using NextWatch.Core.Alerts;
using NextWatch.Core.Domain.Entities;
using Xunit;

namespace NextWatch.Core.Tests;

public class AlertEngineTests
{
  private static readonly AppSettings Settings = new() { DefaultWebhookUrl = "https://default.example/hook" };

  [Fact]
  public void ResolveWebhookUrl_WhenDisabled_ReturnsNull()
  {
    var rule = new AlertRule { WebhookEnabled = false, WebhookUrl = "https://rule.example/hook" };
    Assert.Null(AlertEngine.ResolveWebhookUrl(rule, Settings));
  }

  [Fact]
  public void ResolveWebhookUrl_WhenEnabled_UsesRuleUrlOrDefault()
  {
    var withUrl = new AlertRule { WebhookEnabled = true, WebhookUrl = "https://rule.example/hook" };
    Assert.Equal("https://rule.example/hook", AlertEngine.ResolveWebhookUrl(withUrl, Settings));

    var withoutUrl = new AlertRule { WebhookEnabled = true, WebhookUrl = null };
    Assert.Equal("https://default.example/hook", AlertEngine.ResolveWebhookUrl(withoutUrl, Settings));
  }

  [Fact]
  public void ResolveWebhookUrl_WhenNoRule_ReturnsNull()
  {
    Assert.Null(AlertEngine.ResolveWebhookUrl(null, Settings));
  }
}
