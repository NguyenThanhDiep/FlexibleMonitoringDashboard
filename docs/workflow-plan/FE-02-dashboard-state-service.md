# Task FE-02: Dashboard State Service

## Metadata
- **ID**: FE-02-dashboard-state-service
- **Layer**: 2
- **Dependencies**: `FE-01`
- **Blocks**: `UI-03`, `UI-10`
- **Parallel With**: FE-03, BE-04

---

## Files Created/Modified

| File | Action |
|------|--------|
| `DashboardStateService.cs` | **Create** at `src/FlexibleMonitoringDashboard.Client/Services/DashboardStateService.cs` |
| `Program.cs` | **Modify** — register service (can be deferred until integration) |

---

## Step-by-Step Implementation

### Step 1: Create `DashboardStateService.cs`

This is the central state management service. It holds the entire dashboard configuration in memory and notifies UI components when state changes.

```csharp
// src/FlexibleMonitoringDashboard.Client/Services/DashboardStateService.cs
using FlexibleMonitoringDashboard.Client.Models;

namespace FlexibleMonitoringDashboard.Client.Services;

/// <summary>
/// Manages dashboard state in memory. Provides CRUD operations for sections and widgets.
/// Notifies subscribers on state changes for UI re-rendering.
/// </summary>
public class DashboardStateService
{
    private DashboardConfig _config = new();

    /// <summary>
    /// Fires whenever the dashboard state changes.
    /// UI components should subscribe to this to trigger re-renders.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Whether unsaved changes exist (config modified since last export).
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Gets the current dashboard configuration (read-only snapshot).
    /// </summary>
    public DashboardConfig Config => _config;

    // ───────── Dashboard-Level Operations ─────────

    /// <summary>
    /// Replaces the entire dashboard configuration (used for import).
    /// </summary>
    public void LoadConfig(DashboardConfig config)
    {
        _config = config;
        IsDirty = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// Clears all sections and widgets.
    /// </summary>
    public void Clear()
    {
        _config = new DashboardConfig();
        IsDirty = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// Updates the dashboard name.
    /// </summary>
    public void SetDashboardName(string name)
    {
        _config.Name = name;
        MarkDirty();
    }

    /// <summary>
    /// Updates the dashboard description.
    /// </summary>
    public void SetDashboardDescription(string? description)
    {
        _config.Description = description;
        MarkDirty();
    }

    /// <summary>
    /// Toggles dark mode.
    /// </summary>
    public void SetDarkMode(bool darkMode)
    {
        _config.DarkMode = darkMode;
        MarkDirty();
    }

    // ───────── Section Operations ─────────

    /// <summary>
    /// Adds a new section to the dashboard.
    /// Returns the new section's Id.
    /// </summary>
    public string AddSection(string title = "New Section", string? description = null)
    {
        var section = new SectionConfig
        {
            Title = title,
            Description = description
        };
        _config.Sections.Add(section);
        MarkDirty();
        return section.Id;
    }

    /// <summary>
    /// Updates a section's title and description.
    /// </summary>
    public void UpdateSection(string sectionId, string title, string? description)
    {
        var section = FindSection(sectionId);
        if (section == null) return;

        section.Title = title;
        section.Description = description;
        MarkDirty();
    }

    /// <summary>
    /// Removes a section and all its widgets.
    /// </summary>
    public void RemoveSection(string sectionId)
    {
        _config.Sections.RemoveAll(s => s.Id == sectionId);
        MarkDirty();
    }

    /// <summary>
    /// Toggles section collapsed state.
    /// </summary>
    public void ToggleSectionCollapse(string sectionId)
    {
        var section = FindSection(sectionId);
        if (section == null) return;

        section.IsCollapsed = !section.IsCollapsed;
        NotifyStateChanged(); // UI-only change, don't mark dirty
    }

    /// <summary>
    /// Moves a section up or down in the list.
    /// </summary>
    public void MoveSection(string sectionId, int direction)
    {
        var index = _config.Sections.FindIndex(s => s.Id == sectionId);
        if (index < 0) return;

        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _config.Sections.Count) return;

        var section = _config.Sections[index];
        _config.Sections.RemoveAt(index);
        _config.Sections.Insert(newIndex, section);
        MarkDirty();
    }

    // ───────── Widget Operations ─────────

    /// <summary>
    /// Adds a widget to a specific section.
    /// Returns the new widget's Id.
    /// </summary>
    public string AddWidget(string sectionId, WidgetConfig widget)
    {
        var section = FindSection(sectionId);
        if (section == null) return string.Empty;

        section.Widgets.Add(widget);
        MarkDirty();
        return widget.Id;
    }

    /// <summary>
    /// Updates a widget's configuration.
    /// </summary>
    public void UpdateWidget(string sectionId, WidgetConfig updatedWidget)
    {
        var section = FindSection(sectionId);
        if (section == null) return;

        var index = section.Widgets.FindIndex(w => w.Id == updatedWidget.Id);
        if (index < 0) return;

        section.Widgets[index] = updatedWidget;
        MarkDirty();
    }

    /// <summary>
    /// Removes a widget from its section.
    /// </summary>
    public void RemoveWidget(string sectionId, string widgetId)
    {
        var section = FindSection(sectionId);
        if (section == null) return;

        section.Widgets.RemoveAll(w => w.Id == widgetId);
        MarkDirty();
    }

    /// <summary>
    /// Moves a widget up or down within its section.
    /// </summary>
    public void MoveWidget(string sectionId, string widgetId, int direction)
    {
        var section = FindSection(sectionId);
        if (section == null) return;

        var index = section.Widgets.FindIndex(w => w.Id == widgetId);
        if (index < 0) return;

        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= section.Widgets.Count) return;

        var widget = section.Widgets[index];
        section.Widgets.RemoveAt(index);
        section.Widgets.Insert(newIndex, widget);
        MarkDirty();
    }

    // ───────── Query Helpers ─────────

    /// <summary>
    /// Gets all widgets across all sections (flat list).
    /// </summary>
    public IEnumerable<WidgetConfig> GetAllWidgets()
    {
        return _config.Sections.SelectMany(s => s.Widgets);
    }

    /// <summary>
    /// Checks if the dashboard has any widgets.
    /// </summary>
    public bool HasWidgets => _config.Sections.Any(s => s.Widgets.Count > 0);

    /// <summary>
    /// Gets the total number of widgets.
    /// </summary>
    public int WidgetCount => _config.Sections.Sum(s => s.Widgets.Count);

    /// <summary>
    /// Marks the state as saved (not dirty). Called after export.
    /// </summary>
    public void MarkClean()
    {
        IsDirty = false;
    }

    // ───────── Internal Helpers ─────────

    private SectionConfig? FindSection(string sectionId)
    {
        return _config.Sections.FirstOrDefault(s => s.Id == sectionId);
    }

    private void MarkDirty()
    {
        IsDirty = true;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
```

