namespace ActivityTracker.Core.Models;

/// <summary>
/// Represents a single continuous usage session of an application.
/// Maps to the AppUsage table in SQLite.
/// </summary>
public class AppUsage
{
    public long Id { get; set; }

    /// <summary>Process name, e.g. "devenv.exe", "chrome.exe".</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Optional window title for future extension (e.g. URL or document name).</summary>
    public string? WindowTitle { get; set; }

    /// <summary>UTC timestamp when this usage session began.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Duration of the session in whole seconds.</summary>
    public int DurationSeconds { get; set; }
}
