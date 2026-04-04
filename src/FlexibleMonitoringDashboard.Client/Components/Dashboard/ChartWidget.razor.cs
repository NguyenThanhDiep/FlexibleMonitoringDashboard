// src/FlexibleMonitoringDashboard.Client/Components/Dashboard/ChartWidget.razor.cs
using System.Text.Json;
using ApexCharts;
using FlexibleMonitoringDashboard.Client.Components.Threshold;
using FlexibleMonitoringDashboard.Client.Models;
using ChartType = FlexibleMonitoringDashboard.Client.Models.ChartType;
using FlexibleMonitoringDashboard.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FlexibleMonitoringDashboard.Client.Components.Dashboard;

public partial class ChartWidget : IDisposable
{
    [Inject] private DataProxyClient ProxyClient { get; set; } = null!;
    [Inject] private DashboardStateService State { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter] public WidgetConfig WidgetConfig { get; set; } = null!;
    [Parameter] public string SectionId { get; set; } = "";

    private bool _isLoading = true;
    private string? _error;
    private DateTime? _lastUpdated;
    private bool _isExpanded;
    private bool _thresholdBreached;
    private double? _latestValue;

    // Chart data
    private List<ChartDataPoint> _chartData = new();
    private List<ChartDataPoint> _secondaryChartData = new();

