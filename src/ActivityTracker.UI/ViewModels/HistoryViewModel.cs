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
/// ViewModel for the History page — shows daily totals as a line chart
/// with toggle between 7-day and 30-day views.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IAppUsageRepository _repository;

    [ObservableProperty]
    private int _selectedPeriod = 7;

    [ObservableProperty]
    private string _totalRangeHours = "0h 0m";

    [ObservableProperty]
    private string _dailyAverage = "0h 0m";

    [ObservableProperty]
    private ObservableCollection<DailySummary> _dailySummaries = new();

    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _chartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _chartYAxes = Array.Empty<Axis>();

    public HistoryViewModel(IAppUsageRepository repository)
    {
        _repository = repository;
    }

    partial void OnSelectedPeriodChanged(int value)
    {
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-SelectedPeriod);

        var summaries = await _repository.GetDailySummariesAsync(from, to);

        // Fill in missing days with zero
        var filled = new List<DailySummary>();
        for (var d = from; d < to; d = d.AddDays(1))
        {
            var existing = summaries.FirstOrDefault(s => s.Date.Date == d.Date);
            filled.Add(existing ?? new DailySummary { Date = d, TotalSeconds = 0 });
        }

        DailySummaries = new ObservableCollection<DailySummary>(filled);

        // Compute KPIs
        var totalSec = filled.Sum(s => s.TotalSeconds);
        TotalRangeHours = DashboardViewModel.FormatDuration(totalSec);
        DailyAverage = DashboardViewModel.FormatDuration(SelectedPeriod > 0 ? totalSec / SelectedPeriod : 0);

        BuildChart(filled);
    }

    private void BuildChart(List<DailySummary> data)
    {
        if (data.Count == 0)
        {
            ChartSeries = Array.Empty<ISeries>();
            return;
        }

        var values = data.Select(d => (double)d.TotalSeconds / 3600.0).ToArray();
        var labels = data.Select(d => d.Date.ToString("MM/dd")).ToArray();

        ChartSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = values,
                Fill = null,
                GeometrySize = 8,
                Stroke = new SolidColorPaint(new SKColor(0x00, 0x78, 0xD4), 2.5f),
                GeometryFill = new SolidColorPaint(new SKColor(0x00, 0x78, 0xD4)),
                GeometryStroke = new SolidColorPaint(new SKColor(0x00, 0x78, 0xD4)),
                LineSmoothness = 0.4
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
                Name = "Hours",
                TextSize = 12,
                MinLimit = 0
            }
        };
    }
}
