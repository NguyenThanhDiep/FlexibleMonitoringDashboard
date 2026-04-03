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
