using System.Text;
using Microsoft.EntityFrameworkCore;
using NextWatch.Core.Data;

namespace NextWatch.Core.Services;

public sealed class ReportExportService
{
    public async Task<string> ExportCsvAsync(NextWatchDbContext db, CancellationToken ct = default)
    {
        var results = await db.Results
            .Include(r => r.Check)!.ThenInclude(c => c!.Target)
            .OrderByDescending(r => r.TimestampUtc)
            .Take(5000)
            .AsNoTracking()
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("TimestampUtc,Target,CheckType,Status,LatencyMs,Message");
        foreach (var r in results)
        {
            sb.AppendLine($"{r.TimestampUtc:O},{Escape(r.Check?.Target?.Name)},{r.Check?.Type},{r.Status},{r.LatencyMs},{Escape(r.Message)}");
        }
        return sb.ToString();
    }

    public async Task<string> ExportHtmlSnapshotAsync(NextWatchDbContext db, CancellationToken ct = default)
    {
        var targets = await db.Targets.Include(t => t.Checks).AsNoTracking().ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>NextWatch Snapshot</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,sans-serif;background:#111;color:#eee;padding:2rem}table{border-collapse:collapse;width:100%}td,th{border:1px solid #333;padding:.5rem}</style></head><body>");
        sb.AppendLine($"<h1>NextWatch Snapshot</h1><p>Generated {DateTime.UtcNow:u}</p><table><tr><th>Target</th><th>Tag</th><th>Check</th><th>Status</th></tr>");
        foreach (var t in targets)
        foreach (var c in t.Checks)
            sb.AppendLine($"<tr><td>{EscapeHtml(t.Name)}</td><td>{EscapeHtml(t.Tag)}</td><td>{c.Type}</td><td>{c.LastStatus}</td></tr>");
        sb.AppendLine("</table></body></html>");
        return sb.ToString();
    }

    private static string Escape(string? v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\"";
    private static string EscapeHtml(string? v) => System.Net.WebUtility.HtmlEncode(v ?? "");
}
