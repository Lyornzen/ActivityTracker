using System.Text.Json;
using ActivityTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace ActivityTracker.Core.Configuration;

/// <summary>
/// Reads and writes AppConfig as JSON in the application data directory.
/// Thread-safe for reads; writes should be serialized by the caller.
/// </summary>
public class ConfigManager
{
    private readonly string _configPath;
    private readonly ILogger<ConfigManager> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfigManager(string appDataFolder, ILogger<ConfigManager> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(appDataFolder, "config.json");
    }

    /// <summary>
    /// Loads configuration from disk. Returns defaults if the file doesn't exist.
    /// </summary>
    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("Config file not found at {Path}; using defaults", _configPath);
                return new AppConfig();
            }

            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load config; using defaults");
            return new AppConfig();
        }
    }

    /// <summary>
    /// Persists configuration to disk. Creates the directory if needed.
    /// </summary>
    public void Save(AppConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(_configPath, json);
            _logger.LogInformation("Config saved to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save config");
        }
    }
}
