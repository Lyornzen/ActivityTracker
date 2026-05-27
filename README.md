# ActivityTracker — Windows Application Usage Time Tracker

[![Build & Publish](https://github.com/<owner>/<repo>/actions/workflows/build.yml/badge.svg)](https://github.com/<owner>/<repo>/actions/workflows/build.yml)

A WPF desktop application that silently records foreground application usage and visualizes the data with charts.

## Prerequisites

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build

```bash
dotnet build ActivityTracker.sln -c Release
```

## Run

```bash
dotnet run --project src/ActivityTracker.UI
```

## Test

```bash
dotnet test ActivityTracker.sln
```

## Publish (single-file framework-dependent)

```bash
dotnet publish src/ActivityTracker.UI -c Release -r win-x64 --self-contained false -o publish
```

Published output: `publish/ActivityTracker.exe`

## Architecture

```
ActivityTracker.Core       — Class library: models, SQLite data layer, monitoring service, config
ActivityTracker.UI         — WPF app: MVVM (CommunityToolkit.Mvvm), ModernWpf Fluent UI, LiveCharts2
ActivityTracker.Tests      — xUnit tests with Moq and temp-file SQLite
```

### Layers

| Layer | Project | Key Types |
|-------|---------|-----------|
| Models | Core | `AppUsage`, `DailySummary`, `AppConfig` |
| Data | Core | `IAppUsageRepository`, `AppUsageRepository`, `DatabaseInitializer` |
| Services | Core | `IMonitoringService`, `MonitoringService`, `WindowDetector` |
| Config | Core | `ConfigManager` (JSON) |
| ViewModels | UI | `DashboardViewModel`, `HistoryViewModel`, `AppDetailViewModel`, `SettingsViewModel` |
| Views | UI | `DashboardPage`, `HistoryPage`, `AppDetailPage`, `SettingsPage` |

### Data Flow

```
Foreground window (Win32 API)
    → WindowDetector.GetForegroundWindowInfo()
    → MonitoringService (timer tick, filter, accumulate)
    → AppUsageRepository.InsertAsync()
    → SQLite (AppUsage table)
    → ViewModels (aggregate queries)
    → LiveCharts2 charts
```

### Configuration

Stored at `%LocalAppData%\ActivityTracker\config.json`:

```json
{
  "ScanIntervalSeconds": 5,
  "RetentionDays": 30,
  "AutoStart": false,
  "IsPaused": false,
  "BlacklistedProcesses": ["explorer.exe"],
  "WhitelistedProcesses": []
}
```

- **Blacklist**: processes always excluded
- **Whitelist**: when non-empty, ONLY listed processes are tracked
- Both are case-insensitive and editable from the Settings page

## License

MIT
