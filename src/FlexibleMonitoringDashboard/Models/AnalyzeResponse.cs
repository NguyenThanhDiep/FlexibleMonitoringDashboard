// src/FlexibleMonitoringDashboard/Models/AnalyzeResponse.cs
using System.Text.Json;

namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Response from the analyze endpoint — includes raw data + detected fields.
/// </summary>
public class AnalyzeResponse
{
    /// <summary>
    /// Whether the external API call and analysis were successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The raw JSON data from the API.
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// List of detected fields in the JSON response.
    /// </summary>
    public List<JsonFieldInfo> Fields { get; set; } = new();

    /// <summary>
    /// Suggested chart type based on the detected fields.
    /// </summary>
    public string? SuggestedChartType { get; set; }

    /// <summary>
    /// Suggested X-axis field path.
    /// </summary>
    public string? SuggestedXField { get; set; }

    /// <summary>
    /// Suggested Y-axis field path(s).
    /// </summary>
    public List<string> SuggestedYFields { get; set; } = new();
}
