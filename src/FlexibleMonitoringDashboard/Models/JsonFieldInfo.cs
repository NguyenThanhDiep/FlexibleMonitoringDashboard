// src/FlexibleMonitoringDashboard/Models/JsonFieldInfo.cs
namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Describes a field detected in a JSON API response.
/// </summary>
public class JsonFieldInfo
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
    /// Detected data type of the field values.
    /// </summary>
    public JsonFieldType FieldType { get; set; }

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

/// <summary>
/// Detected type of a JSON field.
/// </summary>
public enum JsonFieldType
{
    String,
    Number,
    Boolean,
    DateTime,
    Object,
    Unknown
}
