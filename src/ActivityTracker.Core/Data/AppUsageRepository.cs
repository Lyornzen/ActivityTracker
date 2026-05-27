using Microsoft.Data.Sqlite;
using ActivityTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace ActivityTracker.Core.Data;

/// <summary>
/// SQLite implementation of IAppUsageRepository.
/// All DateTime values are stored/queried as UTC ISO-8601 strings.
/// </summary>
public class AppUsageRepository : IAppUsageRepository, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<AppUsageRepository> _logger;
    private bool _disposed;

    public AppUsageRepository(DatabaseInitializer dbInit, ILogger<AppUsageRepository> logger)
    {
        _logger = logger;
        _connection = new SqliteConnection(dbInit.ConnectionString);
        _connection.Open();

        // Enable WAL mode for better concurrent read/write performance
        using var walCmd = _connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        walCmd.ExecuteNonQuery();
    }

    // ── Insert ──────────────────────────────────────────────

    public async Task InsertAsync(AppUsage record)
    {
        const string sql = @"
            INSERT INTO AppUsage (ProcessName, WindowTitle, StartTime, DurationSeconds)
            VALUES (@proc, @title, @start, @dur);";

        await using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@proc", record.ProcessName);
        cmd.Parameters.AddWithValue("@title", (object?)record.WindowTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@start", record.StartTime.ToString("O"));
        cmd.Parameters.AddWithValue("@dur", record.DurationSeconds);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Today aggregates ────────────────────────────────────

    public async Task<int> GetTodayTotalSecondsAsync()
    {
        var today = DateTime.UtcNow.Date;
        const string sql = @"
            SELECT COALESCE(SUM(DurationSeconds), 0)
            FROM AppUsage
            WHERE StartTime >= @start AND StartTime < @end;";

        await using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@start", today.ToString("O"));
        cmd.Parameters.AddWithValue("@end", today.AddDays(1).ToString("O"));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);
    }

    public async Task<List<AppUsageSummary>> GetTodayTopAppsAsync(int topN = 5)
    {
        var today = DateTime.UtcNow.Date;
        const string sql = @"
            SELECT ProcessName, SUM(DurationSeconds) AS Total
            FROM AppUsage
            WHERE StartTime >= @start AND StartTime < @end
            GROUP BY ProcessName
            ORDER BY Total DESC
            LIMIT @top;";

        await using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@start", today.ToString("O"));
        cmd.Parameters.AddWithValue("@end", today.AddDays(1).ToString("O"));
        cmd.Parameters.AddWithValue("@top", topN);

        var results = new List<AppUsageSummary>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new AppUsageSummary
            {
                ProcessName = reader.GetString(0),
                TotalSeconds = reader.GetInt32(1)
            });
        }
        return results;
    }

    // ── Daily summaries ─────────────────────────────────────

    public async Task<List<DailySummary>> GetDailySummariesAsync(DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT DATE(StartTime) AS Day, SUM(DurationSeconds) AS Total
            FROM AppUsage
            WHERE StartTime >= @from AND StartTime < @to
            GROUP BY Day
            ORDER BY Day;";

        await using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@from", from.ToString("O"));
        cmd.Parameters.AddWithValue("@to", to.ToString("O"));

        var results = new List<DailySummary>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DailySummary
            {
                Date = DateTime.Parse(reader.GetString(0)),
                TotalSeconds = reader.GetInt32(1)
            });
        }
        return results;
    }

    // ── Hourly distribution ─────────────────────────────────

    public async Task<List<int>> GetHourlyDistributionAsync(string processName, DateTime date)
    {
        var buckets = new int[24];
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        const string sql = @"
            SELECT StartTime, DurationSeconds
            FROM AppUsage
            WHERE ProcessName = @proc
              AND StartTime >= @start AND StartTime < @end;";

        await using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@proc", processName);
        cmd.Parameters.AddWithValue("@start", dayStart.ToString("O"));
        cmd.Parameters.AddWithValue("@end", dayEnd.ToString("O"));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var startTime = DateTime.Parse(reader.GetString(0));
            var duration = reader.GetInt32(1);
            var hour = startTime.Hour;
            if (hour >= 0 && hour < 24)
                buckets[hour] += duration;
        }

        return buckets.ToList();
    }

    // ── Process names ───────────────────────────────────────

    public async Task<List<string>> GetAllProcessNamesAsync()
    {
        const string sql = "SELECT DISTINCT ProcessName FROM AppUsage ORDER BY ProcessName;";
        await using var cmd = new SqliteCommand(sql, _connection);
        var names = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }

    // ── Prune ───────────────────────────────────────────────

    public async Task<int> PruneOlderThanAsync(DateTime cutoff)
    {
        const string sql = "DELETE FROM AppUsage WHERE StartTime < @cutoff;";
        await using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
        return await cmd.ExecuteNonQueryAsync();
    }

    // ── Process today total ─────────────────────────────────

    public async Task<int> GetProcessTodayTotalAsync(string processName)
    {
        var today = DateTime.UtcNow.Date;
        const string sql = @"
            SELECT COALESCE(SUM(DurationSeconds), 0)
            FROM AppUsage
            WHERE ProcessName = @proc
              AND StartTime >= @start AND StartTime < @end;";

        await using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@proc", processName);
        cmd.Parameters.AddWithValue("@start", today.ToString("O"));
        cmd.Parameters.AddWithValue("@end", today.AddDays(1).ToString("O"));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
    }

    // ── Dispose ─────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
