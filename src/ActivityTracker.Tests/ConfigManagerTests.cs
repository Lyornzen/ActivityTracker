using ActivityTracker.Core.Configuration;
using ActivityTracker.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ActivityTracker.Tests;

public class ConfigManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConfigManager _configManager;

    public ConfigManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ActivityTracker_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var logger = Mock.Of<ILogger<ConfigManager>>();
        _configManager = new ConfigManager(_tempDir, logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Load_NoFileExists_ReturnsDefaults()
    {
        var config = _configManager.Load();

        Assert.Equal(5, config.ScanIntervalSeconds);
        Assert.Equal(30, config.RetentionDays);
        Assert.False(config.AutoStart);
        Assert.False(config.IsPaused);
        Assert.Empty(config.BlacklistedProcesses);
        Assert.Empty(config.WhitelistedProcesses);
    }

    [Fact]
    public void Save_Then_Load_RoundTrip()
    {
        var config = new AppConfig
        {
            ScanIntervalSeconds = 10,
            RetentionDays = 60,
            AutoStart = true,
            IsPaused = false,
            BlacklistedProcesses = new List<string> { "explorer.exe", "cmd.exe" },
            WhitelistedProcesses = new List<string> { "chrome.exe" }
        };

        _configManager.Save(config);
        var loaded = _configManager.Load();

        Assert.Equal(10, loaded.ScanIntervalSeconds);
        Assert.Equal(60, loaded.RetentionDays);
        Assert.True(loaded.AutoStart);
        Assert.Equal(2, loaded.BlacklistedProcesses.Count);
        Assert.Contains("explorer.exe", loaded.BlacklistedProcesses);
        Assert.Single(loaded.WhitelistedProcesses);
    }

    [Fact]
    public void Load_MalformedFile_ReturnsDefaults()
    {
        var configPath = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(configPath, "{ this is not valid json !!!");

        var config = _configManager.Load();

        // Should fall back to defaults
        Assert.Equal(5, config.ScanIntervalSeconds);
    }
}
