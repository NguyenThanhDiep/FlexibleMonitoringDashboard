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
