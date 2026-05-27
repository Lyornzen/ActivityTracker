using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Core.Configuration;
using ActivityTracker.Core.Data;
using ActivityTracker.Core.Services;
using Microsoft.Win32;

namespace ActivityTracker.UI.ViewModels;

/// <summary>
/// ViewModel for the Settings page — autostart, retention, pause/resume,
/// and blacklist/whitelist management.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigManager _configManager;
    private readonly IMonitoringService _monitor;
    private readonly IAppUsageRepository _repository;

    [ObservableProperty] private int _scanIntervalSeconds = 5;
    [ObservableProperty] private int _retentionDays = 30;
    [ObservableProperty] private bool _autoStart;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _pauseButtonText = "Pause Monitoring";

    // Blacklist / Whitelist editors
    [ObservableProperty] private string _newBlacklistItem = string.Empty;
    [ObservableProperty] private string _newWhitelistItem = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _blacklist = new();
    [ObservableProperty] private ObservableCollection<string> _whitelist = new();

    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel(
        ConfigManager configManager,
        IMonitoringService monitor,
        IAppUsageRepository repository)
    {
        _configManager = configManager;
        _monitor = monitor;
        _repository = repository;
    }

    [RelayCommand]
    public void Load()
    {
        var config = _configManager.Load();
        ScanIntervalSeconds = config.ScanIntervalSeconds;
        RetentionDays = config.RetentionDays;
        AutoStart = config.AutoStart;
        IsPaused = config.IsPaused;
        PauseButtonText = config.IsPaused ? "Resume Monitoring" : "Pause Monitoring";
        Blacklist = new ObservableCollection<string>(config.BlacklistedProcesses);
        Whitelist = new ObservableCollection<string>(config.WhitelistedProcesses);
    }

    partial void OnScanIntervalSecondsChanged(int value)
    {
        SaveConfig();
    }

    partial void OnRetentionDaysChanged(int value)
    {
        SaveConfig();
    }

    [RelayCommand]
    public void ToggleAutoStart()
    {
        AutoStart = !AutoStart;
        SaveConfig();

        const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        var appPath = Environment.ProcessPath ?? "ActivityTracker.exe";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKey, writable: true);
            if (key == null) return;

            if (AutoStart)
                key.SetValue("ActivityTracker", appPath);
            else
                key.DeleteValue("ActivityTracker", throwOnMissingValue: false);

            StatusMessage = AutoStart
                ? "Added to startup programs"
                : "Removed from startup programs";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update startup: {ex.Message}";
        }
    }

    [RelayCommand]
    public void TogglePause()
    {
        IsPaused = !IsPaused;

        if (IsPaused)
        {
            _monitor.Pause();
            PauseButtonText = "Resume Monitoring";
        }
        else
        {
            _monitor.Resume();
            PauseButtonText = "Pause Monitoring";
        }

        var config = _configManager.Load();
        config.IsPaused = IsPaused;
        _configManager.Save(config);

        StatusMessage = IsPaused ? "Monitoring paused" : "Monitoring resumed";
    }

    // ── Blacklist ───────────────────────────────────────────

    [RelayCommand]
    public void AddBlacklistItem()
    {
        if (string.IsNullOrWhiteSpace(NewBlacklistItem)) return;
        var item = NewBlacklistItem.Trim();

        if (!Blacklist.Contains(item, StringComparer.OrdinalIgnoreCase))
        {
            Blacklist.Add(item);
            SaveListConfig();
        }

        NewBlacklistItem = string.Empty;
    }

    [RelayCommand]
    public void RemoveBlacklistItem(string? item)
    {
        if (item == null) return;
        Blacklist.Remove(item);
        SaveListConfig();
    }

    // ── Whitelist ───────────────────────────────────────────

    [RelayCommand]
    public void AddWhitelistItem()
    {
        if (string.IsNullOrWhiteSpace(NewWhitelistItem)) return;
        var item = NewWhitelistItem.Trim();

        if (!Whitelist.Contains(item, StringComparer.OrdinalIgnoreCase))
        {
            Whitelist.Add(item);
            SaveListConfig();
        }

        NewWhitelistItem = string.Empty;
    }

    [RelayCommand]
    public void RemoveWhitelistItem(string? item)
    {
        if (item == null) return;
        Whitelist.Remove(item);
        SaveListConfig();
    }

    [RelayCommand]
    public async Task PruneNowAsync()
    {
        var config = _configManager.Load();
        var cutoff = DateTime.UtcNow.Date.AddDays(-config.RetentionDays);
        var deleted = await _repository.PruneOlderThanAsync(cutoff);
        StatusMessage = $"Pruned {deleted} old records";
    }

    // ── Private helpers ─────────────────────────────────────

    private void SaveConfig()
    {
        var config = _configManager.Load();
        config.ScanIntervalSeconds = ScanIntervalSeconds;
        config.RetentionDays = RetentionDays;
        config.AutoStart = AutoStart;
        config.IsPaused = IsPaused;
        _configManager.Save(config);
    }

    private void SaveListConfig()
    {
        var config = _configManager.Load();
        config.BlacklistedProcesses = Blacklist.ToList();
        config.WhitelistedProcesses = Whitelist.ToList();
        _configManager.Save(config);
    }
}
