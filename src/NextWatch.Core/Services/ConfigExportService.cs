using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextWatch.Core.Data;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Services;

public sealed class ConfigExportDto
{
    public List<MonitorTargetExport> Targets { get; set; } = [];
    public List<AlertRuleExport> AlertRules { get; set; } = [];
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MonitorTargetExport
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<CheckExport> Checks { get; set; } = [];
}

public sealed class CheckExport
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ParametersJson { get; set; }
    public int IntervalSeconds { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class AlertRuleExport
{
    public Guid Id { get; set; }
    public Guid? CheckId { get; set; }
    public bool ToastEnabled { get; set; }
    public bool SoundEnabled { get; set; }
    public bool WebhookEnabled { get; set; }
}

public sealed class ConfigExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<string> ExportAsync(NextWatchDbContext db, CancellationToken ct = default)
    {
        var targets = await db.Targets.Include(t => t.Checks).AsNoTracking().ToListAsync(ct);
        var rules = await db.AlertRules.AsNoTracking().ToListAsync(ct);
        var dto = new ConfigExportDto
        {
            Targets = targets.Select(t => new MonitorTargetExport
            {
                Id = t.Id,
                Name = t.Name,
                Host = t.Host,
                Tag = t.Tag,
                IsEnabled = t.IsEnabled,
                Checks = t.Checks.Select(c => new CheckExport
                {
                    Id = c.Id,
                    Type = c.Type.ToString(),
                    ParametersJson = SanitizeParameters(c.ParametersJson),
                    IntervalSeconds = c.IntervalSeconds,
                    IsEnabled = c.IsEnabled
                }).ToList()
            }).ToList(),
            AlertRules = rules.Select(r => new AlertRuleExport
            {
                Id = r.Id,
                CheckId = r.CheckId,
                ToastEnabled = r.ToastEnabled,
                SoundEnabled = r.SoundEnabled,
                WebhookEnabled = r.WebhookEnabled
            }).ToList()
        };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public async Task ImportAsync(NextWatchDbContext db, string json, CancellationToken ct = default)
    {
        var dto = JsonSerializer.Deserialize<ConfigExportDto>(json, JsonOptions)
                  ?? throw new InvalidOperationException("Invalid config JSON");
        foreach (var t in dto.Targets)
        {
            var target = new MonitorTarget
            {
                Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                Name = t.Name,
                Host = t.Host,
                Tag = t.Tag,
                IsEnabled = t.IsEnabled
            };
            db.Targets.Add(target);
            foreach (var c in t.Checks)
            {
                if (!Enum.TryParse<Domain.CheckType>(c.Type, out var type))
                    continue;
                db.Checks.Add(new CheckDefinition
                {
                    Id = c.Id == Guid.Empty ? Guid.NewGuid() : c.Id,
                    TargetId = target.Id,
                    Type = type,
                    ParametersJson = c.ParametersJson,
                    IntervalSeconds = c.IntervalSeconds,
                    IsEnabled = c.IsEnabled,
                    NextRunUtc = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static string? SanitizeParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        return json.Replace("\"community\"", "\"community_redacted\"", StringComparison.OrdinalIgnoreCase);
    }
}
