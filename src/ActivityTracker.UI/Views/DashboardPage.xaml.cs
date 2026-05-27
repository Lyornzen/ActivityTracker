using System.Windows.Controls;
using ActivityTracker.UI.ViewModels;

namespace ActivityTracker.UI.Views;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += async (_, _) => await _viewModel.LoadDataAsync();
    }
}
