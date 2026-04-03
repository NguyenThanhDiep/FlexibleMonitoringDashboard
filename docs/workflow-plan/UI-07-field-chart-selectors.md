# Task UI-07: Field & Chart Type Selectors

## Metadata
- **ID**: UI-07-field-chart-selectors
- **Layer**: 3
- **Dependencies**: `FE-01`
- **Blocks**: None
- **Parallel With**: UI-01, UI-09, UI-10

---

## Files Created

| File | Path |
|------|------|
| `FieldSelector.razor` | `src/FlexibleMonitoringDashboard.Client/Components/DataSource/FieldSelector.razor` |
| `ChartTypeSelector.razor` | `src/FlexibleMonitoringDashboard.Client/Components/DataSource/ChartTypeSelector.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `FieldSelector.razor`

Reusable dropdown for selecting a JSON field path from a list of detected fields. Filters by compatible types.

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/DataSource/FieldSelector.razor *@
@using FlexibleMonitoringDashboard.Client.Models

<MudSelect T="string"
           Value="SelectedField"
           ValueChanged="OnFieldChanged"
           Label="@Label"
           Variant="Variant.Outlined"
           Dense="true"
           AnchorOrigin="Origin.BottomCenter">
    <MudSelectItem T="string" Value="@("")">— None —</MudSelectItem>
    @foreach (var field in FilteredFields)
    {
        <MudSelectItem T="string" Value="@field.Path">
            <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1">
                <MudChip T="string" Size="Size.Small" Variant="Variant.Text"
                         Color="@GetTypeColor(field.FieldType)">
                    @field.FieldType
                </MudChip>
                <MudText Typo="Typo.body2" Style="font-family: monospace;">
                    @field.Path
                </MudText>
                @if (field.IsArray)
                {
                    <MudText Typo="Typo.caption" Style="opacity: 0.5;">
                        [@field.ArrayLength]
                    </MudText>
                }
            </MudStack>
        </MudSelectItem>
    }
</MudSelect>

@code {
    [Parameter] public string Label { get; set; } = "Select Field";
    [Parameter] public string? SelectedField { get; set; }
    [Parameter] public EventCallback<string?> SelectedFieldChanged { get; set; }
    [Parameter] public List<DetectedField> Fields { get; set; } = new();
    [Parameter] public List<string>? AllowedTypes { get; set; }
    [Parameter] public bool ArraysOnly { get; set; } = false;

    private IEnumerable<DetectedField> FilteredFields
    {
        get
        {
            var filtered = Fields.AsEnumerable();
            if (AllowedTypes != null && AllowedTypes.Count > 0)
                filtered = filtered.Where(f => AllowedTypes.Contains(f.FieldType));
            if (ArraysOnly)
                filtered = filtered.Where(f => f.IsArray);
            return filtered;
        }
    }

    private async Task OnFieldChanged(string? value)
    {
        SelectedField = value;
        await SelectedFieldChanged.InvokeAsync(value);
    }

    private static Color GetTypeColor(string fieldType) => fieldType switch
    {
        "Number" => Color.Primary,
        "String" => Color.Secondary,
        "DateTime" => Color.Tertiary,
        "Boolean" => Color.Info,
        _ => Color.Default
    };
}
```

### Step 2: Create `ChartTypeSelector.razor`

Reusable component for selecting a chart type, with visual previews.

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/DataSource/ChartTypeSelector.razor *@
@using FlexibleMonitoringDashboard.Client.Models

<MudText Typo="Typo.subtitle2" Class="mb-2">@Label</MudText>

<MudToggleGroup T="ChartType"
                @bind-Value="SelectedType"
                Color="Color.Primary"
                CheckMark="true"
                FixedContent="true">
    @foreach (var option in ChartOptions)
    {
        <MudToggleItem T="ChartType" Value="option.Type">
            <MudStack AlignItems="AlignItems.Center" Spacing="0">
                <MudIcon Icon="@option.Icon" Size="Size.Small" />
                <MudText Typo="Typo.caption">@option.DisplayName</MudText>
            </MudStack>
        </MudToggleItem>
    }
</MudToggleGroup>

@if (!string.IsNullOrEmpty(Description))
{
    <MudText Typo="Typo.caption" Style="opacity: 0.6;" Class="mt-1">
        @Description
    </MudText>
}

@code {
    [Parameter] public string Label { get; set; } = "Chart Type";

    [Parameter] public ChartType SelectedType { get; set; } = ChartType.Line;
    [Parameter] public EventCallback<ChartType> SelectedTypeChanged { get; set; }

    [Parameter] public string? RecommendedReason { get; set; }

    private string? Description => RecommendedReason ??
        ChartOptions.FirstOrDefault(o => o.Type == SelectedType)?.Description;

    private static readonly List<ChartOption> ChartOptions = new()
    {
        new(ChartType.Line, "Line", "Best for time series and trends", Icons.Material.Filled.ShowChart),
        new(ChartType.Bar, "Bar", "Best for comparing categories", Icons.Material.Filled.BarChart),
        new(ChartType.Area, "Area", "Line with filled area beneath", Icons.Material.Filled.AreaChart),
        new(ChartType.Pie, "Pie", "Proportional distribution", Icons.Material.Filled.PieChart),
        new(ChartType.Donut, "Donut", "Proportions with center space", Icons.Material.Filled.DonutLarge),
        new(ChartType.RadialBar, "Gauge", "Single KPI/value display", Icons.Material.Filled.Speed),
        new(ChartType.Scatter, "Scatter", "Correlation between two variables", Icons.Material.Filled.ScatterPlot),
    };

    private record ChartOption(ChartType Type, string DisplayName, string Description, string Icon);
}
```

---

## Usage Examples

### FieldSelector:
```razor
<FieldSelector Label="X-Axis Field"
               Fields="_detectedFields"
               AllowedTypes='new() { "DateTime", "String" }'
               ArraysOnly="true"
               @bind-SelectedField="_xFieldPath" />
```

### ChartTypeSelector:
```razor
<ChartTypeSelector @bind-SelectedType="_chartType"
                   RecommendedReason="@_recommendation?.Reason" />
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] `FieldSelector` shows all fields, filtered by `AllowedTypes` and `ArraysOnly`
- [ ] Selecting a field fires `SelectedFieldChanged` callback
- [ ] `ChartTypeSelector` renders all 7 chart types with icons
- [ ] Selected chart type is visually highlighted
- [ ] Description text shows for selected chart type
- [ ] Both components are reusable and parameterized
