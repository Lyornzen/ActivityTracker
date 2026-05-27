using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;

namespace ActivityTracker.Tests;

public class MonitoringServiceTests
{
    [Theory]
    [InlineData("chrome.exe", true)]   // not in any list
    [InlineData("explorer.exe", false)] // blacklisted
    [InlineData("cmd.exe", false)]      // blacklisted
    [InlineData("notepad.exe", false)]  // not in whitelist, whitelist is non-empty
    public void IsProcessAllowed_BlacklistWhitelist(string processName, bool expected)
    {
        var config = new AppConfig
        {
            BlacklistedProcesses = new List<string> { "explorer.exe", "cmd.exe" },
            WhitelistedProcesses = new List<string> { "chrome.exe", "devenv.exe" }
        };

        var result = MonitoringService.IsProcessAllowed(processName, config);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsProcessAllowed_EmptyWhitelist_AllowsAllExceptBlacklist()
    {
        var config = new AppConfig
        {
            BlacklistedProcesses = new List<string> { "malware.exe" },
            WhitelistedProcesses = new List<string>() // empty = allow all
        };

        Assert.True(MonitoringService.IsProcessAllowed("anything.exe", config));
        Assert.False(MonitoringService.IsProcessAllowed("malware.exe", config));
    }

    [Fact]
    public void IsProcessAllowed_WhitelistOnly_RestrictsToWhitelist()
    {
        var config = new AppConfig
        {
            BlacklistedProcesses = new List<string>(),
            WhitelistedProcesses = new List<string> { "teams.exe" }
        };

        Assert.True(MonitoringService.IsProcessAllowed("teams.exe", config));
        Assert.False(MonitoringService.IsProcessAllowed("slack.exe", config));
    }

    [Fact]
    public void IsProcessAllowed_CaseInsensitive()
    {
        var config = new AppConfig
        {
            BlacklistedProcesses = new List<string> { "Explorer.EXE" },
            WhitelistedProcesses = new List<string> { "Chrome.Exe" }
        };

        Assert.False(MonitoringService.IsProcessAllowed("explorer.exe", config));
        Assert.True(MonitoringService.IsProcessAllowed("CHROME.EXE", config));
    }
}
