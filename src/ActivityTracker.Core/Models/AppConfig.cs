using System.Text.Json.Serialization;

namespace ActivityTracker.Core.Models;

/// <summary>
/// Application configuration persisted as JSON.
/// Blacklist/Whitelist filtering: if Whitelist is non-empty, only whitelisted
/// processes are recorded; blacklisted processes are always excluded regardless.
/// </summary>
public class AppConfig
{
    /// <summary>Interval in seconds between foreground window polls. Default 5.</summary>
    public int ScanIntervalSeconds { get; set; } = 5;

    /// <summary>Number of days to retain usage records. Older records are pruned on startup.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>Whether to register the app for Windows autostart.</summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>When true, monitoring is paused (no records written).</summary>
    public bool IsPaused { get; set; } = false;

    /// <summary>Process names (case-insensitive) that are always excluded from tracking.</summary>
    public List<string> BlacklistedProcesses { get; set; } = new();

    /// <summary>
    /// Process names (case-insensitive) that are exclusively tracked.
    /// When empty, all processes (except blacklisted) are tracked.
    /// </summary>
    public List<string> WhitelistedProcesses { get; set; } = new();
}
