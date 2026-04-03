// tests/FlexibleMonitoringDashboard.Tests/Services/ConfigurationServiceTests.cs
using System.Text.Json;
using FlexibleMonitoringDashboard.Client.Models;
using FlexibleMonitoringDashboard.Client.Services;
using Microsoft.JSInterop;
using Moq;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class ConfigurationServiceTests
{
    private readonly ConfigurationService _service;

    public ConfigurationServiceTests()
    {
        var mockJs = new Mock<IJSRuntime>();
        _service = new ConfigurationService(mockJs.Object);
    }

    [Fact]
    public void SerializeConfig_ProducesValidCamelCaseJson()
    {
        var config = new DashboardConfig { Name = "Test Dashboard" };
        config.Sections.Add(new SectionConfig { Title = "Test Section" });

        var json = _service.SerializeConfig(config);

        Assert.NotNull(json);
        Assert.Contains("\"name\":", json);
        Assert.Contains("Test Dashboard", json);

        // Must be valid JSON
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void ImportFromJson_ValidJson_ReturnsConfig()
    {
        var json = """
        {
            "version": "1.0",
            "name": "Test Dashboard",
            "sections": [
                {
                    "id": "abc123",
                    "title": "Section 1",
                    "widgets": [
                        {
                            "id": "w1",
                            "title": "Widget 1",
                            "chartType": "line",
                            "dataSource": {
                                "url": "https://api.example.com/data"
                            },
                            "columnSpan": 6
                        }
                    ]
                }
            ]
        }
        """;

        var (config, error) = _service.ImportFromJson(json);

        Assert.Null(error);
        Assert.NotNull(config);
        Assert.Equal("Test Dashboard", config.Name);
        Assert.Single(config.Sections);
        Assert.Single(config.Sections[0].Widgets);
    }

    [Fact]
    public void ImportFromJson_InvalidJson_ReturnsError()
    {
        var (config, error) = _service.ImportFromJson("{ invalid json }}}");

        Assert.Null(config);
        Assert.NotNull(error);
        Assert.Contains("Invalid JSON", error);
    }

    [Fact]
    public void ImportFromJson_EmptyString_ReturnsError()
    {
        var (config, error) = _service.ImportFromJson("");

        Assert.Null(config);
        Assert.NotNull(error);
    }

    [Fact]
    public void ImportFromJson_MissingWidgetUrl_ReturnsError()
    {
        var json = """
        {
            "version": "1.0",
            "name": "Test",
            "sections": [
                {
                    "id": "s1",
                    "title": "Section",
                    "widgets": [
                        {
                            "id": "w1",
                            "title": "Widget",
                            "chartType": "line",
                            "dataSource": { "url": "" },
                            "columnSpan": 6
                        }
                    ]
                }
            ]
        }
        """;

        var (config, error) = _service.ImportFromJson(json);

        Assert.Null(config);
        Assert.NotNull(error);
    }

    [Fact]
    public void RoundTrip_SerializeImport_PreservesData()
    {
        var original = new DashboardConfig { Name = "Crypto Dashboard" };
        var section = new SectionConfig { Title = "Crypto" };
        section.Widgets.Add(new WidgetConfig
        {
            Title = "BTC Price",
            ChartType = ChartType.RadialBar,
            DataSource = new DataSourceConfig
            {
                Url = "https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids=bitcoin",
                RefreshIntervalSeconds = 60
            },
            ColumnSpan = 4
        });
        original.Sections.Add(section);

        var json = _service.SerializeConfig(original);
        var (config, error) = _service.ImportFromJson(json);

        Assert.Null(error);
        Assert.NotNull(config);
        Assert.Equal("Crypto Dashboard", config.Name);
        Assert.Single(config.Sections);
        Assert.Single(config.Sections[0].Widgets);
        Assert.Equal("BTC Price", config.Sections[0].Widgets[0].Title);
        Assert.Equal(ChartType.RadialBar, config.Sections[0].Widgets[0].ChartType);
    }

    [Fact]
    public void ImportFromJson_EnumAsString_ParsedCorrectly()
    {
        var json = """
        {
            "version": "1.0",
            "name": "Test",
            "sections": [
                {
                    "id": "s1",
                    "title": "Section",
                    "widgets": [
                        {
                            "id": "w1",
                            "title": "Widget",
                            "chartType": "bar",
                            "dataSource": { "url": "https://example.com/api" },
                            "columnSpan": 6
                        }
                    ]
                }
            ]
        }
        """;

        var (config, error) = _service.ImportFromJson(json);

        Assert.Null(error);
        Assert.Equal(ChartType.Bar, config!.Sections[0].Widgets[0].ChartType);
    }

    [Fact]
    public void ImportFromJson_EmptyVersion_ReturnsError()
    {
        var json = """
        {
            "version": "",
            "name": "Test",
            "sections": []
        }
        """;

        var (config, error) = _service.ImportFromJson(json);

        Assert.Null(config);
        Assert.NotNull(error);
    }
}
