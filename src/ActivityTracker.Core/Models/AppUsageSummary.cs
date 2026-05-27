namespace ActivityTracker.Core.Models;

/// <summary>
/// Lightweight summary of a single application's usage within a day.
/// </summary>
public class AppUsageSummary
{
    public string ProcessName { get; set; } = string.Empty;
    public int TotalSeconds { get; set; }
}
