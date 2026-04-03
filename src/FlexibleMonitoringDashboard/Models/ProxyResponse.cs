// src/FlexibleMonitoringDashboard/Models/ProxyResponse.cs
using System.Text.Json;

namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Response payload from the proxy fetch endpoint.
/// </summary>
public class ProxyResponse
{
    /// <summary>
    /// Whether the external API call was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The raw JSON data returned by the external API.
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Error message if the call failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// HTTP status code from the external API.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }
}
