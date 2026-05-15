# NextWatch Documentation

## Overview

NextWatch is a **local, portable** Windows network monitor. It runs from the system tray, stores history in SQLite on your PC, and deliberately avoids cloud accounts or NMS-style complexity.

| Principle | Meaning |
|-----------|---------|
| Local | Data stays on your machine |
| Simple | One app, one database, clear screens |
| Portable | Copy the folder and run |
| Fast | Lean scheduler, low idle overhead |

Repository: [github.com/cowebsLB/NextWatch](https://github.com/cowebsLB/NextWatch)

## Install

1. Download `NextWatch-win-x64.zip` from [Releases](https://github.com/cowebsLB/NextWatch/releases) or build from source (see [README](../README.md)).
2. Extract the folder.
3. Run `NextWatch.exe`.

**Requirements:** Windows 10/11, [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).

## Command-line options

| Flag | Effect |
|------|--------|
| `--portable` | Store database and logs in `./data/` next to the executable |
| `--minimized` | Start in the tray only (no main window) |

Example:

```text
NextWatch.exe --portable
```

## Data locations

| Mode | Database | Logs |
|------|----------|------|
| Installed (default) | `%AppData%\NextWatch\data.db` | `%AppData%\NextWatch\logs\` |
| Portable (`--portable`) | `./data/data.db` | `./data/logs/` |

Portable flags are applied at startup and saved to `AppSettings` so Settings and diagnostics use the same paths.

## First run

1. Onboarding wizard prompts for a first host.
2. Default checks: **ping** (60s) and **HTTP** (120s).
3. Overview shows aggregate status; tray icon tooltip includes version and health.

## Monitoring checks

| Type | What it does |
|------|----------------|
| Ping | ICMP latency and reachability |
| HTTP/HTTPS | Status code, optional keyword |
| TCP | Port open/closed |
| SSL | Certificate expiry warning |
| SNMP (v2c) | Device reachability via `sysUpTime` |
| DNS | Hostname resolution |
| Bandwidth | SNMP interface counters or local perf counters |

SNMP v3 is planned after v2c is stable.

## Alerts

- **Toast** (tray balloon) on DOWN/WARN
- **Webhook** (optional per rule): Slack/Discord-friendly JSON `{ "text": "..." }`
- **Repeat:** re-notifies on open incidents until acknowledged (interval from rule, default 15 min)
- **Mute:** tray “Mute alerts 1h” or per-target **mute until** datetime

Webhook rules only fire when `WebhookEnabled` is true. The URL uses the rule’s URL, then falls back to the global default in settings — never the default when webhooks are disabled.

## Config backup

Settings → **Export config** / **Import config** (JSON).

- Exports targets, checks, and alert rule flags (not webhook URLs or SNMP secrets).
- Secret fields in check parameters (`community`, `password`, `secret`) are **removed** from the file — re-enter SNMP communities after import.
- Import adds targets; it does not merge-delete existing rows (import into a fresh install or after manual cleanup if replacing everything).

## Diagnostics export

Settings → **Export diagnostics** creates a ZIP of:

- Database file (`data.db`)
- Log files from the active data directory

Uses **runtime** portable paths (not stale DB flags), so `--portable` exports from `./data/` correctly.

## Trusted-LAN viewer

Settings → enable **trusted-LAN viewer** (default **off**).

This is **not real authentication**. Anyone on your LAN who can reach the port can read monitor status. Use only on networks you trust.

- Default port: `5080`
- Optional shared secret: HTTP header `X-NextWatch-Secret`
- Browser: `http://<your-pc-ip>:5080/dashboard`
- Live updates on the LAN page poll every 10s (SignalR on LAN only; WPF uses in-process events)

## Export reports

| Format | Contents |
|--------|----------|
| CSV | Recent check results (up to 5000 rows) |
| HTML | Snapshot table of targets and last status |

## Updates

Settings → **Check for updates** calls the GitHub Releases API. If a newer version exists, your browser opens the release page. There is **no self-patcher** in v1.

## Build from source

```powershell
git clone https://github.com/cowebsLB/NextWatch.git
cd NextWatch
dotnet restore src/NextWatch.Desktop/NextWatch.Desktop.csproj
dotnet build src/NextWatch.Desktop/NextWatch.Desktop.csproj
dotnet test tests/NextWatch.Core.Tests/NextWatch.Core.Tests.csproj
```

## Logs and troubleshooting

| Issue | What to try |
|-------|-------------|
| Ping always down | Target may block ICMP; add a TCP check on port 80/443 |
| SNMP fails | UDP 161 open on device; v2c community correct |
| Diagnostics zip empty | Run with `--portable` if data is under `./data/` |
| Webhook not firing | Enable webhook on the alert rule; set URL or global default |
| LAN viewer unreachable | Windows Firewall allow port 5080; viewer enabled in Settings |

Logs: see [Data locations](#data-locations) above.

## Further reading

- [architecture.md](architecture.md) — projects, scheduler, data model
- [versioning.md](versioning.md) — releases and tags
- [signing.md](signing.md) — optional Authenticode signing
