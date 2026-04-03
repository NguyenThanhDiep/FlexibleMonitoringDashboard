# Task UI-06: Add Data Source Dialog

## Metadata
- **ID**: UI-06-add-datasource-dialog
- **Layer**: 4
- **Dependencies**: `FE-04`, `FE-05`
- **Blocks**: None
- **Parallel With**: UI-02, UI-05

---

## Files Created

| File | Path |
|------|------|
| `AddDataSourceDialog.razor` | `src/FlexibleMonitoringDashboard.Client/Components/DataSource/AddDataSourceDialog.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `AddDataSourceDialog.razor`

Multi-step dialog that guides users through:
1. **Paste URL** → hit "Analyze"
2. **Review detected fields** and chart type suggestion
3. **Configure** chart title, description, X/Y fields, chart type, column width
4. **Add** widget to section

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/DataSource/AddDataSourceDialog.razor *@
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@inject DataProxyClient ProxyClient
@inject ChartTypeRecommender Recommender
@inject DashboardStateService State

<MudDialog>
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center">
            <MudIcon Icon="@Icons.Material.Filled.AddChart" Class="mr-2" />
            <MudText Typo="Typo.h6">Add Chart</MudText>
        </MudStack>
    </TitleContent>
    <DialogContent>
        <MudStepper @ref="_stepper" Linear="true" Color="Color.Primary">
            @* Step 1: Enter URL *@
            <MudStep Title="Data Source">
                <MudStack Spacing="3">
                    <MudText Typo="Typo.body2" Style="opacity: 0.7;">
                        Paste any public JSON API URL. We'll auto-detect the data fields.
                    </MudText>
                    <MudTextField @bind-Value="_url"
                                  Label="API URL"
                                  Placeholder="https://api.example.com/data.json"
                                  Variant="Variant.Outlined"
                                  Adornment="Adornment.Start"
                                  AdornmentIcon="@Icons.Material.Filled.Link"
                                  HelperText="Example: https://api.open-meteo.com/v1/forecast?latitude=10.823&longitude=106.6296&hourly=temperature_2m"
                                  Error="@(!string.IsNullOrEmpty(_urlError))"
                                  ErrorText="@_urlError" />

                    <MudButton Variant="Variant.Filled"
                               Color="Color.Primary"
                               OnClick="AnalyzeUrl"
                               Disabled="_isAnalyzing"
                               FullWidth="true"
                               Size="Size.Large">
                        @if (_isAnalyzing)
                        {
                            <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                            <span>Analyzing...</span>
                        }
                        else
                        {
                            <MudIcon Icon="@Icons.Material.Filled.Search" Class="mr-2" />
                            <span>Analyze Data</span>
                        }
                    </MudButton>

                    @if (_analyzeError != null)
                    {
                        <MudAlert Severity="Severity.Error" Dense="true">@_analyzeError</MudAlert>
                    }
                </MudStack>
            </MudStep>

            @* Step 2: Configure fields and chart type *@
            <MudStep Title="Configure Chart">
                <MudStack Spacing="3">
                    @* Chart title and description *@
                    <MudTextField @bind-Value="_title"
                                  Label="Chart Title"
                                  Variant="Variant.Outlined"
                                  Required="true" />
                    <MudTextField @bind-Value="_description"
                                  Label="Description (optional)"
                                  Variant="Variant.Outlined" />

                    @* Chart type selector *@
                    <MudText Typo="Typo.subtitle2" Class="mt-2">Chart Type</MudText>
                    @if (_recommendations.Count > 0)
                    {
                        <MudChipSet T="ChartType" @bind-SelectedValue="_selectedChartType" Mandatory="true">
                            @foreach (var rec in _recommendations)
                            {
                                <MudChip T="ChartType"
                                         Value="rec.ChartType"
                                         Color="@(rec == _recommendations[0] ? Color.Primary : Color.Default)"
                                         Variant="Variant.Outlined">
                                    @rec.ChartType.ToString()
                                    @if (rec == _recommendations[0])
                                    {
                                        <MudText Typo="Typo.caption"> (Recommended)</MudText>
                                    }
                                </MudChip>
                            }
                        </MudChipSet>
                        <MudText Typo="Typo.caption" Style="opacity: 0.6;">
                            @_recommendations.FirstOrDefault(r => r.ChartType == _selectedChartType)?.Reason
                        </MudText>
                    }

                    @* Detected fields table *@
                    <MudText Typo="Typo.subtitle2" Class="mt-2">Detected Fields</MudText>
                    <MudTable Items="_detectedFields" Dense="true" Hover="true" Elevation="0">
                        <HeaderContent>
                            <MudTh>Use</MudTh>
                            <MudTh>Field</MudTh>
                            <MudTh>Type</MudTh>
                            <MudTh>Array</MudTh>
                            <MudTh>Role</MudTh>
                            <MudTh>Sample</MudTh>
                        </HeaderContent>
                        <RowTemplate>
                            <MudTd>
                                <MudCheckBox T="bool" @bind-Value="context.IsSelected" Dense="true" />
                            </MudTd>
                            <MudTd>
                                <MudText Typo="Typo.body2" Style="font-family: monospace;">
                                    @context.Path
                                </MudText>
                            </MudTd>
                            <MudTd>
                                <MudChip T="string" Size="Size.Small" Variant="Variant.Text"
                                         Color="@GetFieldColor(context.FieldType)">
                                    @context.FieldType
                                </MudChip>
                            </MudTd>
                            <MudTd>
                                @if (context.IsArray)
                                {
                                    <MudText Typo="Typo.caption">[@context.ArrayLength]</MudText>
                                }
                            </MudTd>
                            <MudTd>
                                <MudSelect T="string" @bind-Value="context.Role"
                                           Dense="true" Margin="Margin.Dense" Variant="Variant.Text">
                                    <MudSelectItem Value="@("None")">—</MudSelectItem>
                                    <MudSelectItem Value="@("XAxis")">X Axis</MudSelectItem>
                                    <MudSelectItem Value="@("YAxis")">Y Axis</MudSelectItem>
                                </MudSelect>
                            </MudTd>
                            <MudTd>
                                <MudText Typo="Typo.caption" Style="opacity: 0.6; max-width: 120px; overflow: hidden; text-overflow: ellipsis;">
                                    @string.Join(", ", context.SampleValues.Take(3))
                                </MudText>
                            </MudTd>
                        </RowTemplate>
                    </MudTable>

                    @* Widget size *@
                    <MudText Typo="Typo.subtitle2" Class="mt-2">Widget Size</MudText>
                    <MudSlider @bind-Value="_columnSpan" Min="3" Max="12" Step="1"
                               Color="Color.Primary">
                        @(_columnSpan) columns (@(_columnSpan switch {
                            <= 4 => "Small",
                            <= 6 => "Medium",
                            <= 8 => "Large",
                            _ => "Full Width"
                        }))
                    </MudSlider>

                    @* Refresh interval *@
                    <MudTextField @bind-Value="_refreshInterval"
                                  Label="Auto-refresh interval (seconds)"
                                  InputType="InputType.Number"
                                  Variant="Variant.Outlined"
                                  HelperText="0 = no auto-refresh" />
                </MudStack>
            </MudStep>
        </MudStepper>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="AddWidget"
                   Disabled="@(!CanAdd())">
            Add Chart
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [Parameter] public string SectionId { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private MudStepper _stepper = null!;

    // Step 1
    private string _url = "";
    private string? _urlError;
    private string? _analyzeError;
    private bool _isAnalyzing;

    // Step 2
    private string _title = "";
    private string? _description;
    private ChartType _selectedChartType = ChartType.Line;
    private int _columnSpan = 6;
    private int _refreshInterval = 30;
    private List<ChartTypeRecommender.Recommendation> _recommendations = new();
    private List<FieldSelection> _detectedFields = new();
    private ProxyAnalyzeResponse? _analyzeResult;

    private async Task AnalyzeUrl()
    {
        _urlError = null;
        _analyzeError = null;

        if (string.IsNullOrWhiteSpace(_url))
        {
            _urlError = "Please enter a URL.";
            return;
        }

        if (!Uri.TryCreate(_url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            _urlError = "Please enter a valid http:// or https:// URL.";
            return;
        }

        _isAnalyzing = true;
        StateHasChanged();

        try
        {
            _analyzeResult = await ProxyClient.AnalyzeAsync(_url.Trim());

            if (_analyzeResult?.Success != true)
            {
                _analyzeError = _analyzeResult?.Error ?? "Failed to analyze the URL.";
                return;
            }

            // Convert fields to selection model
            _detectedFields = _analyzeResult.Fields.Select(f => new FieldSelection
            {
                Path = f.Path,
                Name = f.Name,
                FieldType = f.FieldType,
                IsArray = f.IsArray,
                ArrayLength = f.ArrayLength,
                SampleValues = f.SampleValues,
                IsSelected = f.Path == _analyzeResult.SuggestedXField ||
                             _analyzeResult.SuggestedYFields.Contains(f.Path),
                Role = f.Path == _analyzeResult.SuggestedXField ? "XAxis" :
                       _analyzeResult.SuggestedYFields.Contains(f.Path) ? "YAxis" : "None"
            }).ToList();

            // Get chart recommendations
            var fieldInfos = _detectedFields.Select(f => new ChartTypeRecommender.FieldInfo
            {
                Path = f.Path,
                Name = f.Name,
                IsNumeric = f.FieldType == "Number",
                IsString = f.FieldType == "String",
                IsDateTime = f.FieldType == "DateTime",
                IsBoolean = f.FieldType == "Boolean",
                IsArray = f.IsArray
            }).ToList();

            _recommendations = Recommender.Recommend(
                _analyzeResult.SuggestedChartType,
                _analyzeResult.SuggestedXField,
                _analyzeResult.SuggestedYFields,
                fieldInfos);

            _selectedChartType = _recommendations.FirstOrDefault()?.ChartType ?? ChartType.Line;

            // Auto-generate title from URL host
            _title = uri.Host.Replace("api.", "").Replace("www.", "")
                .Split('.').First()
                .ToUpper() + " Data";
        }
        catch (Exception ex)
        {
            _analyzeError = $"Error: {ex.Message}";
        }
        finally
        {
            _isAnalyzing = false;
            StateHasChanged();
        }
    }

    private void AddWidget()
    {
        var selectedFields = _detectedFields.Where(f => f.IsSelected).ToList();
        var xField = selectedFields.FirstOrDefault(f => f.Role == "XAxis");
        var yFields = selectedFields.Where(f => f.Role == "YAxis").ToList();

        var widget = new WidgetConfig
        {
            Title = _title,
            Description = _description,
            ChartType = _selectedChartType,
            ColumnSpan = _columnSpan,
            DataSource = new DataSourceConfig
            {
                Url = _url.Trim(),
                XFieldPath = xField?.Path,
                YFieldPaths = yFields.Select(f => f.Path).ToList(),
                RefreshIntervalSeconds = _refreshInterval
            }
        };

        State.AddWidget(SectionId, widget);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();

    private bool CanAdd()
    {
        return !string.IsNullOrWhiteSpace(_title) &&
               _detectedFields.Any(f => f.IsSelected && f.Role == "YAxis");
    }

    private static Color GetFieldColor(string fieldType) => fieldType switch
    {
        "Number" => Color.Primary,
        "String" => Color.Secondary,
        "DateTime" => Color.Tertiary,
        "Boolean" => Color.Info,
        _ => Color.Default
    };

    /// <summary>
    /// Represents a detected field with selection state for the UI.
    /// </summary>
    public class FieldSelection
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string FieldType { get; set; } = "";
        public bool IsArray { get; set; }
        public int ArrayLength { get; set; }
        public List<string> SampleValues { get; set; } = new();
        public bool IsSelected { get; set; }
        public string Role { get; set; } = "None"; // "None", "XAxis", "YAxis"
    }
}
```

---

## User Flow

```
1. User clicks "Add Chart" in a section
   ↓
2. Dialog opens → Step 1: Paste URL
   ↓
3. Click "Analyze Data" → spinner → backend analyzes
   ↓
4. Step 2 shows: detected fields table, recommended chart type chips,
   title/description fields, size slider, refresh interval
   ↓
5. User adjusts settings → clicks "Add Chart"
   ↓
6. Widget added to section → dialog closes → chart renders
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Dialog opens as a MudDialog with stepper
- [ ] Invalid URLs show validation error
- [ ] Analyzing a valid URL shows detected fields in a table
- [ ] Chart type chips show recommendation with "(Recommended)" label
- [ ] Pre-selecting X/Y roles is correct based on server suggestion
- [ ] "Add Chart" creates a WidgetConfig and adds it to state
- [ ] "Add Chart" is disabled until at least one Y-axis field is selected
- [ ] Auto-generated title is reasonable (e.g., "COINGECKO Data")
- [ ] Dialog closes properly on "Add Chart" and "Cancel"
