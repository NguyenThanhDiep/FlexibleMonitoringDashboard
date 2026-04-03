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
