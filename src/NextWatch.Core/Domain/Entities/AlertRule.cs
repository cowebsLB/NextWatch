namespace NextWatch.Core.Domain.Entities;

public sealed class AlertRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CheckId { get; set; }
    public CheckDefinition? Check { get; set; }
    public bool ToastEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; }
    public bool WebhookEnabled { get; set; }
    public string? WebhookUrl { get; set; }
    public int RepeatMinutes { get; set; } = 15;
}
