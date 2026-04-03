# Task FE-01: Client Configuration Models

## Metadata
- **ID**: FE-01-config-models
- **Layer**: 1
- **Dependencies**: `01-project-scaffolding`
- **Blocks**: `FE-02`, `FE-03`, `UI-05`, `UI-07`
- **Parallel With**: BE-01, BE-02, BE-03, FE-04, FE-05, FE-06, UI-12, DOC-*

---

## Files Created

| File | Path |
|------|------|
| `DashboardConfig.cs` | `src/FlexibleMonitoringDashboard.Client/Models/DashboardConfig.cs` |
| `SectionConfig.cs` | `src/FlexibleMonitoringDashboard.Client/Models/SectionConfig.cs` |
| `WidgetConfig.cs` | `src/FlexibleMonitoringDashboard.Client/Models/WidgetConfig.cs` |
| `DataSourceConfig.cs` | `src/FlexibleMonitoringDashboard.Client/Models/DataSourceConfig.cs` |
| `CorrelationConfig.cs` | `src/FlexibleMonitoringDashboard.Client/Models/CorrelationConfig.cs` |
| `ThresholdConfig.cs` | `src/FlexibleMonitoringDashboard.Client/Models/ThresholdConfig.cs` |
| `ChartType.cs` | `src/FlexibleMonitoringDashboard.Client/Models/ChartType.cs` |
| `FieldMapping.cs` | `src/FlexibleMonitoringDashboard.Client/Models/FieldMapping.cs` |

---

## Step-by-Step Implementation

### Step 1: Create `ChartType.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/ChartType.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Supported chart types for widgets.
/// </summary>
public enum ChartType
{
    Line,
    Bar,
    Area,
    Pie,
    Donut,
    RadialBar,
    Scatter
}
```

### Step 2: Create `FieldMapping.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/FieldMapping.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Maps a JSON field path to an axis role in a chart.
/// </summary>
public class FieldMapping
{
    /// <summary>
    /// The JSON dot-path to the field (e.g., "hourly.temperature_2m").
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Display label for this field in the chart legend.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Role of this field (XAxis, YAxis, Category, Value).
    /// </summary>
    public FieldRole Role { get; set; }
}

/// <summary>
/// The role a field plays in a chart.
/// </summary>
public enum FieldRole
{
    XAxis,
    YAxis,
    Category,
    Value
}
```

### Step 3: Create `DataSourceConfig.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/DataSourceConfig.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Configuration for a single data source (public JSON API).
/// </summary>
public class DataSourceConfig
{
    /// <summary>
    /// Unique identifier for this data source.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// The public JSON API URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom headers (e.g., API keys).
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Auto-refresh interval in seconds. 0 = no auto-refresh.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Optional JSON path to drill into (e.g., "data.items" to access nested arrays).
    /// Empty means use root element.
    /// </summary>
    public string? JsonPath { get; set; }
}
```

### Step 4: Create `ThresholdConfig.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/ThresholdConfig.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Threshold alarm configuration for a widget (nice-to-have feature).
/// </summary>
public class ThresholdConfig
{
    /// <summary>
    /// The threshold value to compare against.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Direction of the threshold trigger.
    /// </summary>
    public ThresholdDirection Direction { get; set; } = ThresholdDirection.Above;

    /// <summary>
    /// Color to use for the alarm indicator.
    /// </summary>
    public string Color { get; set; } = "#FF5252";

    /// <summary>
    /// Label for the threshold line shown on the chart.
    /// </summary>
    public string Label { get; set; } = "Threshold";

    /// <summary>
    /// Whether the threshold alarm is currently enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

public enum ThresholdDirection
{
    Above,
    Below
}
```

### Step 5: Create `WidgetConfig.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/WidgetConfig.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Configuration for a single chart widget on the dashboard.
/// </summary>
public class WidgetConfig
{
    /// <summary>
    /// Unique identifier for this widget.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Display title shown above the chart.
    /// </summary>
    public string Title { get; set; } = "Untitled Chart";

    /// <summary>
    /// Optional description shown below the title.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The chart type to render.
    /// </summary>
    public ChartType ChartType { get; set; } = ChartType.Line;

    /// <summary>
    /// Primary data source configuration.
    /// </summary>
    public DataSourceConfig DataSource { get; set; } = new();

    /// <summary>
    /// Field mappings (which JSON fields map to which chart axes).
    /// </summary>
    public List<FieldMapping> FieldMappings { get; set; } = new();

    /// <summary>
    /// Widget width in grid columns (1-12 for MudGrid).
    /// </summary>
    public int ColumnSpan { get; set; } = 6;

