// src/FlexibleMonitoringDashboard.Client/Components/Dashboard/ChartDataPoint.cs
namespace FlexibleMonitoringDashboard.Client.Components.Dashboard;

/// <summary>
/// Generic data point for chart rendering.
/// </summary>
public class ChartDataPoint
{
    public string X { get; set; } = "";
    public double Y { get; set; }
    public string Series { get; set; } = "";
}
