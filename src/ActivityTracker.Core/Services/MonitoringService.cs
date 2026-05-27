using System.Diagnostics;
using ActivityTracker.Core.Configuration;
using ActivityTracker.Core.Data;
using ActivityTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace ActivityTracker.Core.Services;

/// <summary>
/// Background monitoring service that polls the foreground window at a
/// configurable interval and records usage sessions to SQLite.
///
/// Design:
/// - Runs on a dedicated background thread via a periodic timer.
/// - Tracks the current foreground process and accumulates DurationSeconds.
/// - On process switch, flushes the previous session to the repository.
/// - Respects blacklist/whitelist from AppConfig.
/// - Respects the IsPaused flag.
/// </summary>
public class MonitoringService : IMonitoringService, IDisposable
{
    private readonly IAppUsageRepository _repository;
    private readonly ConfigManager _config;
    private readonly ILogger<MonitoringService> _logger;

    private System.Threading.Timer? _timer;
    private string? _currentProcess;
    private DateTime _sessionStart;
    private bool _isRunning;
    private bool _isPaused;
    private bool _disposed;

    public bool IsRunning => _isRunning;

    public MonitoringService(
        IAppUsageRepository repository,
        ConfigManager config,
        ILogger<MonitoringService> logger)
    {
        _repository = repository;
        _config = config;
        _logger = logger;
    }

    public void Start()
    {
        if (_isRunning) return;

        var config = _config.Load();
        _isPaused = config.IsPaused;
        var intervalMs = Math.Max(config.ScanIntervalSeconds, 1) * 1000;

        _logger.LogInformation("MonitoringService starting with interval {Interval}s, paused={Paused}",
            config.ScanIntervalSeconds, _isPaused);

        _timer = new System.Threading.Timer(
            callback: OnTimerTick,
            state: null,
            dueTime: 0,
            period: intervalMs);

        _isRunning = true;
    }

    public void Pause()
    {
        _isPaused = true;
        FlushCurrentSession();
        _logger.LogInformation("Monitoring paused");
    }

    public void Resume()
    {
        _isPaused = false;
        _logger.LogInformation("Monitoring resumed");
    }

    public void Stop()
    {
        if (!_isRunning) return;
        FlushCurrentSession();
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
        _logger.LogInformation("MonitoringService stopped");
    }

    // ── Timer tick ──────────────────────────────────────────

    private void OnTimerTick(object? state)
    {
        try
        {
            if (_isPaused) return;

            var config = _config.Load();
            var (processName, windowTitle) = WindowDetector.GetForegroundWindowInfo();

            if (string.IsNullOrEmpty(processName))
                return;

            // Apply blacklist/whitelist filters
            if (!IsProcessAllowed(processName, config))
                return;

            var now = DateTime.UtcNow;

            if (_currentProcess == null)
            {
                // First tick or after flush — start new session
                _currentProcess = processName;
                _sessionStart = now;
            }
            else if (!string.Equals(_currentProcess, processName, StringComparison.OrdinalIgnoreCase))
            {
                // Process switched — flush old, start new
                FlushCurrentSession();
                _currentProcess = processName;
                _sessionStart = now;
            }
            // else: same process — accumulate (flushed on next tick or stop)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monitoring tick");
        }
    }

    private void FlushCurrentSession()
    {
        if (_currentProcess == null) return;

        var duration = (int)(DateTime.UtcNow - _sessionStart).TotalSeconds;
        if (duration <= 0)
        {
            _currentProcess = null;
            return;
        }

        var record = new AppUsage
        {
            ProcessName = _currentProcess,
            StartTime = _sessionStart,
            DurationSeconds = duration
        };

        try
        {
            // Fire-and-forget to avoid blocking the timer
            _ = _repository.InsertAsync(record);
            _logger.LogDebug("Flushed {Process}: {Duration}s", _currentProcess, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush session for {Process}", _currentProcess);
        }

        _currentProcess = null;
    }

    // ── Filter helpers ──────────────────────────────────────

    internal static bool IsProcessAllowed(string processName, AppConfig config)
    {
        // Blacklist takes priority
        if (config.BlacklistedProcesses.Any(b =>
                string.Equals(b, processName, StringComparison.OrdinalIgnoreCase)))
            return false;

        // If whitelist is non-empty, process must be in it
        if (config.WhitelistedProcesses.Count > 0)
        {
            return config.WhitelistedProcesses.Any(w =>
                string.Equals(w, processName, StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }

    // ── Dispose ─────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
