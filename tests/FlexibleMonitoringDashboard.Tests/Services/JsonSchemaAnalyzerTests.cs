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
        // 12 levels deep (exceeds MaxDepth of 10)
        var deepJson = """{"a":{"b":{"c":{"d":{"e":{"f":{"g":{"h":{"i":{"j":{"k":{"l":42}}}}}}}}}}}}""";
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
