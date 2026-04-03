// tests/FlexibleMonitoringDashboard.Tests/Services/ChartTypeRecommenderTests.cs
using FlexibleMonitoringDashboard.Client.Models;
using FlexibleMonitoringDashboard.Client.Services;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class ChartTypeRecommenderTests
{
    private readonly ChartTypeRecommender _recommender = new();

    [Fact]
    public void RecommendFromServerSuggestion_Line_ReturnsLine()
    {
        Assert.Equal(ChartType.Line, _recommender.RecommendFromServerSuggestion("Line"));
    }

    [Fact]
    public void RecommendFromServerSuggestion_CaseInsensitive_ReturnsCorrectType()
    {
        Assert.Equal(ChartType.Bar, _recommender.RecommendFromServerSuggestion("BAR"));
        Assert.Equal(ChartType.Area, _recommender.RecommendFromServerSuggestion("AREA"));
        Assert.Equal(ChartType.Pie, _recommender.RecommendFromServerSuggestion("pie"));
    }

    [Fact]
    public void RecommendFromServerSuggestion_Null_ReturnsFallback()
    {
        Assert.Equal(ChartType.Bar, _recommender.RecommendFromServerSuggestion(null));
    }

    [Fact]
    public void RecommendFromServerSuggestion_Unknown_ReturnsFallback()
    {
        Assert.Equal(ChartType.Bar, _recommender.RecommendFromServerSuggestion("unknown_type"));
    }

    [Fact]
    public void RecommendFromServerSuggestion_RadialBar_ReturnsRadialBar()
    {
        Assert.Equal(ChartType.RadialBar, _recommender.RecommendFromServerSuggestion("radialbar"));
    }

    [Fact]
    public void RecommendFromServerSuggestion_Gauge_ReturnsRadialBar()
    {
        Assert.Equal(ChartType.RadialBar, _recommender.RecommendFromServerSuggestion("gauge"));
    }

    [Fact]
    public void RecommendFromFields_EmptyList_ReturnsBar()
    {
        var result = _recommender.RecommendFromFields(new List<AnalyzedField>());
        Assert.Equal(ChartType.Bar, result);
    }

    [Fact]
    public void RecommendFromFields_TimeSeriesArrays_ReturnsLine()
    {
        var fields = new List<AnalyzedField>
        {
            new() { Path = "time", Name = "time", FieldType = "DateTime", IsArray = true },
            new() { Path = "temperature", Name = "temperature", FieldType = "Number", IsArray = true }
        };

        Assert.Equal(ChartType.Line, _recommender.RecommendFromFields(fields));
    }

    [Fact]
    public void RecommendFromFields_SingleNumericScalar_ReturnsRadialBar()
    {
        var fields = new List<AnalyzedField>
        {
            new() { Path = "bitcoin.usd", Name = "usd", FieldType = "Number", IsArray = false }
        };

        Assert.Equal(ChartType.RadialBar, _recommender.RecommendFromFields(fields));
    }

    [Fact]
    public void RecommendFromFields_CategoricalStringArrayAndNumericArray_ReturnsBar()
    {
        var fields = new List<AnalyzedField>
        {
            new() { Path = "categories", Name = "categories", FieldType = "String", IsArray = true },
            new() { Path = "values", Name = "values", FieldType = "Number", IsArray = true }
        };

        Assert.Equal(ChartType.Bar, _recommender.RecommendFromFields(fields));
    }

    [Fact]
    public void RecommendFromFields_FewNumericScalars_ReturnsPie()
    {
        var fields = new List<AnalyzedField>
        {
            new() { Path = "a", Name = "a", FieldType = "Number", IsArray = false },
            new() { Path = "b", Name = "b", FieldType = "Number", IsArray = false },
            new() { Path = "c", Name = "c", FieldType = "Number", IsArray = false }
        };

        Assert.Equal(ChartType.Pie, _recommender.RecommendFromFields(fields));
    }

    [Fact]
    public void BuildDefaultMappings_CreatesXAndYMappings()
    {
        var fields = new List<AnalyzedField>
        {
            new() { Path = "time", Name = "time", FieldType = "DateTime" },
            new() { Path = "temp", Name = "temp", FieldType = "Number" }
        };

        var mappings = _recommender.BuildDefaultMappings("time", new List<string> { "temp" }, fields);

        Assert.Equal(2, mappings.Count);
        Assert.Contains(mappings, m => m.Role == FieldRole.XAxis && m.FieldPath == "time");
        Assert.Contains(mappings, m => m.Role == FieldRole.YAxis && m.FieldPath == "temp");
    }

    [Fact]
    public void BuildDefaultMappings_NullXField_OnlyYMappingsCreated()
    {
        var mappings = _recommender.BuildDefaultMappings(
            null,
            new List<string> { "bitcoin.usd" },
            new List<AnalyzedField>());

        Assert.Single(mappings);
        Assert.Equal(FieldRole.YAxis, mappings[0].Role);
    }

    [Fact]
    public void BuildDefaultMappings_UsesFieldNameAsLabel()
    {
        var fields = new List<AnalyzedField>
        {
            new() { Path = "hourly.temperature_2m", Name = "temperature_2m", FieldType = "Number" }
        };

        var mappings = _recommender.BuildDefaultMappings(null, new List<string> { "hourly.temperature_2m" }, fields);

        Assert.Single(mappings);
        Assert.Equal("temperature_2m", mappings[0].Label);
    }

    [Fact]
    public void GetAvailableChartTypes_ReturnsOneEntryPerChartType()
    {
        var types = ChartTypeRecommender.GetAvailableChartTypes();
        var enumValues = Enum.GetValues<ChartType>();

        Assert.Equal(enumValues.Length, types.Count);
    }
}
