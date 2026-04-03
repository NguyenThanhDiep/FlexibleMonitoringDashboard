# Task UI-02: Dashboard Page (Main Page)

## Metadata
- **ID**: UI-02-dashboard-page
- **Layer**: 4
- **Dependencies**: `UI-01`
- **Blocks**: `UI-03`
- **Parallel With**: UI-05, UI-06

---

## Files Created/Modified

| File | Action |
|------|--------|
| `Dashboard.razor` | **Create** at `src/FlexibleMonitoringDashboard.Client/Pages/Dashboard.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `Dashboard.razor`

This is the main (and only) page. It's the route `/` and orchestrates the entire dashboard UI.

```razor
@* src/FlexibleMonitoringDashboard.Client/Pages/Dashboard.razor *@
@page "/"
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@inject DashboardStateService State
@inject ConfigurationService ConfigService
@inject IDialogService DialogService
@inject ISnackbar Snackbar
@implements IDisposable

<PageTitle>FlexBoard — Monitoring Dashboard</PageTitle>

@* Toolbar actions — rendered into the MainLayout app bar *@
<MudToolBar Dense="true" DisableGutters="true" Class="d-flex gap-2">
    <MudButton Variant="Variant.Text"
               Color="Color.Inherit"
               StartIcon="@Icons.Material.Filled.Add"
               OnClick="AddSection"
               Size="Size.Small">
        Section
    </MudButton>

    <MudButton Variant="Variant.Text"
               Color="Color.Inherit"
               StartIcon="@Icons.Material.Filled.FileUpload"
               OnClick="ImportConfig"
               Size="Size.Small">
        Import
    </MudButton>

    <MudButton Variant="Variant.Text"
               Color="Color.Inherit"
               StartIcon="@Icons.Material.Filled.FileDownload"
               OnClick="ExportConfig"
               Size="Size.Small"
               Disabled="@(!State.IsDirty && State.Config.Sections.Count == 0)">
        Export
    </MudButton>
</MudToolBar>

@* Main content *@
@if (State.Config.Sections.Count == 0)
{
    @* Empty state *@
    <MudContainer MaxWidth="MaxWidth.Small" Class="mt-16 text-center">
        <MudIcon Icon="@Icons.Material.Filled.DashboardCustomize"
                 Style="font-size: 80px; opacity: 0.3;" />
        <MudText Typo="Typo.h4" Class="mt-4" Style="opacity: 0.6;">
            Welcome to FlexBoard
        </MudText>
        <MudText Typo="Typo.body1" Class="mt-2 mb-6" Style="opacity: 0.5;">
            Start building your monitoring dashboard by adding a section,
            or import an existing configuration.
        </MudText>
        <MudStack Row="true" Justify="Justify.Center" Spacing="4">
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       Size="Size.Large"
                       StartIcon="@Icons.Material.Filled.Add"
                       OnClick="AddSection">
                Add First Section
            </MudButton>
            <MudButton Variant="Variant.Outlined"
                       Color="Color.Primary"
                       Size="Size.Large"
                       StartIcon="@Icons.Material.Filled.FileUpload"
                       OnClick="ImportConfig">
                Import Config
            </MudButton>
        </MudStack>
    </MudContainer>
}
else
{
    @* Dashboard content — rendered by DashboardContainer *@
    <DashboardContainer />

    @* Floating add section button at the bottom *@
    <MudStack AlignItems="AlignItems.Center" Class="mt-4 mb-8">
        <MudButton Variant="Variant.Outlined"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="AddSection">
            Add Section
        </MudButton>
    </MudStack>
}

@* Unsaved changes guard *@
<UnsavedChangesGuard />

@code {
    protected override void OnInitialized()
    {
        State.OnStateChanged += StateHasChanged;
    }

    private void AddSection()
    {
        State.AddSection();
    }

    private async Task ImportConfig()
    {
        // This will be implemented in UI-09 (ImportButton component)
        // For now, placeholder that opens a file upload dialog
    }

    private async Task ExportConfig()
    {
        try
        {
            await ConfigService.ExportAsync();
            Snackbar.Add("Dashboard configuration exported successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
    }

    public void Dispose()
    {
        State.OnStateChanged -= StateHasChanged;
    }
}
```

### Step 2: Verify Route Registration

The page route `@page "/"` should be automatically picked up by the Blazor router. Ensure `Routes.razor` exists in the Client project:

```razor
@* src/FlexibleMonitoringDashboard.Client/Routes.razor (if not already created) *@
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="typeof(Layout.MainLayout)">
            <MudText Typo="Typo.h6">Page not found</MudText>
        </LayoutView>
    </NotFound>
</Router>
```

> **Note**: The actual Routes.razor location depends on Blazor Web App structure. In the default template, Routes.razor is in the server project and references are auto-resolved. Verify during implementation.

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Navigating to `/` shows the dashboard page
- [ ] Empty state shows welcome message with "Add First Section" and "Import Config" buttons
- [ ] After adding a section, empty state disappears and `DashboardContainer` renders
- [ ] Export button triggers file download
- [ ] Export button is disabled when no sections exist and not dirty
- [ ] Adding sections updates UI reactively via `OnStateChanged`
- [ ] Page title shows "FlexBoard — Monitoring Dashboard"
