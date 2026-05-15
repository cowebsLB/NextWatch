# Changelog

All notable changes to NextWatch are documented in this file.

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
