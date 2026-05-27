using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ActivityTracker.Core.Services;

/// <summary>
/// Thin wrapper over Windows API for detecting the foreground window's process.
/// </summary>
public static class WindowDetector
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// Returns the process name of the currently active foreground window,
    /// or null if detection fails.
    /// </summary>
    public static string? GetForegroundProcessName()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return null;

        GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId == 0)
            return null;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a (processName, windowTitle) tuple for the foreground window.
    /// </summary>
    public static (string? ProcessName, string? WindowTitle) GetForegroundWindowInfo()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return (null, null);

        GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId == 0)
            return (null, null);

        string? processName = null;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
        }
        catch
        {
            return (null, null);
        }

        var sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, sb.Capacity);
        var title = sb.Length > 0 ? sb.ToString() : null;

        return (processName, title);
    }
}
