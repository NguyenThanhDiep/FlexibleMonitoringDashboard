# Task TEST-02: Frontend Service Tests

## Metadata
- **ID**: TEST-02-frontend-tests
- **Layer**: 8 (after implementation)
- **Dependencies**: `FE-03`, `FE-04`
- **Blocks**: None
- **Parallel With**: TEST-01

---

## Files Created

| File | Path |
|------|------|
| `ChartTypeRecommenderTests.cs` | `tests/FlexibleMonitoringDashboard.Tests/Services/ChartTypeRecommenderTests.cs` |
| `ConfigurationServiceTests.cs` | `tests/FlexibleMonitoringDashboard.Tests/Services/ConfigurationServiceTests.cs` |
| `DashboardStateServiceTests.cs` | `tests/FlexibleMonitoringDashboard.Tests/Services/DashboardStateServiceTests.cs` |

---

## Step-by-Step Implementation

### Step 1: Create `ChartTypeRecommenderTests.cs`

```csharp
// tests/FlexibleMonitoringDashboard.Tests/Services/ChartTypeRecommenderTests.cs
using FlexibleMonitoringDashboard.Client.Models;
using FlexibleMonitoringDashboard.Client.Services;
using Xunit;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class ChartTypeRecommenderTests
{
    private readonly ChartTypeRecommender _recommender = new();

    [Fact]
    public void Recommend_TimeSeriesFields_SuggestsLine()
    {
        var fields = new List<ChartTypeRecommender.FieldInfo>
        {
            new() { Path = "time", Name = "time", IsDateTime = true, IsArray = true },
            new() { Path = "temperature", Name = "temperature", IsNumeric = true, IsArray = true }
        };

        var recommendations = _recommender.Recommend("Line", "time", new() { "temperature" }, fields);

        Assert.NotEmpty(recommendations);
        Assert.Equal(ChartType.Line, recommendations[0].ChartType);
    }

    [Fact]
    public void Recommend_SingleNumericField_SuggestsRadialBar()
    {
        var fields = new List<ChartTypeRecommender.FieldInfo>
        {
            new() { Path = "bitcoin.usd", Name = "usd", IsNumeric = true, IsArray = false }
        };

        var recommendations = _recommender.Recommend("RadialBar", null, new() { "bitcoin.usd" }, fields);

        Assert.Contains(recommendations, r => r.ChartType == ChartType.RadialBar);
    }

    [Fact]
    public void Recommend_CategoricalFields_SuggestsBar()
    {
        var fields = new List<ChartTypeRecommender.FieldInfo>
        {
            new() { Path = "categories", Name = "categories", IsString = true, IsArray = true },
            new() { Path = "values", Name = "values", IsNumeric = true, IsArray = true }
        };

        var recommendations = _recommender.Recommend("Bar", "categories", new() { "values" }, fields);

        Assert.Contains(recommendations, r => r.ChartType == ChartType.Bar);
    }

    [Fact]
    public void Recommend_NullSuggestion_ReturnsFallback()
    {
        var fields = new List<ChartTypeRecommender.FieldInfo>();
        var recommendations = _recommender.Recommend(null, null, new(), fields);

        Assert.NotEmpty(recommendations); // Should have at least one suggestion
    }

    [Fact]
    public void Recommend_NoDuplicateChartTypes()
    {
        var fields = new List<ChartTypeRecommender.FieldInfo>
        {
            new() { Path = "time", Name = "time", IsDateTime = true, IsArray = true },
            new() { Path = "temp", Name = "temp", IsNumeric = true, IsArray = true }
        };

        var recommendations = _recommender.Recommend("Line", "time", new() { "temp" }, fields);

        var types = recommendations.Select(r => r.ChartType).ToList();
        Assert.Equal(types.Count, types.Distinct().Count());
    }

    [Fact]
    public void Recommend_SortedByConfidenceDescending()
    {
        var fields = new List<ChartTypeRecommender.FieldInfo>
        {
            new() { Path = "time", Name = "time", IsDateTime = true, IsArray = true },
            new() { Path = "temp", Name = "temp", IsNumeric = true, IsArray = true }
        };

        var recommendations = _recommender.Recommend("Line", "time", new() { "temp" }, fields);

        for (int i = 1; i < recommendations.Count; i++)
        {
            Assert.True(recommendations[i - 1].Confidence >= recommendations[i].Confidence);
        }
    }
}
```

