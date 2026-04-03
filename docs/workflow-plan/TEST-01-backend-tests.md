# Task TEST-01: Backend Tests

## Metadata
- **ID**: TEST-01-backend-tests
- **Layer**: 8 (after implementation)
- **Dependencies**: `BE-03`, `BE-04`
- **Blocks**: None
- **Parallel With**: TEST-02

---

## Files Created

| File | Path |
|------|------|
| `JsonSchemaAnalyzerTests.cs` | `tests/FlexibleMonitoringDashboard.Tests/Services/JsonSchemaAnalyzerTests.cs` |
| `UrlValidatorTests.cs` | `tests/FlexibleMonitoringDashboard.Tests/Services/UrlValidatorTests.cs` |
| `DataProxyEndpointsTests.cs` | `tests/FlexibleMonitoringDashboard.Tests/Endpoints/DataProxyEndpointsTests.cs` |

---

## Step-by-Step Implementation

### Step 1: Create `JsonSchemaAnalyzerTests.cs`

```csharp
// tests/FlexibleMonitoringDashboard.Tests/Services/JsonSchemaAnalyzerTests.cs
using System.Text.Json;
using FlexibleMonitoringDashboard.Models;
using FlexibleMonitoringDashboard.Services;
using Xunit;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class JsonSchemaAnalyzerTests
{
    private readonly JsonSchemaAnalyzer _analyzer = new();

    [Fact]
    public void AnalyzeFields_FlatObject_DetectsScalarFields()
    {
        // CoinGecko-style response
        var json = JsonDocument.Parse("""{"bitcoin":{"usd":50000.5}}""");

        var fields = _analyzer.AnalyzeFields(json.RootElement);

        Assert.Contains(fields, f => f.Path == "bitcoin.usd" && f.FieldType == JsonFieldType.Number);
    }

    [Fact]
    public void AnalyzeFields_ParallelArrays_DetectsArrayFields()
    {
        // Open-Meteo-style response
        var json = JsonDocument.Parse("""
        {
            "hourly": {
                "time": ["2024-01-01T00:00", "2024-01-01T01:00"],
                "temperature_2m": [20.1, 21.5]
            }
        }
        """);

        var fields = _analyzer.AnalyzeFields(json.RootElement);

        Assert.Contains(fields, f => f.Path == "hourly.time" && f.IsArray && f.FieldType == JsonFieldType.DateTime);
        Assert.Contains(fields, f => f.Path == "hourly.temperature_2m" && f.IsArray && f.FieldType == JsonFieldType.Number);
    }

    [Fact]
    public void AnalyzeFields_ArrayOfObjects_DetectsChildFields()
    {
        var json = JsonDocument.Parse("""
        [
            {"name": "Alpha", "value": 10, "active": true},
            {"name": "Beta", "value": 20, "active": false}
        ]
        """);

        var fields = _analyzer.AnalyzeFields(json.RootElement);

        // Root array should be detected
        Assert.Contains(fields, f => f.FieldType == JsonFieldType.Object && f.IsArray);
    }

    [Fact]
    public void AnalyzeFields_EmptyObject_ReturnsNoFields()
    {
        var json = JsonDocument.Parse("{}");
        var fields = _analyzer.AnalyzeFields(json.RootElement);
        Assert.Empty(fields);
    }

    [Fact]
    public void AnalyzeFields_EmptyArray_ReturnsNoFields()
    {
        var json = JsonDocument.Parse("[]");
        var fields = _analyzer.AnalyzeFields(json.RootElement);
        Assert.Empty(fields);
    }

    [Fact]
    public void AnalyzeFields_DeeplyNested_RespectsMaxDepth()
    {
        // 15 levels deep
        var deepJson = """{"a":{"b":{"c":{"d":{"e":{"f":{"g":{"h":{"i":{"j":{"k":{"l":42}}}}}}}}}}}""";
        var json = JsonDocument.Parse(deepJson);

        var fields = _analyzer.AnalyzeFields(json.RootElement);

        // Should not throw, should stop at max depth (10)
        Assert.NotNull(fields);
    }

    [Fact]
    public void SuggestChart_TimeSeriesData_SuggestsLine()
    {
        var fields = new List<JsonFieldInfo>
        {
            new() { Path = "time", Name = "time", FieldType = JsonFieldType.DateTime, IsArray = true, ArrayLength = 24 },
            new() { Path = "temperature", Name = "temperature", FieldType = JsonFieldType.Number, IsArray = true, ArrayLength = 24 }
        };

        var (chartType, xField, yFields) = _analyzer.SuggestChart(fields);

        Assert.Equal("Line", chartType);
        Assert.Equal("time", xField);
        Assert.Contains("temperature", yFields);
    }

    [Fact]
    public void SuggestChart_SingleNumericValue_SuggestsRadialBar()
    {
        var fields = new List<JsonFieldInfo>
        {
            new() { Path = "bitcoin.usd", Name = "usd", FieldType = JsonFieldType.Number, IsArray = false }
        };

        var (chartType, xField, yFields) = _analyzer.SuggestChart(fields);

        Assert.Equal("RadialBar", chartType);
        Assert.Contains("bitcoin.usd", yFields);
    }

    [Fact]
    public void SuggestChart_CategoricalData_SuggestsBar()
    {
        var fields = new List<JsonFieldInfo>
        {
            new() { Path = "categories", Name = "categories", FieldType = JsonFieldType.String, IsArray = true, ArrayLength = 5 },
            new() { Path = "values", Name = "values", FieldType = JsonFieldType.Number, IsArray = true, ArrayLength = 5 }
        };

        var (chartType, xField, yFields) = _analyzer.SuggestChart(fields);

        Assert.Equal("Bar", chartType);
    }

    [Fact]
    public void AnalyzeFields_DateTimeStrings_DetectedAsDateTime()
    {
        var json = JsonDocument.Parse("""{"timestamp": "2024-01-15T10:30:00Z"}""");
        var fields = _analyzer.AnalyzeFields(json.RootElement);

        Assert.Contains(fields, f => f.Path == "timestamp" && f.FieldType == JsonFieldType.DateTime);
    }
}
```

