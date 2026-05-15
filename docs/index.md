# NextWatch Documentation

## Overview

NextWatch monitors hosts and services from the Windows system tray. Data stays local in SQLite.

## Install

1. Download `NextWatch-win-x64.zip` from [Releases](https://github.com/cowebsLB/NextWatch/releases) (when published) or build from source.
2. Extract the folder.
3. Run `NextWatch.exe`.

## Portable mode

```text
NextWatch.exe --portable
```

Database and logs: `./data/`

## First run

The onboarding wizard adds your first host with ping + HTTP checks.

## Trusted-LAN viewer

Settings → enable **trusted-LAN viewer**. This is **not real authentication** — anyone on your LAN who can reach the port can read status. Use only on networks you trust. Optional shared secret blocks casual browsing.

Default port: `5080`. Open `http://<your-pc-ip>:5080/dashboard` from another device.

## Config backup

Settings → Export/Import config (JSON). Secrets are stripped — re-enter SNMP communities after import.

## Updates

Settings → **Check for updates** opens the GitHub release page if a newer version exists.

## Logs

`%AppData%\NextWatch\logs\` or `./data/logs/` in portable mode.

See [architecture.md](architecture.md) for internals.