### Step 2: Register in Client `Program.cs`

Add to `src/FlexibleMonitoringDashboard.Client/Program.cs`:

```csharp
using FlexibleMonitoringDashboard.Client.Services;

// After other service registrations:
builder.Services.AddScoped<DashboardStateService>();
```

> **Note**: If working in parallel, just add a comment placeholder `// Register DashboardStateService` and another task will wire it up.

---

## Usage Example (for reference by UI tasks)

```csharp
// In a Blazor component:
@inject DashboardStateService State

@code {
    protected override void OnInitialized()
    {
        State.OnStateChanged += StateHasChanged;
    }

    private void AddSection()
    {
        State.AddSection("My Section", "Description here");
    }

    public void Dispose()
    {
        State.OnStateChanged -= StateHasChanged;
    }
}
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] `AddSection()` creates section with unique Id and fires `OnStateChanged`
- [ ] `AddWidget()` adds widget to correct section and fires `OnStateChanged`
- [ ] `RemoveWidget()` removes widget and fires `OnStateChanged`
- [ ] `IsDirty` is `true` after any modification
- [ ] `MarkClean()` resets `IsDirty` to `false`
- [ ] `LoadConfig()` replaces entire config and resets `IsDirty`
- [ ] `Clear()` resets to empty dashboard
- [ ] `MoveSection()` and `MoveWidget()` correctly reorder items
