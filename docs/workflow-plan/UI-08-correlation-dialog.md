# Task UI-08: Correlation Dialog

## Metadata
- **ID**: UI-08-correlation-dialog
- **Layer**: 5
- **Dependencies**: `UI-05`
- **Blocks**: None
- **Parallel With**: UI-03, UI-11

---

## Files Created

| File | Path |
|------|------|
| `CorrelationDialog.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Correlation/CorrelationDialog.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `CorrelationDialog.razor`

This dialog lets users overlay data from two different APIs on one chart with dual Y-axes.

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Correlation/CorrelationDialog.razor *@
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@inject DataProxyClient ProxyClient
@inject DashboardStateService State
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center">
            <MudIcon Icon="@Icons.Material.Filled.CompareArrows" Class="mr-2" />
            <MudText Typo="Typo.h6">Cross-Source Correlation</MudText>
        </MudStack>
    </TitleContent>
    <DialogContent>
        <MudText Typo="Typo.body2" Class="mb-4" Style="opacity: 0.7;">
            Overlay data from two different APIs in one chart with dual Y-axes.
        </MudText>

        <MudGrid Spacing="3">
            @* Primary source *@
            <MudItem xs="12" md="6">
                <MudPaper Elevation="0" Class="pa-3" Style="border: 1px solid; border-color: var(--mud-palette-primary); border-radius: 8px;">
                    <MudText Typo="Typo.subtitle2" Color="Color.Primary" Class="mb-2">
                        Primary Source (Left Y-Axis)
                    </MudText>
                    <MudTextField @bind-Value="_primaryUrl"
                                  Label="API URL"
                                  Variant="Variant.Outlined"
                                  Margin="Margin.Dense" />
                    <MudButton Variant="Variant.Text" Color="Color.Primary"
                               Size="Size.Small" OnClick="AnalyzePrimary"
                               Disabled="_isAnalyzingPrimary" Class="mt-2">
                        @(_isAnalyzingPrimary ? "Analyzing..." : "Analyze")
                    </MudButton>
                    @if (_primaryFields.Count > 0)
                    {
                        <MudSelect T="string" @bind-Value="_primaryYField"
                                   Label="Y-Axis Field" Variant="Variant.Outlined"
                                   Margin="Margin.Dense" Class="mt-2">
                            @foreach (var f in _primaryFields.Where(f => f.FieldType == "Number"))
                            {
                                <MudSelectItem Value="@f.Path">@f.Path</MudSelectItem>
                            }
                        </MudSelect>
                        <MudTextField @bind-Value="_primaryYLabel"
                                      Label="Y-Axis Label"
                                      Variant="Variant.Outlined"
                                      Margin="Margin.Dense" Class="mt-2" />
                    }
                </MudPaper>
            </MudItem>

            @* Secondary source *@
            <MudItem xs="12" md="6">
                <MudPaper Elevation="0" Class="pa-3" Style="border: 1px solid; border-color: var(--mud-palette-secondary); border-radius: 8px;">
                    <MudText Typo="Typo.subtitle2" Color="Color.Secondary" Class="mb-2">
                        Secondary Source (Right Y-Axis)
                    </MudText>
                    <MudTextField @bind-Value="_secondaryUrl"
                                  Label="API URL"
                                  Variant="Variant.Outlined"
                                  Margin="Margin.Dense" />
                    <MudButton Variant="Variant.Text" Color="Color.Secondary"
                               Size="Size.Small" OnClick="AnalyzeSecondary"
                               Disabled="_isAnalyzingSecondary" Class="mt-2">
                        @(_isAnalyzingSecondary ? "Analyzing..." : "Analyze")
                    </MudButton>
                    @if (_secondaryFields.Count > 0)
                    {
                        <MudSelect T="string" @bind-Value="_secondaryYField"
                                   Label="Y-Axis Field" Variant="Variant.Outlined"
                                   Margin="Margin.Dense" Class="mt-2">
                            @foreach (var f in _secondaryFields.Where(f => f.FieldType == "Number"))
                            {
                                <MudSelectItem Value="@f.Path">@f.Path</MudSelectItem>
                            }
                        </MudSelect>
                        <MudTextField @bind-Value="_secondaryYLabel"
                                      Label="Y-Axis Label"
                                      Variant="Variant.Outlined"
                                      Margin="Margin.Dense" Class="mt-2" />
                    }
                </MudPaper>
            </MudItem>
        </MudGrid>

        @* General settings *@
        <MudStack Spacing="3" Class="mt-4">
            <MudTextField @bind-Value="_title"
                          Label="Chart Title"
                          Variant="Variant.Outlined"
                          Required="true" />
            <MudTextField @bind-Value="_description"
                          Label="Description (optional)"
                          Variant="Variant.Outlined" />
            <MudSlider @bind-Value="_columnSpan" Min="6" Max="12" Step="1"
                       Color="Color.Primary">
                Width: @_columnSpan columns
            </MudSlider>
        </MudStack>

        @if (!string.IsNullOrEmpty(_error))
        {
            <MudAlert Severity="Severity.Error" Dense="true" Class="mt-3">@_error</MudAlert>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled"
                   OnClick="AddCorrelation" Disabled="@(!CanAdd())">
            Add Correlation Chart
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [Parameter] public string SectionId { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    // Primary
    private string _primaryUrl = "";
    private bool _isAnalyzingPrimary;
    private List<DetectedField> _primaryFields = new();
    private string? _primaryYField;
    private string _primaryYLabel = "Primary";

    // Secondary
    private string _secondaryUrl = "";
    private bool _isAnalyzingSecondary;
    private List<DetectedField> _secondaryFields = new();
    private string? _secondaryYField;
    private string _secondaryYLabel = "Secondary";

    // General
    private string _title = "Correlation Chart";
    private string? _description;
    private int _columnSpan = 12;
    private string? _error;

    private async Task AnalyzePrimary()
    {
        _isAnalyzingPrimary = true;
        _error = null;
        StateHasChanged();

        var result = await ProxyClient.AnalyzeAsync(_primaryUrl.Trim());
        if (result.Success)
            _primaryFields = result.Fields;
        else
            _error = $"Primary: {result.Error}";

        _isAnalyzingPrimary = false;
    }

    private async Task AnalyzeSecondary()
    {
        _isAnalyzingSecondary = true;
        _error = null;
        StateHasChanged();

        var result = await ProxyClient.AnalyzeAsync(_secondaryUrl.Trim());
        if (result.Success)
            _secondaryFields = result.Fields;
        else
            _error = $"Secondary: {result.Error}";

        _isAnalyzingSecondary = false;
    }

    private void AddCorrelation()
    {
        var widget = new WidgetConfig
        {
            Title = _title,
            Description = _description,
            ChartType = ChartType.Line,
            ColumnSpan = _columnSpan,
            IsCorrelation = true,
            DataSource = new DataSourceConfig
            {
                Url = _primaryUrl.Trim(),
                YFieldPaths = new List<string> { _primaryYField! }
            },
            Correlation = new CorrelationConfig
            {
                PrimarySource = new DataSourceConfig
                {
                    Url = _primaryUrl.Trim(),
                    YFieldPaths = new List<string> { _primaryYField! }
                },
                SecondarySource = new DataSourceConfig
                {
                    Url = _secondaryUrl.Trim(),
                    YFieldPaths = new List<string> { _secondaryYField! }
                },
                PrimaryYAxisLabel = _primaryYLabel,
                SecondaryYAxisLabel = _secondaryYLabel
            }
        };

        State.AddWidget(SectionId, widget);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();

    private bool CanAdd() =>
        !string.IsNullOrWhiteSpace(_title) &&
        !string.IsNullOrWhiteSpace(_primaryYField) &&
        !string.IsNullOrWhiteSpace(_secondaryYField);
}
```

---

## User Flow

```
1. User opens correlation dialog (from section menu or toolbar)
2. Paste primary API URL → Analyze → select numeric Y field
3. Paste secondary API URL → Analyze → select numeric Y field
4. Set title, labels, size → "Add Correlation Chart"
5. Widget renders with both series on dual Y-axes
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Dialog renders with two side-by-side URL input panels
- [ ] Each panel can independently analyze a URL and show fields
- [ ] Only numeric fields appear in Y-axis selectors
- [ ] "Add Correlation Chart" creates a widget with `IsCorrelation = true`
- [ ] Widget's `Correlation` property has both data sources configured
- [ ] "Add" button is disabled until both Y fields are selected
- [ ] Error messages show per-source (Primary/Secondary prefix)
