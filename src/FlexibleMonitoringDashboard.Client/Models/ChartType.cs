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
