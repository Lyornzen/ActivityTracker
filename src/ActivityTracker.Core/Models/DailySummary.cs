namespace ActivityTracker.Core.Models;

/// <summary>
/// Aggregated usage statistics for a single calendar day.
/// </summary>
public class DailySummary
{
    public DateTime Date { get; set; }
    public int TotalSeconds { get; set; }
    public List<AppUsageSummary> TopApps { get; set; } = new();
}
