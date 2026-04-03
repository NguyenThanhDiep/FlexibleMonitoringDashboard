// src/FlexibleMonitoringDashboard/Models/ProxyRequest.cs
namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Request payload for the proxy fetch endpoint.
/// </summary>
public class ProxyRequest
{
    /// <summary>
    /// The public JSON API URL to fetch data from.
    /// Must be http:// or https:// scheme.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom headers to include in the request (e.g., API keys).
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional timeout in seconds. Default: 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
