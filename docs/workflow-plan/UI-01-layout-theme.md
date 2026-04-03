# Task UI-01: Layout & Theme Setup

## Metadata
- **ID**: UI-01-layout-theme
- **Layer**: 3
- **Dependencies**: `01-project-scaffolding`
- **Blocks**: `UI-02`
- **Parallel With**: UI-07, UI-09, UI-10

---

## Files Modified

| File | Action |
|------|--------|
| `MainLayout.razor` | **Modify** at `src/FlexibleMonitoringDashboard/Components/Layout/MainLayout.razor` |
| `App.razor` | **Modify** at `src/FlexibleMonitoringDashboard/Components/App.razor` (if not done in scaffolding) |

---

## Step-by-Step Implementation

### Step 1: Update `MainLayout.razor`

Replace the default layout with a MudBlazor layout that includes:
- App bar with product branding
- Dark/light theme toggle
- Toolbar slots for Import/Export/Add buttons (implemented by other tasks)

```razor
@* src/FlexibleMonitoringDashboard/Components/Layout/MainLayout.razor *@
@inherits LayoutComponentBase
@using MudBlazor

<MudThemeProvider @bind-IsDarkMode="@_isDarkMode" Theme="_theme" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1" Dense="true">
        <MudIcon Icon="@Icons.Material.Filled.Dashboard" Class="mr-2" />
        <MudText Typo="Typo.h6" Class="mr-4">FlexBoard</MudText>
        <MudText Typo="Typo.subtitle2" Class="d-none d-sm-flex" Style="opacity: 0.7;">
            Flexible Monitoring Dashboard
        </MudText>
        <MudSpacer />

        @* Toolbar buttons placeholder — child components will render here via CascadingValue *@
        <div id="toolbar-actions">
            @ToolbarContent
        </div>

        <MudIconButton Icon="@(_isDarkMode ? Icons.Material.Filled.LightMode : Icons.Material.Filled.DarkMode)"
                       Color="Color.Inherit"
                       OnClick="ToggleTheme"
                       Title="@(_isDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode")" />
    </MudAppBar>

    <MudMainContent Class="pt-16 px-4 pb-4">
        <CascadingValue Value="this">
            @Body
        </CascadingValue>
    </MudMainContent>
</MudLayout>

@code {
    private bool _isDarkMode = false;
    private RenderFragment? ToolbarContent { get; set; }

    private MudTheme _theme = new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976D2",
            Secondary = "#FF9800",
            AppbarBackground = "#1976D2",
            AppbarText = "#FFFFFF",
            Background = "#F5F5F5",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90CAF9",
            Secondary = "#FFB74D",
            AppbarBackground = "#1E1E2E",
            AppbarText = "#FFFFFF",
            Background = "#121212",
            Surface = "#1E1E2E",
            DrawerBackground = "#1E1E2E"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" }
            }
        }
    };

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
    }

    /// <summary>
    /// Called by child components to register toolbar buttons.
    /// </summary>
    public void SetToolbarContent(RenderFragment content)
    {
        ToolbarContent = content;
        StateHasChanged();
    }

    /// <summary>
    /// Gets or sets dark mode. Used by child components.
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _isDarkMode = value;
            StateHasChanged();
        }
    }
}
```

### Step 2: Update `MainLayout.razor.css` (if exists)

Create or update the scoped CSS:

```css
/* src/FlexibleMonitoringDashboard/Components/Layout/MainLayout.razor.css */
/* Keep minimal — MudBlazor handles most styling */
```

### Step 3: Verify `App.razor` Has Required Providers

Ensure `App.razor` (configured in scaffolding) references MudBlazor CSS/JS. This should already be done in task `01-project-scaffolding`.

---

## Design Decisions

- **MudAppBar**: Dense mode for compact toolbar. Blue primary color.
- **Product name**: "FlexBoard" — short, memorable, dashboard-related.
- **Theme**: Light by default. Blue/Orange primary/secondary accent. Custom typography with Roboto.
- **CascadingValue**: `MainLayout` cascades itself so child components can register toolbar buttons.
- **Responsive**: Product subtitle hidden on small screens (`d-none d-sm-flex`).

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] App renders with blue app bar and "FlexBoard" title
- [ ] Dark/light theme toggle works (icon changes, colors switch)
- [ ] Layout is responsive — subtitle hides on small screens
- [ ] MudDialog, MudSnackbar, MudPopover providers are registered
- [ ] Body content renders within `MudMainContent`
