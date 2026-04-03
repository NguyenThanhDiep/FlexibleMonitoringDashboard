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
