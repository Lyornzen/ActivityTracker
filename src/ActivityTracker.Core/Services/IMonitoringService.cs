namespace ActivityTracker.Core.Services;

/// <summary>
/// Interface for the background monitoring service.
/// </summary>
public interface IMonitoringService
{
    /// <summary>Whether the service is currently monitoring.</summary>
    bool IsRunning { get; }

    /// <summary>Start the monitoring loop.</summary>
    void Start();

    /// <summary>Pause monitoring without disposing.</summary>
    void Pause();

    /// <summary>Resume after pausing.</summary>
    void Resume();

    /// <summary>Stop monitoring and release the timer.</summary>
    void Stop();
}
