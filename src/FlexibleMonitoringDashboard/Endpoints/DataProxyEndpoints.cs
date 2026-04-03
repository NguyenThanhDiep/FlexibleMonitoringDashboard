// src/FlexibleMonitoringDashboard/Endpoints/DataProxyEndpoints.cs
using FlexibleMonitoringDashboard.Models;
using FlexibleMonitoringDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlexibleMonitoringDashboard.Endpoints;

/// <summary>
/// Minimal API endpoints for proxying external JSON API requests.
/// </summary>
public static class DataProxyEndpoints
{
    /// <summary>
    /// Maps all proxy-related endpoints.
    /// </summary>
    public static void MapDataProxyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/proxy")
            .WithTags("DataProxy");

        group.MapPost("/fetch", FetchAsync)
            .WithName("FetchExternalData")
            .WithDescription("Fetches JSON from a public API URL via server proxy.");

        group.MapPost("/analyze", AnalyzeAsync)
            .WithName("AnalyzeExternalData")
            .WithDescription("Fetches JSON and analyzes its structure for chart configuration.");
    }

    /// <summary>
    /// POST /api/proxy/fetch
    /// Fetches JSON data from an external public API URL.
    /// </summary>
    private static async Task<IResult> FetchAsync(
        [FromBody] ProxyRequest request,
        ExternalApiService apiService)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return Results.BadRequest(new ProxyResponse
            {
                Success = false,
                Error = "URL is required."
            });
        }

        var (success, data, statusCode, responseTime, error) = await apiService.FetchJsonAsync(
            request.Url,
            request.Headers,
            request.TimeoutSeconds);

        return Results.Ok(new ProxyResponse
        {
            Success = success,
            Data = data,
            StatusCode = statusCode,
            ResponseTimeMs = responseTime,
            Error = error
        });
    }

    /// <summary>
    /// POST /api/proxy/analyze
    /// Fetches JSON from an external URL and analyzes its field structure.
    /// </summary>
    private static async Task<IResult> AnalyzeAsync(
        [FromBody] ProxyRequest request,
        ExternalApiService apiService,
        JsonSchemaAnalyzer analyzer)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return Results.BadRequest(new AnalyzeResponse
            {
                Success = false,
                Error = "URL is required."
            });
        }

        // Fetch the JSON data
        var (success, data, statusCode, responseTime, error) = await apiService.FetchJsonAsync(
            request.Url,
            request.Headers,
            request.TimeoutSeconds);

        if (!success || data == null)
        {
            return Results.Ok(new AnalyzeResponse
            {
                Success = false,
                Error = error ?? "Failed to fetch data."
            });
        }

        // Analyze the JSON structure
        var fields = analyzer.AnalyzeFields(data.Value);
        var (chartType, xField, yFields) = analyzer.SuggestChart(fields);

        return Results.Ok(new AnalyzeResponse
        {
            Success = true,
            Data = data,
            Fields = fields,
            SuggestedChartType = chartType,
            SuggestedXField = xField,
            SuggestedYFields = yFields
        });
    }
}
