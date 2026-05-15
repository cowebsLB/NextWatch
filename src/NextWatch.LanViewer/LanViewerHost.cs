using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Data;
using NextWatch.Core.Scheduling;

namespace NextWatch.LanViewer;

public sealed class LanViewerHost
{
    private WebApplication? _app;

    public async Task StartAsync(IServiceProvider rootProvider, int port, string? sharedSecret, CancellationToken ct)
    {
        if (_app is not null)
            return;

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(rootProvider);
        builder.Services.AddSignalR();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        var app = builder.Build();
        app.Use(async (ctx, next) =>
        {
            if (!string.IsNullOrEmpty(sharedSecret))
            {
                if (!ctx.Request.Headers.TryGetValue("X-NextWatch-Secret", out var val) || val != sharedSecret)
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
            await next();
        });

        app.MapGet("/", () => Results.Redirect("/dashboard"));
        app.MapGet("/api/status", async () =>
        {
            using var scope = rootProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
            var targets = await db.Targets.Include(t => t.Checks).AsNoTracking().ToListAsync();
            return Results.Json(targets.Select(t => new
            {
                t.Name,
                t.Tag,
                t.Host,
                Checks = t.Checks.Select(c => new { c.Type, c.LastStatus })
            }));
        });

        app.MapHub<StatusHub>("/hub");
        app.MapGet("/dashboard", async ctx =>
        {
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(LanDashboardHtml.Page);
        });

        await app.StartAsync(ct);
        _app = app;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_app is not null)
        {
            await _app.StopAsync(ct);
            await _app.DisposeAsync();
            _app = null;
        }
    }
}

public sealed class StatusHub : Hub
{
    public override Task OnConnectedAsync()
    {
        var logger = Context.GetHttpContext()?.RequestServices.GetService<ILogger<StatusHub>>();
        logger?.LogInformation("LAN viewer connected from {IP}", Context.GetHttpContext()?.Connection.RemoteIpAddress);
        return base.OnConnectedAsync();
    }
}

internal static class LanDashboardHtml
{
    public const string Page = """
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <title>NextWatch — Trusted LAN view</title>
  <style>
    body { font-family: Segoe UI, sans-serif; background: #0f1115; color: #e8e8e8; margin: 2rem; }
    h1 { font-size: 1.4rem; }
    .warn { color: #f0ad4e; font-size: 0.9rem; }
    table { border-collapse: collapse; width: 100%; margin-top: 1rem; }
    td, th { border: 1px solid #333; padding: 0.5rem; text-align: left; }
    .ok { color: #5cb85c; } .down { color: #d9534f; } .warn { color: #f0ad4e; }
  </style>
</head>
<body>
  <h1>NextWatch</h1>
  <p class="warn">Read-only trusted-LAN view. Not authenticated security.</p>
  <table id="t"><tr><th>Target</th><th>Tag</th><th>Check</th><th>Status</th></tr></table>
  <script>
    async function load() {
      const r = await fetch('/api/status');
      const data = await r.json();
      const t = document.getElementById('t');
      t.innerHTML = '<tr><th>Target</th><th>Tag</th><th>Check</th><th>Status</th></tr>';
      data.forEach(row => row.checks.forEach(c => {
        const tr = document.createElement('tr');
        tr.innerHTML = `<td>${row.name}</td><td>${row.tag||''}</td><td>${c.type}</td><td class="${(c.lastStatus+'').toLowerCase()}">${c.lastStatus}</td>`;
        t.appendChild(tr);
      }));
    }
    load(); setInterval(load, 10000);
  </script>
</body>
</html>
""";
}
