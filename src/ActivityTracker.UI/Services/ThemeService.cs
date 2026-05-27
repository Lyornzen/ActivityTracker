using ModernWpf;

namespace ActivityTracker.UI.Services;

/// <summary>
/// Manages light/dark theme based on the Windows system setting.
/// </summary>
public class ThemeService
{
    public void ApplySystemTheme()
    {
        // ModernWpf automatically follows the system theme when using ThemeResources.
        // This method is a hook for manual overrides if needed in the future.
        var systemTheme = ThemeManager.Current.ActualApplicationTheme;
        ThemeManager.Current.ApplicationTheme = null; // null = follow system
        System.Diagnostics.Debug.WriteLine($"ThemeService: system theme = {systemTheme}");
    }

    public void SetTheme(ApplicationTheme? theme)
    {
        ThemeManager.Current.ApplicationTheme = theme;
    }
}
