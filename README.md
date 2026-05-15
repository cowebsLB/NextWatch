# NextWatch

[![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**NextWatch** is a local, portable Windows network monitor. Watch hosts and services from the system tray — not a mini-PRTG.

Licensed under the MIT License.

## Features

- Ping, HTTP/HTTPS, TCP, and SSL expiry checks
- System tray with live status
- Tags, onboarding, config JSON export/import
- Webhook alerts with repeat-until-ack
- Optional SNMP v2c, bandwidth, subnet discovery, DNS checks
- Trusted-LAN read-only dashboard (off by default)
- Portable folder mode (`--portable`)

## Requirements

- Windows 10/11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (desktop)

## Build

```powershell
git clone https://github.com/cowebsLB/NextWatch.git
cd NextWatch
dotnet restore
dotnet build
dotnet test
```

## Run (development)

```powershell
dotnet run --project src/NextWatch.Desktop
```

Portable:

```powershell
dotnet run --project src/NextWatch.Desktop -- --portable
```

## Publish

```powershell
dotnet publish src/NextWatch.Desktop -c Release -r win-x64 --self-contained -p:PublishSingleFile=false -o ./publish
```

Copy `publish/` anywhere. Use `--portable` so data lives in `./data/`.

## Updates

Help → **Check for updates** compares against [GitHub Releases](https://github.com/cowebsLB/NextWatch/releases) and opens the download page (no self-patcher in v1).

## Docs

See [docs/index.md](docs/index.md) and [docs/architecture.md](docs/architecture.md).

## Versioning

Current version: **0.1.4** (see [`VERSION`](VERSION), [`CHANGELOG.md`](CHANGELOG.md), [versioning guide](docs/versioning.md)).

```powershell
git tag -l "v*"
```

## Repository

https://github.com/cowebsLB/NextWatch
