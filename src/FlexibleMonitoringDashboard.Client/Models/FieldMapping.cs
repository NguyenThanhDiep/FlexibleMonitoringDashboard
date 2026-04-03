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
