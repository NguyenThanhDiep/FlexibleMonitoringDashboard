// src/FlexibleMonitoringDashboard.Client/Models/DetectedField.cs
namespace FlexibleMonitoringDashboard.Client.Models;

/// <summary>
/// Represents a field detected in a JSON API response, used by field selector components.
/// </summary>
public class DetectedField
{
    /// <summary>
    /// Dot-separated path to the field (e.g., "hourly.temperature_2m").
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Simple field name (e.g., "temperature_2m").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detected data type as a string (e.g., "Number", "String", "DateTime", "Boolean").
    /// </summary>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this field contains an array of values.
    /// </summary>
    public bool IsArray { get; set; }

    /// <summary>
    /// Number of values in the array (if IsArray is true).
    /// </summary>
    public int ArrayLength { get; set; }

    /// <summary>
    /// Up to 5 sample values for preview.
    /// </summary>
    public List<string> SampleValues { get; set; } = new();
}
