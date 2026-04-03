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
