# Changelog

All notable changes to NextWatch are documented in this file.

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
