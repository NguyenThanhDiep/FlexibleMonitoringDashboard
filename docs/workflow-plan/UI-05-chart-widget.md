# Task UI-05: Chart Widget

## Metadata
- **ID**: UI-05-chart-widget
- **Layer**: 4
- **Dependencies**: `FE-05`, `FE-01`
- **Blocks**: `UI-08`, `UI-11`
- **Parallel With**: UI-02, UI-06

---

## Files Created

| File | Path |
|------|------|
| `ChartWidget.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Dashboard/ChartWidget.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `ChartWidget.razor`

This is the most complex UI component. It:
- Fetches data from the backend proxy
- Maps JSON fields to chart data using configured field paths
- Renders an ApexChart of the configured type
- Supports auto-refresh on a timer
- Has toolbar for edit, delete, refresh, resize actions
- Supports correlation mode (two data sources, dual Y-axis)
- Supports threshold annotations (nice-to-have)

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Dashboard/ChartWidget.razor *@
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@using System.Text.Json
@using ApexCharts
@inject DataProxyClient ProxyClient
@inject DashboardStateService State
@inject ISnackbar Snackbar
@implements IDisposable

<MudPaper Elevation="2" Class="pa-3" Style="border-radius: 8px; position: relative;">
    @* Widget header *@
    <MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-2">
        <MudStack Spacing="0" Style="flex: 1; min-width: 0;">
            <MudText Typo="Typo.subtitle1" Style="font-weight: 500;">
                @WidgetConfig.Title
            </MudText>
            @if (!string.IsNullOrWhiteSpace(WidgetConfig.Description))
            {
                <MudText Typo="Typo.caption" Style="opacity: 0.6;">
                    @WidgetConfig.Description
                </MudText>
            }
        </MudStack>

        @* Widget toolbar *@
        <MudMenu Icon="@Icons.Material.Filled.MoreVert" Size="Size.Small" Dense="true">
            <MudMenuItem Icon="@Icons.Material.Filled.Refresh" OnClick="RefreshData">
                Refresh Now
            </MudMenuItem>
            <MudMenuItem Icon="@Icons.Material.Filled.Edit" OnClick="EditWidget">
                Edit
            </MudMenuItem>
            <MudMenuItem Icon="@Icons.Material.Filled.Fullscreen" OnClick="ToggleSize">
                @(_isExpanded ? "Shrink" : "Expand")
            </MudMenuItem>
            <MudMenuItem Icon="@Icons.Material.Filled.Delete" OnClick="DeleteWidget"
                         Style="color: var(--mud-palette-error);">
                Delete
            </MudMenuItem>
        </MudMenu>
    </MudStack>

    @* Chart area *@
    @if (_isLoading)
    {
        <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center"
                  Style="min-height: 200px;">
            <MudProgressCircular Indeterminate="true" Size="Size.Medium" />
            <MudText Typo="Typo.caption" Style="opacity: 0.5;">Loading data...</MudText>
        </MudStack>
    }
    else if (_error != null)
    {
        <MudAlert Severity="Severity.Error" Dense="true" Class="mb-2">
            @_error
        </MudAlert>
        <MudButton Variant="Variant.Outlined"
                   Color="Color.Primary"
                   Size="Size.Small"
                   OnClick="RefreshData">
            Retry
        </MudButton>
    }
    else
    {
        @RenderChart()
    }

    @* Status bar *@
    <MudStack Row="true" AlignItems="AlignItems.Center" Class="mt-1">
        <MudText Typo="Typo.caption" Style="opacity: 0.4;">
            @if (_lastUpdated.HasValue)
            {
                @($"Updated {FormatTimeAgo(_lastUpdated.Value)}")
            }
        </MudText>
        <MudSpacer />
        @if (WidgetConfig.DataSource.RefreshIntervalSeconds > 0)
        {
            <MudChip T="string" Size="Size.Small" Variant="Variant.Text" Style="opacity: 0.4;">
                @($"Every {WidgetConfig.DataSource.RefreshIntervalSeconds}s")
            </MudChip>
        }
    </MudStack>

    @* Threshold alarm indicator *@
    @if (_thresholdBreached)
    {
        <MudAlert Severity="Severity.Warning" Dense="true" Class="mt-2" Variant="Variant.Filled">
            ⚠ Threshold breached: value @_latestValue exceeds @WidgetConfig.Threshold?.Value
        </MudAlert>
    }
</MudPaper>

@code {
    [Parameter] public WidgetConfig WidgetConfig { get; set; } = null!;
    [Parameter] public string SectionId { get; set; } = "";

    private bool _isLoading = true;
    private string? _error;
    private DateTime? _lastUpdated;
    private bool _isExpanded = false;
    private bool _thresholdBreached = false;
    private double? _latestValue;

    // Chart data
    private List<ChartDataPoint> _chartData = new();
    private List<ChartDataPoint> _secondaryChartData = new(); // For correlation
    private JsonElement? _rawData;

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

            if (!response.Success || response.Data == null)
            {
                _error = response.Error ?? "Failed to fetch data.";
                _isLoading = false;
                StateHasChanged();
                return;
            }

            _rawData = response.Data;
            _chartData = ExtractChartData(response.Data.Value, WidgetConfig.DataSource);
            _lastUpdated = DateTime.Now;

            // Correlation: fetch secondary data source
            if (WidgetConfig.IsCorrelation && WidgetConfig.Correlation != null)
            {
                var secondaryResponse = await ProxyClient.FetchAsync(
                    WidgetConfig.Correlation.SecondarySource.Url,
                    WidgetConfig.Correlation.SecondarySource.Headers);

                if (secondaryResponse.Success && secondaryResponse.Data != null)
                {
                    _secondaryChartData = ExtractChartData(
                        secondaryResponse.Data.Value,
                        WidgetConfig.Correlation.SecondarySource);
                }
            }

            // Threshold check
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

    private List<ChartDataPoint> ExtractChartData(JsonElement data, DataSourceConfig source)
    {
        var points = new List<ChartDataPoint>();

        // Extract X values (labels/categories/timestamps)
        var xValues = new List<string>();
        if (!string.IsNullOrEmpty(source.XFieldPath))
        {
            xValues = DataProxyClient.ExtractValues<string>(data, source.XFieldPath);
        }

        // Extract Y values for each Y field
        foreach (var yPath in source.YFieldPaths)
        {
            var yValues = DataProxyClient.ExtractValues<double>(data, yPath);
            var fieldName = yPath.Split('.').LastOrDefault() ?? yPath;

            for (int i = 0; i < yValues.Count; i++)
            {
                points.Add(new ChartDataPoint
                {
                    X = i < xValues.Count ? xValues[i] : i.ToString(),
                    Y = yValues[i],
                    Series = fieldName
                });
            }

            // Single value (non-array)
            if (yValues.Count == 0)
            {
                var element = DataProxyClient.NavigateToField(data, yPath);
                if (element?.ValueKind == JsonValueKind.Number && element.Value.TryGetDouble(out var val))
                {
                    points.Add(new ChartDataPoint { X = fieldName, Y = val, Series = fieldName });
                }
            }
        }

        return points;
    }

    private RenderFragment RenderChart() => WidgetConfig.ChartType switch
    {
        ChartType.Line or ChartType.Area or ChartType.Bar or ChartType.Scatter => RenderXYChart(),
        ChartType.Pie or ChartType.Donut => RenderPieChart(),
        ChartType.RadialBar => RenderGaugeChart(),
        _ => RenderXYChart()
    };

    private RenderFragment RenderXYChart() => __builder =>
    {
        // Group data by series name
        var seriesGroups = _chartData.GroupBy(p => p.Series).ToList();

        <ApexChart TItem="ChartDataPoint"
                   Title=""
                   Height="250">
            @foreach (var series in seriesGroups)
            {
                var seriesType = WidgetConfig.ChartType switch
                {
                    ChartType.Line => SeriesType.Line,
                    ChartType.Area => SeriesType.Area,
                    ChartType.Bar => SeriesType.Bar,
                    ChartType.Scatter => SeriesType.Scatter,
                    _ => SeriesType.Line
                };

                <ApexPointSeries TItem="ChartDataPoint"
                                 Items="series.ToList()"
                                 Name="@series.Key"
                                 SeriesType="seriesType"
                                 XValue="e => e.X"
                                 YValue="e => (decimal)e.Y" />
            }

            @* Secondary series for correlation *@
            @if (WidgetConfig.IsCorrelation && _secondaryChartData.Count > 0)
            {
                var secondaryGroups = _secondaryChartData.GroupBy(p => p.Series);
                @foreach (var series in secondaryGroups)
                {
                    <ApexPointSeries TItem="ChartDataPoint"
                                     Items="series.ToList()"
                                     Name="@series.Key"
                                     SeriesType="SeriesType.Line"
                                     XValue="e => e.X"
                                     YValue="e => (decimal)e.Y" />
                }
            }
        </ApexChart>
    };

    private RenderFragment RenderPieChart() => __builder =>
    {
        <ApexChart TItem="ChartDataPoint"
                   Title=""
                   Height="250">
            <ApexPointSeries TItem="ChartDataPoint"
                             Items="_chartData"
                             Name="Values"
                             SeriesType="@(WidgetConfig.ChartType == ChartType.Donut ? SeriesType.Donut : SeriesType.Pie)"
                             XValue="e => e.X"
                             YAggregate="e => (decimal)e.Sum(p => p.Y)"
                             OrderByDescending="e => e.Y" />
        </ApexChart>
    };

    private RenderFragment RenderGaugeChart() => __builder =>
    {
        var value = _chartData.FirstOrDefault()?.Y ?? 0;
        <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Style="min-height: 200px;">
            <MudText Typo="Typo.h3" Color="Color.Primary" Style="font-weight: 700;">
                @FormatNumber(value)
            </MudText>
            @if (_chartData.Count > 0)
            {
                <MudText Typo="Typo.body2" Style="opacity: 0.6;">
                    @_chartData.First().Series
                </MudText>
            }
        </MudStack>
    };

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
            Snackbar.Add($"⚠ {WidgetConfig.Title}: Threshold breached! Value: {_latestValue:F2}",
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

    private void DeleteWidget()
    {
        State.RemoveWidget(SectionId, WidgetConfig.Id);
    }

    private void EditWidget()
    {
        // TODO: Open edit dialog (re-use AddDataSourceDialog in edit mode)
    }

    private void ToggleSize()
    {
        _isExpanded = !_isExpanded;
        var newSpan = _isExpanded ? 12 : 6;
        WidgetConfig.ColumnSpan = newSpan;
        State.UpdateWidget(SectionId, WidgetConfig);
    }

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

/// <summary>
/// Generic data point for chart rendering.
/// </summary>
public class ChartDataPoint
{
    public string X { get; set; } = "";
    public double Y { get; set; }
    public string Series { get; set; } = "";
}
```

---

## Key Design Details

| Feature | Implementation |
|---------|---------------|
| Auto-refresh | `PeriodicTimer` with configurable interval, cancelled on dispose |
| Error state | MudAlert + Retry button |
| Loading state | MudProgressCircular spinner |
| Gauge (single value) | Large styled number display (not ApexChart radial) |
| Correlation | Two data fetches, merged into same chart |
| Threshold | Comparison after data fetch, MudAlert + MudSnackbar |
| Resize | Toggle between column span 6 ↔ 12 |
| Time ago | "Updated 5s ago", "Updated 2m ago" format |

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Widget renders chart from fetched data
- [ ] Loading spinner shows during data fetch
- [ ] Error state shows with retry button on failed fetch
- [ ] Auto-refresh timer works at configured interval
- [ ] Deleting widget removes it from state
- [ ] Resize toggles between half-width and full-width
- [ ] Gauge shows formatted number for single-value APIs
- [ ] Correlation renders secondary series on same chart
- [ ] Threshold breach shows warning alert and snackbar
- [ ] Timer is properly disposed when component unmounts
