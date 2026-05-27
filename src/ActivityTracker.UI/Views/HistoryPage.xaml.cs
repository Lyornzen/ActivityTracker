using System.Windows.Controls;
using ActivityTracker.UI.ViewModels;

namespace ActivityTracker.UI.Views;

public partial class HistoryPage : Page
{
    private readonly HistoryViewModel _viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += async (_, _) => await _viewModel.LoadDataAsync();

        // Wire period selector
        Radio7Days.Checked += (_, _) => _viewModel.SelectedPeriod = 7;
        Radio30Days.Checked += (_, _) => _viewModel.SelectedPeriod = 30;
    }
}
