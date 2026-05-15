# NextWatch Architecture

## Projects

| Project | Role |
|---------|------|
| `NextWatch.Core` | Checks, scheduler, EF Core, alerts, services |
| `NextWatch.Desktop` | WPF UI, tray, Generic Host |
| `NextWatch.LanViewer` | Embedded Kestrel read-only LAN dashboard |
| `NextWatch.Core.Tests` | Unit tests |

## Scheduler

`CheckSchedulerService` polls due checks (priority by `NextRunUtc`), runs up to 20 in parallel, applies hysteresis, writes `CheckResult`, publishes in-process via `ICheckStatusNotifier`.

## Live UI updates

WPF subscribes to `ICheckStatusNotifier` — **no SignalR in the desktop app**. SignalR is used only by the LAN viewer (Phase 4).

## Data

SQLite file with EF migrations. v1 entities: `MonitorTarget`, `CheckDefinition`, `CheckResult`, `AlertRule`, `AlertEvent`, `AppSettings` (typed single row).

## Extension points

Implement `ICheckExecutor` and register in `DependencyInjection.AddNextWatchCore`.