### Step 2: Create `ConfigurationServiceTests.cs`

```csharp
// tests/FlexibleMonitoringDashboard.Tests/Services/ConfigurationServiceTests.cs
using System.Text.Json;
using FlexibleMonitoringDashboard.Client.Models;
using FlexibleMonitoringDashboard.Client.Services;
using Moq;
using Microsoft.JSInterop;
using Xunit;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class ConfigurationServiceTests
{
    private readonly ConfigurationService _service;
    private readonly DashboardStateService _stateService;

    public ConfigurationServiceTests()
    {
        var mockJs = new Mock<IJSRuntime>();
        _stateService = new DashboardStateService();
        _service = new ConfigurationService(mockJs.Object, _stateService);
    }

    [Fact]
    public void SerializeConfig_ProducesValidJson()
    {
        _stateService.AddSection("Test Section", "Description");
        var json = _service.SerializeConfig();

        Assert.NotNull(json);
        Assert.Contains("\"name\":", json); // camelCase
        Assert.Contains("Test Section", json);

        // Should be parseable
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void DeserializeConfig_ValidJson_ReturnsConfig()
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

        var (config, error) = _service.DeserializeConfig(json);

        Assert.Null(error);
        Assert.NotNull(config);
        Assert.Equal("Test Dashboard", config.Name);
        Assert.Single(config.Sections);
        Assert.Single(config.Sections[0].Widgets);
    }

    [Fact]
    public void DeserializeConfig_InvalidJson_ReturnsError()
    {
        var (config, error) = _service.DeserializeConfig("{ invalid json }}}");

        Assert.Null(config);
        Assert.NotNull(error);
        Assert.Contains("Invalid JSON", error);
    }

    [Fact]
    public void DeserializeConfig_EmptyString_ReturnsError()
    {
        var (config, error) = _service.DeserializeConfig("");

        Assert.Null(config);
        Assert.NotNull(error);
    }

    [Fact]
    public void DeserializeConfig_MissingWidgetUrl_ReturnsError()
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

        var (config, error) = _service.DeserializeConfig(json);

        // Should fail validation because URL is empty
        Assert.NotNull(error);
    }

    [Fact]
    public void RoundTrip_SerializeDeserialize_PreservesData()
    {
        // Setup state
        _stateService.AddSection("Crypto", "Cryptocurrency prices");
        var sectionId = _stateService.Config.Sections[0].Id;
        _stateService.AddWidget(sectionId, new WidgetConfig
        {
            Title = "BTC Price",
            ChartType = ChartType.RadialBar,
            DataSource = new DataSourceConfig
            {
                Url = "https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids=bitcoin",
                YFieldPaths = new() { "bitcoin.usd" },
                RefreshIntervalSeconds = 60
            },
            ColumnSpan = 4
        });

        // Serialize
        var json = _service.SerializeConfig();

        // Deserialize
        var (config, error) = _service.DeserializeConfig(json);

        Assert.Null(error);
        Assert.NotNull(config);
        Assert.Equal("My Dashboard", config.Name);
        Assert.Single(config.Sections);
        Assert.Single(config.Sections[0].Widgets);
        Assert.Equal("BTC Price", config.Sections[0].Widgets[0].Title);
        Assert.Equal(ChartType.RadialBar, config.Sections[0].Widgets[0].ChartType);
    }

    [Fact]
    public void DeserializeConfig_EnumAsString_ParsedCorrectly()
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

        var (config, error) = _service.DeserializeConfig(json);

        Assert.Null(error);
        Assert.Equal(ChartType.Bar, config!.Sections[0].Widgets[0].ChartType);
    }
}
```

### Step 3: Create `DashboardStateServiceTests.cs`

