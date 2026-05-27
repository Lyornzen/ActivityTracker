using ActivityTracker.Core.Data;
using ActivityTracker.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ActivityTracker.Tests;

public class RepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly AppUsageRepository _repository;

    public RepositoryTests()
    {
        // Use a temp file database — more realistic and avoids in-memory connection-string quirks
        _dbPath = Path.Combine(Path.GetTempPath(), $"ActivityTracker_test_{Guid.NewGuid():N}.db");

        var dbInit = new DatabaseInitializer(_dbPath);
        dbInit.InitializeAsync().GetAwaiter().GetResult();

        var logger = Mock.Of<ILogger<AppUsageRepository>>();
        _repository = new AppUsageRepository(dbInit, logger);
    }

    public void Dispose()
    {
        _repository.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task Insert_And_Query_TodayTotal()
    {
        // Arrange
        var record = new AppUsage
        {
            ProcessName = "test.exe",
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            DurationSeconds = 600
        };

        // Act
        await _repository.InsertAsync(record);
        var total = await _repository.GetTodayTotalSecondsAsync();

        // Assert
        Assert.Equal(600, total);
    }

    [Fact]
    public async Task GetTodayTopApps_ReturnsCorrectOrder()
    {
        // Arrange
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "chrome.exe", StartTime = DateTime.UtcNow.AddMinutes(-30), DurationSeconds = 1000 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "devenv.exe", StartTime = DateTime.UtcNow.AddMinutes(-20), DurationSeconds = 2000 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "chrome.exe", StartTime = DateTime.UtcNow.AddMinutes(-10), DurationSeconds = 500 });

        // Act
        var topApps = await _repository.GetTodayTopAppsAsync(3);

        // Assert
        Assert.Equal(2, topApps.Count);
        Assert.Equal("devenv.exe", topApps[0].ProcessName); // 2000s first
        Assert.Equal(2000, topApps[0].TotalSeconds);
        Assert.Equal("chrome.exe", topApps[1].ProcessName); // 1500s second
        Assert.Equal(1500, topApps[1].TotalSeconds);
    }

    [Fact]
    public async Task GetDailySummaries_ReturnsCorrectRange()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        await _repository.InsertAsync(new AppUsage
            { ProcessName = "app.exe", StartTime = today.AddHours(10), DurationSeconds = 3600 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "app.exe", StartTime = yesterday.AddHours(10), DurationSeconds = 1800 });

        // Act
        var summaries = await _repository.GetDailySummariesAsync(yesterday, today.AddDays(1));

        // Assert
        Assert.Equal(2, summaries.Count);
        Assert.Equal(1800, summaries[0].TotalSeconds);
        Assert.Equal(3600, summaries[1].TotalSeconds);
    }

    [Fact]
    public async Task GetHourlyDistribution_BucketsCorrectly()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "app.exe", StartTime = today.AddHours(9).AddMinutes(0), DurationSeconds = 600 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "app.exe", StartTime = today.AddHours(9).AddMinutes(30), DurationSeconds = 300 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "app.exe", StartTime = today.AddHours(14).AddMinutes(0), DurationSeconds = 1200 });

        // Act
        var dist = await _repository.GetHourlyDistributionAsync("app.exe", today);

        // Assert
        Assert.Equal(24, dist.Count);
        Assert.Equal(900, dist[9]);   // 600 + 300
        Assert.Equal(1200, dist[14]); // 1200
        Assert.Equal(0, dist[0]);     // midnight — no usage
    }

    [Fact]
    public async Task PruneOlderThan_RemovesOldRecords()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.Date.AddDays(-10);
        var recentDate = DateTime.UtcNow.Date;

        await _repository.InsertAsync(new AppUsage
            { ProcessName = "old.exe", StartTime = oldDate.AddHours(5), DurationSeconds = 100 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "new.exe", StartTime = recentDate.AddHours(5), DurationSeconds = 200 });

        // Act
        var deleted = await _repository.PruneOlderThanAsync(DateTime.UtcNow.Date.AddDays(-5));

        // Assert
        Assert.Equal(1, deleted);
        var recentTotal = await _repository.GetTodayTotalSecondsAsync();
        Assert.Equal(200, recentTotal);
    }

    [Fact]
    public async Task GetAllProcessNames_ReturnsDistinct()
    {
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "a.exe", StartTime = DateTime.UtcNow, DurationSeconds = 10 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "b.exe", StartTime = DateTime.UtcNow, DurationSeconds = 10 });
        await _repository.InsertAsync(new AppUsage
            { ProcessName = "a.exe", StartTime = DateTime.UtcNow, DurationSeconds = 10 });

        var names = await _repository.GetAllProcessNamesAsync();

        Assert.Equal(2, names.Count);
        Assert.Contains("a.exe", names);
        Assert.Contains("b.exe", names);
    }
}
