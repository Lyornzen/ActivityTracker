using System.Windows.Controls;
using ActivityTracker.UI.ViewModels;

namespace ActivityTracker.UI.Views;

public partial class AppDetailPage : Page
{
    private readonly AppDetailViewModel _viewModel;

    public AppDetailPage(AppDetailViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += async (_, _) => await _viewModel.LoadProcessListAsync();
    }
}
