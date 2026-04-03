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
