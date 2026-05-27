using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Core.Data;
using ActivityTracker.Core.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ActivityTracker.UI.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page — shows today's KPIs and Top 5 apps chart.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IAppUsageRepository _repository;

    [ObservableProperty]
    private string _totalToday = "0h 0m";

    [ObservableProperty]
    private int _appCountToday;

    [ObservableProperty]
    private ObservableCollection<AppUsageSummary> _topApps = new();

    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _chartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _chartYAxes = Array.Empty<Axis>();

    public DashboardViewModel(IAppUsageRepository repository)
    {
        _repository = repository;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var total = await _repository.GetTodayTotalSecondsAsync();
        TotalToday = FormatDuration(total);

        var topApps = await _repository.GetTodayTopAppsAsync(5);
        AppCountToday = topApps.Count;
        TopApps = new ObservableCollection<AppUsageSummary>(topApps);

        BuildChart(topApps);
    }

    private void BuildChart(List<AppUsageSummary> topApps)
    {
        if (topApps.Count == 0)
        {
            ChartSeries = Array.Empty<ISeries>();
            return;
        }

        var values = topApps.Select(a => (double)a.TotalSeconds / 60.0).ToArray();
        var labels = topApps.Select(a => a.ProcessName).ToArray();

        ChartSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values,
                Fill = new SolidColorPaint(new SKColor(0x00, 0x78, 0xD4)),
                Stroke = null,
                DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
            }
        };

        ChartXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = -30,
                TextSize = 12
            }
        };

        ChartYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Minutes",
                TextSize = 12,
                MinLimit = 0
            }
        };
    }

    public static string FormatDuration(int totalSeconds)
    {
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        return $"{hours}h {minutes}m";
    }
}
