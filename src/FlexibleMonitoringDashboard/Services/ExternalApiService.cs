using System.Diagnostics;
using System.Text.Json;

namespace FlexibleMonitoringDashboard.Services;

/// <summary>
/// Service that fetches JSON data from external public APIs.
/// Includes URL validation (SSRF protection) and timeout handling.
/// </summary>
public class ExternalApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalApiService> _logger;

    private const int MaxResponseSizeBytes = 10 * 1024 * 1024; // 10 MB limit

    public ExternalApiService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Fetches JSON from the given URL after validating it's safe.
    /// Returns (success, jsonElement, statusCode, responseTimeMs, error).
    /// </summary>
    public async Task<(bool Success, JsonElement? Data, int StatusCode, long ResponseTimeMs, string? Error)>
        FetchJsonAsync(string url, Dictionary<string, string>? headers = null, int timeoutSeconds = 30)
    {
        // Validate URL against SSRF
        var (isValid, validationError) = await UrlValidator.ValidateUrlAsync(url);
        if (!isValid)
        {
            _logger.LogWarning("URL validation failed for {Url}: {Error}", url, validationError);
            return (false, null, 0, 0, validationError);
        }

        var client = _httpClientFactory.CreateClient("ExternalApi");
        client.Timeout = TimeSpan.FromSeconds(Math.Min(timeoutSeconds, 60)); // Cap at 60s

        // Add custom headers
        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                // Prevent injection of dangerous headers
                if (!IsAllowedHeader(key)) continue;
                client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
            }
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(url);
            stopwatch.Stop();

            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, statusCode, stopwatch.ElapsedMilliseconds,
                    $"External API returned HTTP {statusCode}.");
            }

            // Check content length to prevent OOM
            if (response.Content.Headers.ContentLength > MaxResponseSizeBytes)
            {
                return (false, null, statusCode, stopwatch.ElapsedMilliseconds,
                    $"Response too large (max {MaxResponseSizeBytes / 1024 / 1024} MB).");
            }

            var jsonString = await response.Content.ReadAsStringAsync();

            // Additional size check (content-length may be absent)
            if (jsonString.Length > MaxResponseSizeBytes)
            {
                return (false, null, statusCode, stopwatch.ElapsedMilliseconds,
                    $"Response too large (max {MaxResponseSizeBytes / 1024 / 1024} MB).");
            }

            var jsonDoc = JsonDocument.Parse(jsonString);
            return (true, jsonDoc.RootElement.Clone(), statusCode, stopwatch.ElapsedMilliseconds, null);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return (false, null, 0, stopwatch.ElapsedMilliseconds, "Request timed out.");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP request failed for {Url}", url);
            return (false, null, 0, stopwatch.ElapsedMilliseconds, $"Request failed: {ex.Message}");
        }
        catch (JsonException)
        {
            stopwatch.Stop();
            return (false, null, 200, stopwatch.ElapsedMilliseconds, "Response is not valid JSON.");
        }
    }

    /// <summary>
    /// Whitelist of allowed custom headers.
    /// Prevents users from setting Host, Cookie, Authorization to internal services.
    /// </summary>
    private static bool IsAllowedHeader(string headerName)
    {
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Host", "Cookie", "Authorization", "Proxy-Authorization",
            "X-Forwarded-For", "X-Forwarded-Host", "X-Real-IP"
        };
        return !blocked.Contains(headerName);
    }
}
