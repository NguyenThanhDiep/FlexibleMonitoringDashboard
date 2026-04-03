# Task BE-02: External API Service

## Metadata
- **ID**: BE-02-external-api-service
- **Layer**: 1
- **Dependencies**: `01-project-scaffolding`
- **Blocks**: `BE-04`
- **Parallel With**: BE-01, BE-03, FE-01, FE-04, FE-05, FE-06, UI-12, DOC-*

---

## Files Created

| File | Path |
|------|------|
| `ExternalApiService.cs` | `src/FlexibleMonitoringDashboard/Services/ExternalApiService.cs` |
| `UrlValidator.cs` | `src/FlexibleMonitoringDashboard/Services/UrlValidator.cs` |

---

## Step-by-Step Implementation

### Step 1: Create `UrlValidator.cs`

**SECURITY CRITICAL**: This class prevents SSRF (Server-Side Request Forgery) attacks by validating URLs before the server fetches them.

```csharp
// src/FlexibleMonitoringDashboard/Services/UrlValidator.cs
using System.Net;
using System.Net.Sockets;

namespace FlexibleMonitoringDashboard.Services;

/// <summary>
/// Validates URLs to prevent SSRF attacks.
/// Only allows http/https to public IP addresses.
/// </summary>
public static class UrlValidator
{
    /// <summary>
    /// Validates that the URL is safe to fetch from the server.
    /// </summary>
    public static async Task<(bool IsValid, string? Error)> ValidateUrlAsync(string url)
    {
        // 1. Check URL format
        if (string.IsNullOrWhiteSpace(url))
            return (false, "URL cannot be empty.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, "Invalid URL format.");

        // 2. Only allow http and https schemes
        if (uri.Scheme != "http" && uri.Scheme != "https")
            return (false, "Only http:// and https:// URLs are allowed.");

        // 3. Block localhost and loopback
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("127.0.0.1") ||
            uri.Host.Equals("[::1]") ||
            uri.Host.Equals("0.0.0.0"))
            return (false, "Localhost URLs are not allowed.");

        // 4. Resolve DNS and check for private IPs
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host);
            foreach (var address in addresses)
            {
                if (IsPrivateIp(address))
                    return (false, "URLs pointing to private/internal networks are not allowed.");
            }
        }
        catch (SocketException)
        {
            return (false, "Could not resolve hostname.");
        }

        // 5. Block common internal hostnames
        var blockedHosts = new[] { "metadata.google.internal", "169.254.169.254", "metadata" };
        if (blockedHosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)))
            return (false, "Access to cloud metadata endpoints is not allowed.");

        return (true, null);
    }

    /// <summary>
    /// Checks if an IP address is in a private/reserved range.
    /// </summary>
    private static bool IsPrivateIp(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => bytes[0] switch
            {
                10 => true,                                         // 10.0.0.0/8
                127 => true,                                        // 127.0.0.0/8
                169 when bytes[1] == 254 => true,                   // 169.254.0.0/16 (link-local)
                172 when bytes[1] >= 16 && bytes[1] <= 31 => true,  // 172.16.0.0/12
                192 when bytes[1] == 168 => true,                   // 192.168.0.0/16
                0 => true,                                          // 0.0.0.0/8
                _ => false
            },
            AddressFamily.InterNetworkV6 => address.IsIPv6LinkLocal ||
                                            address.IsIPv6SiteLocal ||
                                            IPAddress.IsLoopback(address),
            _ => true // Block unknown address families
        };
    }
}
```

### Step 2: Create `ExternalApiService.cs`

```csharp
// src/FlexibleMonitoringDashboard/Services/ExternalApiService.cs
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
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] `UrlValidator` rejects: `file:///etc/passwd`, `http://localhost`, `http://127.0.0.1`, `http://169.254.169.254`, `http://10.0.0.1`
- [ ] `UrlValidator` allows: `https://api.coingecko.com/...`, `https://api.open-meteo.com/...`
- [ ] `ExternalApiService` returns `JsonElement` (not string) for valid responses
- [ ] Response size is capped at 10 MB
- [ ] Timeout is capped at 60 seconds
- [ ] Dangerous headers (Host, Cookie, Authorization) are blocked
