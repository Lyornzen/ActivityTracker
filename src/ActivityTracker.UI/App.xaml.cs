using System.Windows;
using ActivityTracker.Core.Configuration;
using ActivityTracker.Core.Data;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Services;
using ActivityTracker.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ActivityTracker.UI;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddDebug();
            })
            .ConfigureServices((context, services) =>
            {
                // ── Application data path ─────────────────
                var appData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ActivityTracker");
                Directory.CreateDirectory(appData);
                var dbPath = Path.Combine(appData, "activity.db");

                // ── Core services ──────────────────────────
                services.AddSingleton<ConfigManager>(sp =>
                    new ConfigManager(appData, sp.GetRequiredService<ILogger<ConfigManager>>()));

                services.AddSingleton(new DatabaseInitializer(dbPath));
                services.AddSingleton<IAppUsageRepository, AppUsageRepository>();
                services.AddSingleton<IMonitoringService, MonitoringService>();

                // ── UI services ────────────────────────────
                services.AddSingleton<ThemeService>();

                // ── ViewModels (transient — new instance per navigation) ──
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<HistoryViewModel>();
                services.AddTransient<AppDetailViewModel>();
                services.AddTransient<SettingsViewModel>();

                // ── MainWindow ─────────────────────────────
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services => _host.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Initialize database schema
        var dbInit = _host.Services.GetRequiredService<DatabaseInitializer>();
        await dbInit.InitializeAsync();

        // Apply system theme
        var themeService = _host.Services.GetRequiredService<ThemeService>();
        themeService.ApplySystemTheme();

        // Prune old records
        var configManager = _host.Services.GetRequiredService<ConfigManager>();
        var config = configManager.Load();
        var repo = _host.Services.GetRequiredService<IAppUsageRepository>();
        var cutoff = DateTime.UtcNow.Date.AddDays(-config.RetentionDays);
        var deleted = await repo.PruneOlderThanAsync(cutoff);
        if (deleted > 0)
            System.Diagnostics.Debug.WriteLine($"Pruned {deleted} old records");

        // Start monitoring
        var monitor = _host.Services.GetRequiredService<IMonitoringService>();
        monitor.Start();

        // Show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var monitor = _host.Services.GetRequiredService<IMonitoringService>();
        monitor.Stop();

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
