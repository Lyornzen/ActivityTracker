using System.Windows.Controls;
using ActivityTracker.UI.ViewModels;

namespace ActivityTracker.UI.Views;

public partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += (_, _) => _viewModel.Load();
    }
}