    /// <summary>
    /// Optional threshold alarm configuration.
    /// </summary>
    public ThresholdConfig? Threshold { get; set; }

    /// <summary>
    /// Correlation config (if this is a cross-source overlay widget).
    /// Null for regular single-source widgets.
    /// </summary>
    public CorrelationConfig? Correlation { get; set; }
}
```

### Step 6: Create `CorrelationConfig.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/CorrelationConfig.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Configuration for a cross-source correlation widget
/// that overlays data from two different APIs in one chart.
/// </summary>
public class CorrelationConfig
{
    /// <summary>
    /// Secondary data source for the overlay.
    /// (Primary data source is the widget's main DataSource.)
    /// </summary>
    public DataSourceConfig SecondaryDataSource { get; set; } = new();

    /// <summary>
    /// Field mappings for the secondary data source.
    /// </summary>
    public List<FieldMapping> SecondaryFieldMappings { get; set; } = new();

    /// <summary>
    /// Whether to use a separate Y-axis (right side) for the secondary series.
    /// </summary>
    public bool UseDualYAxis { get; set; } = true;

    /// <summary>
    /// Label for the secondary Y-axis.
    /// </summary>
    public string? SecondaryYAxisLabel { get; set; }

    /// <summary>
    /// Label for the primary Y-axis.
    /// </summary>
    public string? PrimaryYAxisLabel { get; set; }
}
```

### Step 7: Create `SectionConfig.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/SectionConfig.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// A section groups related widgets under a common header/description.
/// </summary>
public class SectionConfig
{
    /// <summary>
    /// Unique identifier for this section.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Section header/title.
    /// </summary>
    public string Title { get; set; } = "New Section";

    /// <summary>
    /// Optional description shown below the title.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ordered list of widgets in this section.
    /// </summary>
    public List<WidgetConfig> Widgets { get; set; } = new();

    /// <summary>
    /// Whether this section is collapsed in the UI.
    /// </summary>
    public bool IsCollapsed { get; set; } = false;
}
```

### Step 8: Create `DashboardConfig.cs`

```csharp
// src/FlexibleMonitoringDashboard.Client/Models/DashboardConfig.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Root configuration object for the entire dashboard.
/// This is what gets serialized to/from JSON for export/import.
/// </summary>
public class DashboardConfig
{
    /// <summary>
    /// Schema version for forward/backward compatibility.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Dashboard display name.
    /// </summary>
    public string Name { get; set; } = "My Dashboard";

    /// <summary>
    /// Optional dashboard description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ordered list of sections in the dashboard.
    /// </summary>
    public List<SectionConfig> Sections { get; set; } = new();

    /// <summary>
    /// Timestamp when this config was last exported.
    /// </summary>
    public DateTime? ExportedAt { get; set; }

    /// <summary>
    /// Whether to use dark theme.
    /// </summary>
    public bool DarkMode { get; set; } = false;
}
```

---

## JSON Config Example (for reference)

This is what the exported JSON file will look like:

```json
{
  "version": "1.0",
  "name": "Crypto & Weather Dashboard",
  "description": "Monitoring BTC price and Ho Chi Minh City weather",
  "darkMode": false,
  "exportedAt": "2024-01-15T10:30:00Z",
  "sections": [
    {
      "id": "abc123",
      "title": "Cryptocurrency Prices",
      "description": "Real-time crypto market data",
      "isCollapsed": false,
      "widgets": [
        {
          "id": "w001",
          "title": "Bitcoin Price (USD)",
          "description": "Current BTC/USD from CoinGecko",
          "chartType": "RadialBar",
          "columnSpan": 4,
          "dataSource": {
            "url": "https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids=bitcoin",
            "refreshIntervalSeconds": 60
          },
          "fieldMappings": [
            { "fieldPath": "bitcoin.usd", "label": "BTC Price", "role": "Value" }
          ],
          "threshold": {
            "value": 50000,
            "direction": "Above",
            "color": "#4CAF50",
            "label": "Target",
            "enabled": true
          }
        }
      ]
    }
  ]
}
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] All models are in `FlexibleMonitoringDashboard.Client.Models` namespace
- [ ] `DashboardConfig` can be serialized/deserialized with `System.Text.Json`
- [ ] All `Id` properties auto-generate unique values
- [ ] `WidgetConfig.ColumnSpan` defaults to 6 (half-width)
- [ ] `DataSourceConfig.RefreshIntervalSeconds` defaults to 30
- [ ] `ChartType` enum covers: Line, Bar, Area, Pie, Donut, RadialBar, Scatter
