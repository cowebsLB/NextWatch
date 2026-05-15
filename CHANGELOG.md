# Changelog

All notable changes to NextWatch are documented in this file.

## [0.1.5] - 2026-05-15

### Added

- **Logs** tab in the main window: streams Serilog into an in-memory buffer (`InMemoryUiLogSink` / `InMemoryUiLogBuffer`), clear view, copy as TSV, open logs folder
- HTTP checks: configurable expected status codes via `ExpectedStatuses` (comma-separated codes/ranges); default accept **200–399** when unset; legacy `ExpectedStatusCode` preserved (`HttpExpectedStatuses`)
- Discovery: **Scan** with empty CIDR probes **all connected IPv4 subnets** reported by the OS (Ethernet, Wi‑Fi, VPN); manual CIDR still supported (`DiscoveryService.GetConnectedIpv4Networks`, `ScanConnectedNetworksAsync`)
- Alert helpers `AlertIncidentTriggers`, `AlertRepeatSchedule`; unit tests for transitions and repeat timing

### Changed

- Serilog: `MinimumLevel.Debug` with overrides so EF Core SQL and `System.Net.Http` / `HttpClient` stay at **Warning**; file sink unchanged; UI sink added
- Check scheduler logs each run at Information (target, status, latency, message)
- Ping OK messages distinguish **ICMP RTT** vs **check duration**
- `AppSettings` reads use `OrderBy(Id).FirstAsync` for a stable single row

### Fixed

- **Tray/webhook spam:** incidents fire only on **transition** into DOWN/WARN (or WARN↔DOWN), not every poll while still failing
- **Repeat reminders:** first repeat waits **RepeatMinutes** after `FiredAtUtc` (no immediate repeat when `RepeatCount` was 0)
- **Recovery:** returning OK **auto-acks** open `AlertEvent` rows for that check so repeats stop
- **`ToastEnabled`:** tray balloons honor the alert rule; webhooks unchanged
- Alert rules: resolve **check-specific** rule before **global** (`ResolveAlertRule`); supersede prior open incidents when opening a new one for the same check

## [0.1.4] - 2026-05-15

### Added

- EF Core `InitialCreate` migration (`src/NextWatch.Core/Data/Migrations/`)
- `NextWatchDesignTimeDbContextFactory` for `dotnet ef` design-time (`dotnet ef migrations add` against Core)

### Changed

- Database startup uses **`Database.MigrateAsync`** only; legacy SQLite files created with **`EnsureCreated`** are detected (schema present, no migrations history) and **baselined** before migrate so existing user data is preserved

## [0.1.3] - 2026-05-15

### Changed

- GitHub Actions: `actions/checkout@v6`, `actions/setup-dotnet@v5`, `softprops/action-gh-release@v3` (Node 24–compatible action runtimes)

### Fixed

- Analyzer CA1416: non-SNMP bandwidth checks gate Windows perf counters behind `OperatingSystem.IsWindows()` and `[SupportedOSPlatform("windows")]`
- Analyzer CS9113: `AlertEngine` uses `ILogger` when skipping muted or non-alerting paths

## [0.1.2] - 2026-05-15

### Fixed

- `ConfigExportService` now calls `ConfigSecretsSanitizer` on export (v0.1.1 still used a local rename-only helper, so secrets could leak in exported JSON)

### Added

- `ConfigExportServiceTests` integration test asserting exported config has no secret parameter values

## [0.1.1] - 2026-05-15

### Fixed

- Repeat alert webhooks now respect `WebhookEnabled` (shared `ResolveWebhookUrl` with initial alerts)
- Config export removes SNMP/password/secret parameter values instead of renaming keys only
- Diagnostics export uses runtime portable paths when started with `--portable`
- `.gitignore` no longer excludes `src/NextWatch.Core/Data/` on Windows

### Added

- `NextWatchRuntimeOptions` for startup path configuration
- `ConfigSecretsSanitizer` and unit tests for alerts and export sanitization
- Expanded documentation and worklog

## [0.1.0] - 2026-05-15

### Added

- Initial NextWatch desktop app (WPF, .NET 8)
- Ping, HTTP, TCP, SSL checks with scheduler and SQLite history
- Tray icon, overview dashboard, tags, onboarding wizard
- Config export/import, webhook alerts, mute-until
- SNMP v2c, bandwidth, discovery, DNS (power-user features)
- Trusted-LAN read-only viewer with shared secret
- CSV/HTML export, diagnostics zip, GitHub release update check
