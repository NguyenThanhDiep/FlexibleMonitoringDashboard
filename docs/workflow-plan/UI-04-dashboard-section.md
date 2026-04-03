# Task UI-04: Dashboard Section

## Metadata
- **ID**: UI-04-dashboard-section
- **Layer**: 6
- **Dependencies**: `UI-03`
- **Blocks**: None
- **Parallel With**: None (depends on UI-03 which depends on UI-02/FE-02)

---

## Files Created

| File | Path |
|------|------|
| `DashboardSection.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Dashboard/DashboardSection.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `DashboardSection.razor`

This component renders a single section with:
- Editable header and description
- Visual divider/separator
- Grid of chart widgets
- "Add Chart" button
- Section-level toolbar (move, collapse, delete)

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Dashboard/DashboardSection.razor *@
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@inject DashboardStateService State
@inject IDialogService DialogService
@inject ISnackbar Snackbar

@* Section divider and header *@
<MudDivider Class="mt-4 mb-2" />

<MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-2">
    @* Editable title *@
    @if (_isEditingTitle)
    {
        <MudTextField @bind-Value="_editTitle"
                      Variant="Variant.Text"
                      Margin="Margin.Dense"
                      Style="font-size: 1.25rem; font-weight: 500;"
                      Immediate="true"
                      OnBlur="SaveTitle"
                      OnKeyDown="OnTitleKeyDown"
                      AutoFocus="true" />
    }
    else
    {
        <MudText Typo="Typo.h6"
                 Style="cursor: pointer;"
                 @onclick="StartEditTitle"
                 Title="Click to edit section title">
            @SectionConfig.Title
        </MudText>
    }

    <MudSpacer />

    @* Section actions toolbar *@
    <MudIconButton Icon="@Icons.Material.Filled.ArrowUpward"
                   Size="Size.Small"
                   Disabled="IsFirst"
                   OnClick="OnMoveUp"
                   Title="Move section up" />
    <MudIconButton Icon="@Icons.Material.Filled.ArrowDownward"
                   Size="Size.Small"
                   Disabled="IsLast"
                   OnClick="OnMoveDown"
                   Title="Move section down" />
    <MudIconButton Icon="@(_isCollapsed ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ExpandLess)"
                   Size="Size.Small"
                   OnClick="ToggleCollapse"
                   Title="@(_isCollapsed ? "Expand section" : "Collapse section")" />
    <MudIconButton Icon="@Icons.Material.Filled.Delete"
                   Size="Size.Small"
                   Color="Color.Error"
                   OnClick="OnDelete"
                   Title="Delete section" />
</MudStack>

@* Optional description *@
@if (_isEditingDescription)
{
    <MudTextField @bind-Value="_editDescription"
                  Variant="Variant.Text"
                  Margin="Margin.Dense"
                  Placeholder="Add a description..."
                  Style="font-size: 0.875rem; opacity: 0.7;"
                  OnBlur="SaveDescription"
                  Lines="2"
                  AutoFocus="true" />
}
else if (!string.IsNullOrWhiteSpace(SectionConfig.Description))
{
    <MudText Typo="Typo.body2"
             Style="opacity: 0.6; cursor: pointer;"
             Class="mb-2"
             @onclick="StartEditDescription"
             Title="Click to edit description">
        @SectionConfig.Description
    </MudText>
}
else if (!_isCollapsed)
{
    <MudLink Typo="Typo.caption"
             Style="opacity: 0.4; cursor: pointer;"
             Class="mb-2"
             @onclick="StartEditDescription">
        + Add description
    </MudLink>
}

@* Section content (widgets grid) *@
@if (!_isCollapsed)
{
    <MudGrid Spacing="3" Class="mt-2">
        @foreach (var widget in SectionConfig.Widgets.OrderBy(w => w.Order))
        {
            <MudItem xs="12" sm="@GetSmBreakpoint(widget.ColumnSpan)" md="@widget.ColumnSpan">
                <ChartWidget WidgetConfig="widget"
                             SectionId="SectionConfig.Id" />
            </MudItem>
        }

        @* Add chart card *@
        <MudItem xs="12" sm="6" md="4">
            <MudPaper Elevation="0"
                      Class="d-flex align-center justify-center"
                      Style="min-height: 200px; border: 2px dashed; border-color: var(--mud-palette-primary); opacity: 0.5; cursor: pointer; border-radius: 8px;"
                      @onclick="AddChart">
                <MudStack AlignItems="AlignItems.Center">
                    <MudIcon Icon="@Icons.Material.Filled.AddChart" Style="font-size: 40px;" />
                    <MudText Typo="Typo.body2">Add Chart</MudText>
                </MudStack>
            </MudPaper>
        </MudItem>
    </MudGrid>
}

@code {
    [Parameter] public SectionConfig SectionConfig { get; set; } = null!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }
    [Parameter] public EventCallback OnMoveUp { get; set; }
    [Parameter] public EventCallback OnMoveDown { get; set; }
    [Parameter] public bool IsFirst { get; set; }
    [Parameter] public bool IsLast { get; set; }

    private bool _isCollapsed = false;
    private bool _isEditingTitle = false;
    private bool _isEditingDescription = false;
    private string _editTitle = "";
    private string _editDescription = "";

    private void StartEditTitle()
    {
        _editTitle = SectionConfig.Title;
        _isEditingTitle = true;
    }

    private void SaveTitle()
    {
        _isEditingTitle = false;
        if (!string.IsNullOrWhiteSpace(_editTitle) && _editTitle != SectionConfig.Title)
        {
            State.UpdateSection(SectionConfig.Id, _editTitle, SectionConfig.Description);
        }
    }

    private void OnTitleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") SaveTitle();
        if (e.Key == "Escape") _isEditingTitle = false;
    }

    private void StartEditDescription()
    {
        _editDescription = SectionConfig.Description ?? "";
        _isEditingDescription = true;
    }

    private void SaveDescription()
    {
        _isEditingDescription = false;
        var desc = string.IsNullOrWhiteSpace(_editDescription) ? null : _editDescription;
        if (desc != SectionConfig.Description)
        {
            State.UpdateSection(SectionConfig.Id, SectionConfig.Title, desc);
        }
    }

    private void ToggleCollapse()
    {
        _isCollapsed = !_isCollapsed;
    }

    private async Task AddChart()
    {
        // Opens the AddDataSourceDialog (implemented in UI-06)
        var parameters = new DialogParameters
        {
            { "SectionId", SectionConfig.Id }
        };
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true
        };
        var dialog = await DialogService.ShowAsync<AddDataSourceDialog>("Add Chart", parameters, options);
        await dialog.Result;
    }

    /// <summary>
    /// Maps column span to sm breakpoint (smaller screens get wider widgets).
    /// </summary>
    private static int GetSmBreakpoint(int columnSpan) => columnSpan switch
    {
        <= 4 => 6,   // Small widgets become half-width on tablet
        <= 8 => 12,  // Medium widgets become full-width on tablet
        _ => 12      // Large widgets stay full-width
    };
}
```

---

## Design Details

- **Inline editing**: Click on title/description to edit in-place. Save on blur or Enter. Cancel on Escape.
- **Collapsible sections**: Toggle arrow collapses/expands the widget grid.
- **Add Chart card**: Dashed-border card with "+" icon. Clicking opens the `AddDataSourceDialog`.
- **Responsive grid**: Widgets use `MudGrid` with `xs=12` (mobile full-width), `sm` (tablet), `md` (desktop = column span).
- **Section reordering**: Up/down arrows disabled at boundaries.

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Section renders with title, optional description, and widget grid
- [ ] Clicking title switches to edit mode; saving updates state
- [ ] Collapse button hides/shows widgets
- [ ] "Add Chart" card is visible and clickable (opens dialog)
- [ ] Widget grid is responsive (full-width on mobile, split on desktop)
- [ ] Delete/Move callbacks fire correctly
- [ ] "Add description" link appears when description is empty
