// tests/FlexibleMonitoringDashboard.Tests/Services/UrlValidatorTests.cs
using FlexibleMonitoringDashboard.Services;
using Xunit;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class UrlValidatorTests
{
    [Theory]
    [InlineData("https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids=bitcoin")]
    [InlineData("https://api.open-meteo.com/v1/forecast?latitude=10.823&longitude=106.6296")]
    [InlineData("http://example.com/data.json")]
    public async Task ValidateUrl_PublicUrls_ReturnsValid(string url)
    {
        var (isValid, error) = await UrlValidator.ValidateUrlAsync(url);
        Assert.True(isValid, $"Expected valid but got error: {error}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://files.example.com/data.json")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    public async Task ValidateUrl_InvalidSchemes_ReturnsInvalid(string url)
    {
        var (isValid, _) = await UrlValidator.ValidateUrlAsync(url);
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("http://localhost/api")]
    [InlineData("http://127.0.0.1/api")]
    [InlineData("http://[::1]/api")]
    [InlineData("http://0.0.0.0/api")]
    public async Task ValidateUrl_Localhost_ReturnsInvalid(string url)
    {
        var (isValid, error) = await UrlValidator.ValidateUrlAsync(url);
        Assert.False(isValid);
        Assert.Contains("not allowed", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://metadata.google.internal/computeMetadata/v1")]
    public async Task ValidateUrl_CloudMetadata_ReturnsInvalid(string url)
    {
        var (isValid, _) = await UrlValidator.ValidateUrlAsync(url);
        Assert.False(isValid);
    }
}
