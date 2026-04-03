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
