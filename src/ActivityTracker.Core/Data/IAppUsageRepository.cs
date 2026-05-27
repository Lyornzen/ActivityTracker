using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Data;

/// <summary>
/// Repository interface for application usage records.
/// </summary>
public interface IAppUsageRepository
{
    /// <summary>Insert a single usage record.</summary>
    Task InsertAsync(AppUsage record);

    /// <summary>
    /// Get total usage seconds for today (UTC date boundary).
    /// </summary>
    Task<int> GetTodayTotalSecondsAsync();

    /// <summary>
    /// Get the top N apps by total usage seconds for today.
    /// </summary>
    Task<List<AppUsageSummary>> GetTodayTopAppsAsync(int topN = 5);

    /// <summary>
    /// Get daily total seconds for each day in the specified range.
    /// </summary>
    Task<List<DailySummary>> GetDailySummariesAsync(DateTime from, DateTime to);

    /// <summary>
    /// Get hourly usage breakdown for a specific app on a specific day.
    /// Returns 24-element list (index = hour 0-23).
    /// </summary>
    Task<List<int>> GetHourlyDistributionAsync(string processName, DateTime date);

    /// <summary>
    /// Get all distinct process names recorded in the database.
    /// </summary>
    Task<List<string>> GetAllProcessNamesAsync();

    /// <summary>
    /// Delete records older than the specified date.
    /// </summary>
    Task<int> PruneOlderThanAsync(DateTime cutoff);

    /// <summary>
    /// Get total usage seconds for a specific process today.
    /// </summary>
    Task<int> GetProcessTodayTotalAsync(string processName);
}