```csharp
// tests/FlexibleMonitoringDashboard.Tests/Services/DashboardStateServiceTests.cs
using FlexibleMonitoringDashboard.Client.Models;
using FlexibleMonitoringDashboard.Client.Services;
using Xunit;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class DashboardStateServiceTests
{
    private readonly DashboardStateService _service = new();

    [Fact]
    public void AddSection_CreatesSection_MarksDirty()
    {
        var id = _service.AddSection("Test Section");

        Assert.NotEmpty(id);
        Assert.True(_service.IsDirty);
        Assert.Single(_service.Config.Sections);
        Assert.Equal("Test Section", _service.Config.Sections[0].Title);
    }

    [Fact]
    public void AddWidget_AddsToSection_MarksDirty()
    {
        var sectionId = _service.AddSection("Section");
        var widget = new WidgetConfig { Title = "Widget 1" };

        var widgetId = _service.AddWidget(sectionId, widget);

        Assert.NotEmpty(widgetId);
        Assert.Single(_service.Config.Sections[0].Widgets);
    }

    [Fact]
    public void RemoveWidget_RemovesFromSection()
    {
        var sectionId = _service.AddSection("Section");
        var widget = new WidgetConfig { Title = "To Delete" };
        var widgetId = _service.AddWidget(sectionId, widget);

        _service.RemoveWidget(sectionId, widgetId);

        Assert.Empty(_service.Config.Sections[0].Widgets);
    }

    [Fact]
    public void RemoveSection_RemovesWithAllWidgets()
    {
        var sectionId = _service.AddSection("Section");
        _service.AddWidget(sectionId, new WidgetConfig { Title = "W1" });
        _service.AddWidget(sectionId, new WidgetConfig { Title = "W2" });

        _service.RemoveSection(sectionId);

        Assert.Empty(_service.Config.Sections);
    }

    [Fact]
    public void LoadConfig_ReplacesEntireConfig_MarksClean()
    {
        _service.AddSection("Old");

        var newConfig = new DashboardConfig
        {
            Name = "Imported",
            Sections = new() { new SectionConfig { Title = "New" } }
        };
        _service.LoadConfig(newConfig);

        Assert.False(_service.IsDirty);
        Assert.Equal("Imported", _service.Config.Name);
        Assert.Single(_service.Config.Sections);
    }

    [Fact]
    public void MarkClean_ResetsIsDirty()
    {
        _service.AddSection("Section");
        Assert.True(_service.IsDirty);

        _service.MarkClean();
        Assert.False(_service.IsDirty);
    }

    [Fact]
    public void OnStateChanged_FiresOnMutations()
    {
        int callCount = 0;
        _service.OnStateChanged += () => callCount++;

        _service.AddSection("Section");
        _service.SetDashboardName("New Name");

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void MoveSection_SwapsOrder()
    {
        _service.AddSection("First");
        _service.AddSection("Second");

        var secondId = _service.Config.Sections[1].Id;
        _service.MoveSection(secondId, -1);

        Assert.Equal("Second", _service.Config.Sections[0].Title);
        Assert.Equal("First", _service.Config.Sections[1].Title);
    }

    [Fact]
    public void FindWidget_ReturnsCorrectSectionAndWidget()
    {
        var sectionId = _service.AddSection("Section");
        var widget = new WidgetConfig { Title = "Target" };
        _service.AddWidget(sectionId, widget);

        var (foundSectionId, foundWidget) = _service.FindWidget(widget.Id);

        Assert.Equal(sectionId, foundSectionId);
        Assert.Equal("Target", foundWidget?.Title);
    }

    [Fact]
    public void NewDashboard_ClearsEverything()
    {
        _service.AddSection("Section");
        _service.SetDashboardName("Named Dashboard");

        _service.NewDashboard();

        Assert.Empty(_service.Config.Sections);
        Assert.Equal("My Dashboard", _service.Config.Name);
        Assert.False(_service.IsDirty);
    }
}
```

---

## Verification

- [ ] `dotnet test` — all tests pass
- [ ] ChartTypeRecommender: correct types for time series, single value, categorical
- [ ] ConfigurationService: round-trip serialization preserves all data
- [ ] ConfigurationService: invalid JSON, empty strings, missing URLs all return errors
- [ ] DashboardStateService: CRUD operations, dirty tracking, event firing all work
- [ ] No test has external dependencies (except integration tests that need internet)
