# NextWatch

[![Build](https://github.com/cowebsLB/NextWatch/actions/workflows/build.yml/badge.svg)](https://github.com/cowebsLB/NextWatch/actions/workflows/build.yml)
[![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)](https://github.com/cowebsLB/NextWatch)

> Local, portable Windows tray monitor for hosts and services — SQLite history, no cloud account, not a mini-PRTG.

NextWatch answers **“is my LAN / router / box alive?”** with ping/HTTP/TCP/SSL checks, optional SNMP and discovery, and alerts that fire on **state changes** (not every poll while something stays down).

---

## Contents

- [Screenshots](#screenshots)
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Documentation](#documentation)
- [Development](#development)
- [Contributing](#contributing)
- [License](#license)

---

## Screenshots

There are **no in-repo screenshots yet**. If you add captures, drop them under `docs/assets/` (for example `docs/assets/overview.png`) and reference them here with standard Markdown:

```markdown
![Overview](docs/assets/overview.png)
```

Pull requests that document the UI are welcome.

---

## Features

### Monitoring

- **Ping**, **HTTP/HTTPS** (status rules + optional keyword), **TCP**, **SSL** expiry
- **DNS**, optional **SNMP v2c**, **bandwidth** (SNMP counters or Windows perf counters)

HTTP checks support **`ExpectedStatuses`** (codes/ranges), optional **[Basic auth](https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication#basic_authentication_scheme)** (`Username` / `Password` in check JSON), and gateway-friendly defaults for **new** targets added in the UI — details in [docs/index.md](docs/index.md).

### Operations & UX

- **Overview** grid, **tags**, **onboarding**
- **Discovery:** ICMP subnet sweep — IPv4 subnets from adapters (Wi‑Fi / Ethernet / VPN), optional manual CIDR, structured logs in the **Logs** tab and log files
- **Logs** tab: live Serilog stream; clear / copy / open log folder
- **CSV / HTML** snapshots; **diagnostics ZIP** (database + logs)

### Alerts & sharing

- **Tray** status + balloon (**transition-aware** — no spam every poll while still DOWN)
- **Webhooks** (optional), repeat-until-ack, mute
- **Config JSON** export/import (secrets sanitized on export)
- **Trusted-LAN viewer** (optional Kestrel dashboard — **off** by default; see docs for security notes)

### Stack

- **.NET 8**, **WPF**, **EF Core** + **SQLite**, **Serilog**

---

## Installation

1. Download **`NextWatch-win-x64.zip`** from [**Releases**](https://github.com/cowebsLB/NextWatch/releases).
2. Extract and run **`NextWatch.exe`**.
3. **Requires:** Windows 10 or 11 and the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).

First launch runs **onboarding** (first host); default checks are **Ping** + **HTTP**.

**Updates:** In the app, open **Settings** → **Check for updates** — compares to GitHub Releases and opens the download page (no built-in patcher).

Current release line: **0.1.6** — see [`VERSION`](VERSION), [`CHANGELOG.md`](CHANGELOG.md), [docs/versioning.md](docs/versioning.md). List tags locally with `git tag -l "v*"`.

---

## Usage

### Command-line flags

| Flag | Effect |
|------|--------|
| `--portable` | Database and rolling logs under `./data/` next to the executable (USB-friendly) |
| `--minimized` | Start minimized to the system tray only |

```powershell
.\NextWatch.exe --portable
```

### Where data is stored

| Mode | Database | Logs |
|------|----------|------|
| Default | `%AppData%\NextWatch\data.db` | `%AppData%\NextWatch\logs\` |
| `--portable` | `./data/data.db` | `./data/logs/` |

---

## Documentation

| Doc | Contents |
|-----|----------|
| [docs/index.md](docs/index.md) | User guide: checks, Discovery, alerts, exports, LAN viewer |
| [docs/architecture.md](docs/architecture.md) | Scheduler, storage, alert engine, extension points |
| [docs/versioning.md](docs/versioning.md) | Version and tag conventions |
| [CHANGELOG.md](CHANGELOG.md) | Release notes |

---

## Development

```powershell
git clone https://github.com/cowebsLB/NextWatch.git
cd NextWatch
dotnet restore
dotnet build
dotnet test tests/NextWatch.Core.Tests
```

Run the desktop app:

```powershell
dotnet run --project src/NextWatch.Desktop
```

Portable mode while developing:

```powershell
dotnet run --project src/NextWatch.Desktop -- --portable
```

Publish example (win-x64, folder layout):

```powershell
dotnet publish src/NextWatch.Desktop -c Release -r win-x64 --self-contained -p:PublishSingleFile=false -o ./publish
```

Ship the `./publish/` folder; end users run `NextWatch.exe`. Pair with **`--portable`** if `./data/` should stay beside the binary.

---

## Contributing

See [**CONTRIBUTING.md**](CONTRIBUTING.md).

**Issues & ideas:** [github.com/cowebsLB/NextWatch/issues](https://github.com/cowebsLB/NextWatch/issues)

---

## License

Distributed under the **MIT License**. See [LICENSE](LICENSE).

**Repository:** [github.com/cowebsLB/NextWatch](https://github.com/cowebsLB/NextWatch)
