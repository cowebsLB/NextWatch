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
7. On **transition** into DOWN/WARN (or WARN↔DOWN), invokes `ProcessStatusChangeAsync`; when status returns **OK**, open `AlertEvent` rows for that check are acknowledged automatically.

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

Parameters are JSON in `CheckDefinition.ParametersJson` (see `CheckParameters` DTOs). HTTP checks support optional **Basic** credentials (`Username`/`Password`), `ExpectedStatuses`, keyword match, and legacy exact `ExpectedStatusCode`.

## Discovery

`DiscoveryService` ICMP-probes subnet ranges; structured logs at Information for scan lifecycle (“subnet scan started/completed”, each reachable host, merged counts for multi-subnet scans). `GetDetectedIpv4Networks()` exposes **CIDR + NIC description** for the Desktop Discovery tab.

## Alerts

`AlertEngine`:

- `ProcessStatusChangeAsync` — transition into DOWN/WARN; supersedes prior open incidents for the same check; creates `AlertEvent`; notifies sink (`ToastEnabled` respected).
- `ProcessRepeatsAsync` — repeats for still-open incidents on the `RepeatMinutes` cadence (first repeat after **RepeatMinutes** from `FiredAtUtc`, then every **RepeatMinutes**).
- `ResolveWebhookUrl(rule, settings)` — shared webhook resolution:
  - `null` if `WebhookEnabled` is false or rule is null
  - `rule.WebhookUrl ?? settings.DefaultWebhookUrl` when enabled

`WpfAlertSink` (Desktop): tray balloon when `ToastEnabled` + delegates webhook POST to `WebhookAlertSink`.

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

Database bootstrap: on startup, **`MigrateAsync`** applies `Data/Migrations`. Existing installs that used **`EnsureCreated`** (no `__EFMigrationsHistory`) but already have the **`Targets`** table are **baselined** once: history is stamped with `InitialCreate` so migrate does not recreate tables. New installs apply the migration normally.

Design-time: **`NextWatchDesignTimeDbContextFactory`** + `dotnet ef` (`dotnet-tools.json`) — `dotnet ef migrations add` targets `NextWatch.Core`.

## Config export security

`ConfigExportService.ExportAsync` calls `ConfigSecretsSanitizer.SanitizeParameters` for every check’s `ParametersJson`.

`ConfigSecretsSanitizer` parses parameter JSON and **removes** properties named `community`, `password`, or `secret` (case-insensitive). Values are not redacted in place — the keys are dropped so exported files cannot contain SNMP communities or similar secrets.

`ConfigExportServiceTests` guards this wiring so export cannot regress to string-replace-only sanitization.

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
