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
