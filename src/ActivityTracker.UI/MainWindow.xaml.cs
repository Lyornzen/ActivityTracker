using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ActivityTracker.UI.Views;
using ActivityTracker.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.UI;

public partial class MainWindow
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();

        // Navigate to Dashboard on launch
        NavView.SelectedItem = NavView.MenuItems[0];
        Navigate("Dashboard");
    }

    private void NavView_SelectionChanged(ModernWpf.Controls.NavigationView sender,
        ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is ModernWpf.Controls.NavigationViewItem item && item.Tag is string tag)
        {
            Navigate(tag);
        }
        else if (args.IsSettingsSelected)
        {
            Navigate("Settings");
        }
    }

    private void Navigate(string pageName)
    {
        Page? page = pageName switch
        {
            "Dashboard" => new DashboardPage(_services.GetRequiredService<DashboardViewModel>()),
            "History"   => new HistoryPage(_services.GetRequiredService<HistoryViewModel>()),
            "AppList"   => new AppDetailPage(_services.GetRequiredService<AppDetailViewModel>()),
            "Settings"  => new SettingsPage(_services.GetRequiredService<SettingsViewModel>()),
            _           => null
        };

        if (page != null)
            ContentFrame.Navigate(page);
    }
}
