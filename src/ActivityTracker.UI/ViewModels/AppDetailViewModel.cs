using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Core.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ActivityTracker.UI.ViewModels;

/// <summary>
/// ViewModel for the App Detail page — shows per-app hourly usage distribution.
/// </summary>
public partial class AppDetailViewModel : ObservableObject
{
    private readonly IAppUsageRepository _repository;

    [ObservableProperty]
    private ObservableCollection<string> _processNames = new();

    [ObservableProperty]
    private string? _selectedProcess;

    [ObservableProperty]
    private string _processTodayTotal = "0h 0m";

    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _chartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _chartYAxes = Array.Empty<Axis>();

    public AppDetailViewModel(IAppUsageRepository repository)
    {
        _repository = repository;
    }

    partial void OnSelectedProcessChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = LoadAppDetailAsync(value);
    }

    [RelayCommand]
    public async Task LoadProcessListAsync()
    {
        var names = await _repository.GetAllProcessNamesAsync();
        ProcessNames = new ObservableCollection<string>(names);

        if (names.Count > 0 && SelectedProcess == null)
            SelectedProcess = names[0];
    }

    private async Task LoadAppDetailAsync(string processName)
    {
        var today = DateTime.UtcNow.Date;
        var hourlyData = await _repository.GetHourlyDistributionAsync(processName, today);
        var total = await _repository.GetProcessTodayTotalAsync(processName);
        ProcessTodayTotal = DashboardViewModel.FormatDuration(total);

        BuildChart(hourlyData);
    }

    private void BuildChart(List<int> hourlySeconds)
    {
        var values = hourlySeconds.Select(s => (double)s / 60.0).ToArray();
        var labels = Enumerable.Range(0, 24)
            .Select(h => $"{h:D2}:00")
            .ToArray();

        ChartSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values,
                Fill = new SolidColorPaint(new SKColor(0x00, 0xB7, 0xC3)),
                Stroke = null,
                DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
            }
        };

        ChartXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = -45,
                TextSize = 10,
                Labeler = value => {
                    var idx = (int)value;
                    return idx >= 0 && idx < 24 ? $"{idx:D2}:00" : "";
                }
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
}