    private PeriodicTimer? _refreshTimer;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        await RefreshData();
        StartAutoRefresh();
    }

    private async Task RefreshData()
    {
        _isLoading = true;
        _error = null;
        StateHasChanged();

        try
        {
            var response = await ProxyClient.FetchAsync(
                WidgetConfig.DataSource.Url,
                WidgetConfig.DataSource.Headers);

            if (response == null || !response.Success || response.Data == null)
            {
                _error = response?.Error ?? "Failed to fetch data.";
                return;
            }

            var data = response.Data.Value;
            // Drill into nested path if configured
            if (!string.IsNullOrEmpty(WidgetConfig.DataSource.JsonPath))
            {
                var navigated = NavigateToField(data, WidgetConfig.DataSource.JsonPath);
                if (navigated == null)
                {
                    _error = $"JSON path '{WidgetConfig.DataSource.JsonPath}' not found in response.";
                    return;
                }
                data = navigated.Value;
            }

            _chartData = ExtractChartData(data, WidgetConfig.FieldMappings);
            _lastUpdated = DateTime.Now;

            // Correlation: fetch secondary data source
            if (WidgetConfig.Correlation != null)
            {
                var secondaryResponse = await ProxyClient.FetchAsync(
                    WidgetConfig.Correlation.SecondaryDataSource.Url,
                    WidgetConfig.Correlation.SecondaryDataSource.Headers);

                if (secondaryResponse is { Success: true, Data: not null })
                {
                    var secondaryData = secondaryResponse.Data.Value;
                    if (!string.IsNullOrEmpty(WidgetConfig.Correlation.SecondaryDataSource.JsonPath))
                    {
                        var nav = NavigateToField(secondaryData, WidgetConfig.Correlation.SecondaryDataSource.JsonPath);
                        if (nav != null)
                            secondaryData = nav.Value;
                    }

                    _secondaryChartData = ExtractChartData(
                        secondaryData,
                        WidgetConfig.Correlation.SecondaryFieldMappings);
                }
            }

            CheckThreshold();
        }
        catch (Exception ex)
        {
            _error = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private static List<ChartDataPoint> ExtractChartData(JsonElement data, List<FieldMapping> mappings)
    {
        var points = new List<ChartDataPoint>();

        var xMapping = mappings.FirstOrDefault(m => m.Role == FieldRole.XAxis);
        var yMappings = mappings.Where(m => m.Role == FieldRole.YAxis || m.Role == FieldRole.Value).ToList();

        var xValues = new List<string>();
        if (xMapping != null && !string.IsNullOrEmpty(xMapping.FieldPath))
        {
            xValues = ExtractStringValues(data, xMapping.FieldPath);
        }

        foreach (var yMapping in yMappings)
        {
            var yValues = ExtractDoubleValues(data, yMapping.FieldPath);
            var seriesName = !string.IsNullOrEmpty(yMapping.Label)
                ? yMapping.Label
                : yMapping.FieldPath.Split('.').LastOrDefault() ?? yMapping.FieldPath;

            for (int i = 0; i < yValues.Count; i++)
            {
                points.Add(new ChartDataPoint
                {
                    X = i < xValues.Count ? xValues[i] : i.ToString(),
                    Y = yValues[i],
                    Series = seriesName
                });
            }

            // Single value (non-array field)
            if (yValues.Count == 0)
            {
                var element = NavigateToField(data, yMapping.FieldPath);
                if (element?.ValueKind == JsonValueKind.Number && element.Value.TryGetDouble(out var val))
                {
                    points.Add(new ChartDataPoint { X = seriesName, Y = val, Series = seriesName });
                }
            }
        }

        return points;
    }

    private static JsonElement? NavigateToField(JsonElement root, string dotPath)
    {
        var current = root;
        foreach (var segment in dotPath.Split('.'))
        {
            if (string.IsNullOrEmpty(segment)) continue; // skip empty segments from leading/trailing dots
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var child))
            {
                current = child;
            }
            else
            {
                return null;
            }
        }
        return current;
    }

    private static List<string> ExtractStringValues(JsonElement root, string dotPath)
    {
        var results = new List<string>();

        // Array of objects: extract the field from each element
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var field = NavigateToField(item, dotPath);
                if (field != null)
                    results.Add(field.Value.ToString());
            }
            return results;
        }

        var element = NavigateToField(root, dotPath);
        if (element == null) return results;

        if (element.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.Value.EnumerateArray())
                results.Add(item.ToString());
        }
        else
        {
            results.Add(element.Value.ToString());
        }

        return results;
    }

    private static List<double> ExtractDoubleValues(JsonElement root, string dotPath)
    {
        var results = new List<double>();

        // Array of objects: extract the field from each element
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var field = NavigateToField(item, dotPath);
                if (field == null) continue;
                if (field.Value.ValueKind == JsonValueKind.Number && field.Value.TryGetDouble(out var d))
                    results.Add(d);
                else if (field.Value.ValueKind == JsonValueKind.String && double.TryParse(field.Value.GetString(), out var parsed))
                    results.Add(parsed);
            }
            return results;
        }

        var element = NavigateToField(root, dotPath);
        if (element == null) return results;

        if (element.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var d))
                    results.Add(d);
                else if (item.ValueKind == JsonValueKind.String && double.TryParse(item.GetString(), out var parsed))
                    results.Add(parsed);
            }
        }
        else
        {
            if (element.Value.ValueKind == JsonValueKind.Number && element.Value.TryGetDouble(out var d))
                results.Add(d);
            else if (element.Value.ValueKind == JsonValueKind.String && double.TryParse(element.Value.GetString(), out var parsed))
                results.Add(parsed);
        }

        return results;
    }

    private void CheckThreshold()
    {
        if (WidgetConfig.Threshold?.Enabled != true || _chartData.Count == 0) return;

        _latestValue = _chartData.LastOrDefault()?.Y;
        if (_latestValue == null) return;

        _thresholdBreached = WidgetConfig.Threshold.Direction switch
        {
            ThresholdDirection.Above => _latestValue > WidgetConfig.Threshold.Value,
            ThresholdDirection.Below => _latestValue < WidgetConfig.Threshold.Value,
            _ => false
        };

        if (_thresholdBreached)
        {
            Snackbar.Add($"{WidgetConfig.Title}: Threshold breached! Value: {_latestValue:F2}",
                Severity.Warning);
        }
    }

    private void StartAutoRefresh()
    {
        var interval = WidgetConfig.DataSource.RefreshIntervalSeconds;
        if (interval <= 0) return;

        _cts = new CancellationTokenSource();
        _refreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(interval));

        _ = Task.Run(async () =>
        {
            try
            {
                while (await _refreshTimer.WaitForNextTickAsync(_cts.Token))
                {
                    await InvokeAsync(async () =>
                    {
                        await RefreshData();
                    });
                }
            }
            catch (OperationCanceledException) { }
        });
    }

    private Task DeleteWidget()
    {
        State.RemoveWidget(SectionId, WidgetConfig.Id);
        return Task.CompletedTask;
    }

    private async Task EditWidget()
    {
        var parameters = new DialogParameters
        {
            { "Widget", WidgetConfig },
            { "SectionId", SectionId }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<EditWidgetDialog>("Edit Widget", parameters, options);
        await dialog.Result;
    }

    private async Task ConfigureThreshold()
    {
        var parameters = new DialogParameters
        {
            { "Widget", WidgetConfig },
            { "SectionId", SectionId }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ThresholdDialog>("Threshold", parameters, options);
        await dialog.Result;
    }

    private Task ToggleSize()
    {
        _isExpanded = !_isExpanded;
        var newSpan = _isExpanded ? 12 : 6;
        WidgetConfig.ColumnSpan = newSpan;
        State.UpdateWidget(SectionId, WidgetConfig);
        return Task.CompletedTask;
    }

    private SeriesType GetApexSeriesType() => WidgetConfig.ChartType switch
    {
        ChartType.Line => SeriesType.Line,
        ChartType.Area => SeriesType.Area,
        ChartType.Bar => SeriesType.Bar,
        ChartType.Scatter => SeriesType.Scatter,
        _ => SeriesType.Line
    };

    private static string FormatNumber(double value) =>
        value switch
        {
            >= 1_000_000 => $"{value / 1_000_000:F2}M",
            >= 1_000 => $"{value / 1_000:F1}K",
            _ => value.ToString("F2")
        };

    private static string FormatTimeAgo(DateTime time)
    {
        var diff = DateTime.Now - time;
        return diff.TotalSeconds switch
        {
            < 5 => "just now",
            < 60 => $"{(int)diff.TotalSeconds}s ago",
            < 3600 => $"{(int)diff.TotalMinutes}m ago",
            _ => time.ToString("HH:mm:ss")
        };
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _refreshTimer?.Dispose();
    }
}
