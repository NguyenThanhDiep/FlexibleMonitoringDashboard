using System.Net.Http.Json;
using System.Text.Json;

namespace FlexibleMonitoringDashboard.Client.Services;

/// <summary>
/// Client-side HTTP service that calls backend proxy endpoints
/// to fetch and analyze external JSON APIs.
/// </summary>
public class DataProxyClient
{
    private readonly HttpClient _httpClient;

    public DataProxyClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fetches JSON data from an external API via the backend proxy.
    /// </summary>
    /// <param name="url">The public JSON API URL.</param>
    /// <param name="headers">Optional custom headers.</param>
    /// <returns>Proxy response containing the JSON data or error.</returns>
    public async Task<ProxyResponseDto?> FetchAsync(string url, Dictionary<string, string>? headers = null)
    {
        var request = new ProxyRequestDto
        {
            Url = url,
            Headers = headers
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/proxy/fetch", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProxyResponseDto>();
        }
        catch (HttpRequestException ex)
        {
            return new ProxyResponseDto
            {
                Success = false,
                Error = $"Failed to contact server: {ex.Message}"
            };
        }
        catch (JsonException)
        {
            return new ProxyResponseDto
            {
                Success = false,
                Error = "Invalid response from server."
            };
        }
    }

    /// <summary>
    /// Fetches and analyzes JSON data from an external API via the backend proxy.
    /// Returns detected fields and chart suggestions.
    /// </summary>
    /// <param name="url">The public JSON API URL.</param>
    /// <param name="headers">Optional custom headers.</param>
    /// <returns>Analyze response with fields and chart suggestions.</returns>
    public async Task<AnalyzeResponseDto?> AnalyzeAsync(string url, Dictionary<string, string>? headers = null)
    {
        var request = new ProxyRequestDto
        {
            Url = url,
            Headers = headers
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/proxy/analyze", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AnalyzeResponseDto>();
        }
        catch (HttpRequestException ex)
        {
            return new AnalyzeResponseDto
            {
                Success = false,
                Error = $"Failed to contact server: {ex.Message}"
            };
        }
        catch (JsonException)
        {
            return new AnalyzeResponseDto
            {
                Success = false,
                Error = "Invalid response from server."
            };
        }
    }
}

// ───────── Client-side DTOs ─────────
// These mirror the server models but are independent to avoid
// sharing assemblies between Client and Server projects.

public class ProxyRequestDto
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string>? Headers { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public class ProxyResponseDto
{
    public bool Success { get; set; }
    public JsonElement? Data { get; set; }
    public string? Error { get; set; }
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
}

public class AnalyzeResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public JsonElement? Data { get; set; }
    public List<FieldInfoDto> Fields { get; set; } = new();
    public string? SuggestedChartType { get; set; }
    public string? SuggestedXField { get; set; }
    public List<string> SuggestedYFields { get; set; } = new();
}

public class FieldInfoDto
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsArray { get; set; }
    public int ArrayLength { get; set; }
    public List<string> SampleValues { get; set; } = new();
}