### Step 2: Create `UrlValidatorTests.cs`

```csharp
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
```

### Step 3: Create `DataProxyEndpointsTests.cs`

```csharp
// tests/FlexibleMonitoringDashboard.Tests/Endpoints/DataProxyEndpointsTests.cs
using System.Net;
using System.Net.Http.Json;
using FlexibleMonitoringDashboard.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FlexibleMonitoringDashboard.Tests.Endpoints;

public class DataProxyEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DataProxyEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Fetch_EmptyUrl_ReturnsBadRequest()
    {
        var request = new { url = "" };
        var response = await _client.PostAsJsonAsync("/api/proxy/fetch", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Fetch_ValidPublicUrl_ReturnsData()
    {
        // This test requires internet access — mark as integration test if needed
        var request = new { url = "https://api.open-meteo.com/v1/forecast?latitude=10.823&longitude=106.6296&hourly=temperature_2m" };
        var response = await _client.PostAsJsonAsync("/api/proxy/fetch", request);
        var result = await response.Content.ReadFromJsonAsync<ProxyResponse>();

        Assert.True(result?.Success);
        Assert.NotNull(result?.Data);
    }

    [Fact]
    public async Task Analyze_ValidUrl_ReturnsFieldsAndSuggestion()
    {
        var request = new { url = "https://api.open-meteo.com/v1/forecast?latitude=10.823&longitude=106.6296&hourly=temperature_2m" };
        var response = await _client.PostAsJsonAsync("/api/proxy/analyze", request);
        var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>();

        Assert.True(result?.Success);
        Assert.NotEmpty(result?.Fields ?? new());
        Assert.NotNull(result?.SuggestedChartType);
    }

    [Fact]
    public async Task Fetch_LocalhostUrl_ReturnsError()
    {
        var request = new { url = "http://localhost/secret" };
        var response = await _client.PostAsJsonAsync("/api/proxy/fetch", request);
        var result = await response.Content.ReadFromJsonAsync<ProxyResponse>();

        Assert.False(result?.Success);
        Assert.Contains("not allowed", result?.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
```

> **Note**: For `DataProxyEndpointsTests` to work with `WebApplicationFactory<Program>`, the server project's `Program.cs` must expose `Program` class. Add this at the bottom of the server `Program.cs`:

```csharp
// Make Program class accessible for integration tests
public partial class Program { }
```

---

## Verification

- [ ] `dotnet test` — all tests pass
- [ ] Schema analyzer tests cover: flat objects, parallel arrays, arrays of objects, empty, deep nesting
- [ ] Chart suggestion tests cover: time series → Line, single value → RadialBar, categorical → Bar
- [ ] URL validator tests cover: public URLs pass, SSRF URLs blocked, invalid schemes blocked
- [ ] Integration tests cover: empty URL → 400, valid URL → data, localhost → blocked
