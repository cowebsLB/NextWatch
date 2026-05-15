# NextWatch Architecture

## Solution layout

```
NextWatch.slnx
src/
  NextWatch.Core/        # Domain, checks, scheduler, EF Core, services
  NextWatch.Desktop/     # WPF + tray + Generic Host
  NextWatch.LanViewer/   # Embedded Kestrel read-only API
tests/
  NextWatch.Core.Tests/
```

All monitoring logic lives in **Core**. Desktop and LanViewer are thin hosts.

## Runtime configuration

`NextWatchRuntimeOptions` (singleton) is set at startup from CLI flags:

| Property | Source |
|----------|--------|
| `PortableDataPath` | `--portable` present |
| `PortableDataDirectory` | `.\data` next to exe when portable |

`DatabaseInitializer` syncs these values into the `AppSettings` row after the database is created or migrated.

`NextWatchPaths` resolves database and log directories from runtime options (and is used by Serilog, EF, and diagnostics export).

## Scheduler

`CheckSchedulerService` (`BackgroundService`):

1. Loads due checks (`NextRunUtc <= now`, enabled target).
2. Runs up to **20** checks in parallel.
3. Executes via `ICheckExecutor` per `CheckType`.
4. Applies **hysteresis** (`ApplyHysteresis`) before persisting status.
5. Writes `CheckResult`, updates `CheckDefinition.LastStatus`.
6. Publishes `ICheckStatusNotifier.StatusChanged` for WPF.
7. Invokes `IAlertEngine.ProcessStatusChangeAsync` on DOWN/WARN.

`RetentionService` prunes results older than `AppSettings.RetentionDays` (default 30).

`AlertRepeatService` calls `ProcessRepeatsAsync` every minute for unacknowledged incidents.

## Check executors

| Class | `CheckType` |
|-------|-------------|
| `PingCheckExecutor` | Ping |
| `HttpCheckExecutor` | Http |
| `TcpCheckExecutor` | Tcp |
| `SslCheckExecutor` | Ssl |
| `DnsCheckExecutor` | Dns |
| `SnmpCheckExecutor` | Snmp |
| `BandwidthCheckExecutor` | Bandwidth |

Parameters are JSON in `CheckDefinition.ParametersJson` (see `CheckParameters` DTOs).

## Alerts

`AlertEngine`:

- `ProcessStatusChangeAsync` — new DOWN/WARN, creates `AlertEvent`, notifies sink.
- `ProcessRepeatsAsync` — escalates open incidents.
- `ResolveWebhookUrl(rule, settings)` — shared webhook resolution:
  - `null` if `WebhookEnabled` is false or rule is null
  - `rule.WebhookUrl ?? settings.DefaultWebhookUrl` when enabled

`WpfAlertSink` (Desktop): tray balloon + delegates webhook POST to `WebhookAlertSink`.

## Live UI updates

- **WPF:** `InProcessCheckStatusNotifier` — no SignalR in the desktop app.
- **LAN viewer:** optional Kestrel host + HTML dashboard (polling); SignalR hub registered for future use.

## Data model (v1)

| Entity | Purpose |
|--------|---------|
| `MonitorTarget` | Host/service; optional `Tag` filter string |
| `CheckDefinition` | Type, interval, parameters JSON, hysteresis counters |
| `CheckResult` | Time-series status rows |
| `AlertRule` | Toast/sound/webhook flags per check |
| `AlertEvent` | Fired alerts, ack, repeat count |
| `AppSettings` | Single row: retention, LAN, portable flags, theme, onboarding |

Deferred (add via migration when needed): `TargetGroup`, `CheckResultHourly`, `MaintenanceWindow`.

Database bootstrap: `EnsureCreated` when no EF migrations exist; `Migrate` when migration assemblies are present.

## Config export security

`ConfigSecretsSanitizer.SanitizeParameters` parses check parameter JSON and **removes** properties named `community`, `password`, or `secret` (case-insensitive) before writing export files.

## LAN viewer

`LanViewerBackgroundService` watches `AppSettings.LanViewerEnabled` and starts/stops `LanViewerHost`:

- Binds `http://0.0.0.0:{port}`
- Optional `X-NextWatch-Secret` middleware
- `GET /api/status` — JSON snapshot
- `GET /dashboard` — read-only HTML

## Extension points

1. Implement `ICheckExecutor`.
2. Register in `DependencyInjection.AddNextWatchCore`.
3. Add UI to create checks of the new type (Desktop).

## CI/CD

| Workflow | Trigger |
|----------|---------|
| `build.yml` | Push/PR to `main` |
| `release.yml` | Tag `v*` → publish `NextWatch-win-x64.zip` |
