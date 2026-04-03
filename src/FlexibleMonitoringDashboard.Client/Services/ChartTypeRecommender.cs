// src/FlexibleMonitoringDashboard.Client/Services/ChartTypeRecommender.cs
using FlexibleMonitoringDashboard.Client.Models;

namespace FlexibleMonitoringDashboard.Client.Services;

/// <summary>
/// Recommends chart types and field mappings based on detected JSON field structure.
/// Uses heuristics based on field types, counts, and naming conventions.
/// </summary>
public class ChartTypeRecommender
{
    /// <summary>
    /// Recommends a chart type given a server-suggested type string.
    /// Maps the server's suggestion to the client ChartType enum.
    /// </summary>
    public ChartType RecommendFromServerSuggestion(string? suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion))
            return ChartType.Bar;

        return suggestion.ToLowerInvariant() switch
        {
            "line" => ChartType.Line,
            "bar" => ChartType.Bar,
            "area" => ChartType.Area,
            "pie" => ChartType.Pie,
            "donut" => ChartType.Donut,
            "radialbar" or "gauge" => ChartType.RadialBar,
            "scatter" => ChartType.Scatter,
            _ => ChartType.Bar
        };
    }

    /// <summary>
    /// Recommends a chart type based on field metadata.
    /// This is a client-side heuristic that can override or supplement server suggestions.
    /// </summary>
    /// <param name="fields">List of detected fields with types and array info.</param>
    /// <returns>Recommended chart type.</returns>
    public ChartType RecommendFromFields(List<AnalyzedField> fields)
    {
        if (fields.Count == 0)
            return ChartType.Bar;

        var arrayFields = fields.Where(f => f.IsArray).ToList();
        var scalarFields = fields.Where(f => !f.IsArray).ToList();

        // Case 1: Time series data (array with time + number arrays)
        var hasTimeArray = arrayFields.Any(f => f.IsTimeField);
        var numericArrays = arrayFields.Where(f => f.FieldType == "Number").ToList();

        if (hasTimeArray && numericArrays.Count > 0)
            return ChartType.Line;

        // Case 2: Category + value arrays -> Bar
        var stringArrays = arrayFields.Where(f => f.FieldType == "String").ToList();
        if (stringArrays.Count > 0 && numericArrays.Count > 0)
            return ChartType.Bar;

        // Case 3: Single scalar number -> RadialBar (gauge)
        var numericScalars = scalarFields.Where(f => f.FieldType == "Number").ToList();
        if (numericScalars.Count == 1 && scalarFields.Count <= 2)
            return ChartType.RadialBar;

        // Case 4: Few categories with values -> Pie/Donut
        if (numericScalars.Count >= 2 && numericScalars.Count <= 5)
            return ChartType.Pie;

        // Case 5: Many numeric fields -> Bar
        if (numericScalars.Count > 5)
            return ChartType.Bar;

        // Default
        return ChartType.Bar;
    }

    /// <summary>
    /// Builds default field mappings based on the server's analysis response.
    /// </summary>
    public List<FieldMapping> BuildDefaultMappings(
        string? suggestedXField,
        List<string> suggestedYFields,
        List<AnalyzedField> allFields)
    {
        var mappings = new List<FieldMapping>();

        // X-axis mapping
        if (!string.IsNullOrWhiteSpace(suggestedXField))
        {
            var xField = allFields.FirstOrDefault(f => f.Path == suggestedXField);
            mappings.Add(new FieldMapping
            {
                FieldPath = suggestedXField,
                Label = xField?.Name ?? GetLastSegment(suggestedXField),
                Role = FieldRole.XAxis
            });
        }

        // Y-axis mappings
        foreach (var yPath in suggestedYFields)
        {
            var yField = allFields.FirstOrDefault(f => f.Path == yPath);
            mappings.Add(new FieldMapping
            {
                FieldPath = yPath,
                Label = yField?.Name ?? GetLastSegment(yPath),
                Role = FieldRole.YAxis
            });
        }

        return mappings;
    }

    /// <summary>
    /// Returns all available chart types with descriptions.
    /// </summary>
    public static List<ChartTypeOption> GetAvailableChartTypes()
    {
        return new List<ChartTypeOption>
        {
            new(ChartType.Line, "Line Chart", "Best for time series and trends", "mdi-chart-line"),
            new(ChartType.Bar, "Bar Chart", "Best for comparing categories", "mdi-chart-bar"),
            new(ChartType.Area, "Area Chart", "Line chart with filled area", "mdi-chart-areaspline"),
            new(ChartType.Pie, "Pie Chart", "Best for proportions (few items)", "mdi-chart-pie"),
            new(ChartType.Donut, "Donut Chart", "Proportions with center space", "mdi-chart-donut"),
            new(ChartType.RadialBar, "Gauge", "Best for single KPI values", "mdi-gauge"),
            new(ChartType.Scatter, "Scatter Plot", "Best for correlation analysis", "mdi-chart-scatter-plot"),
        };
    }

    private static string GetLastSegment(string path)
    {
        var lastDot = path.LastIndexOf('.');
        return lastDot >= 0 ? path[(lastDot + 1)..] : path;
    }
}

/// <summary>
/// Simplified field info for client-side recommendation.
/// Mapped from the server's AnalyzeResponse.Fields.
/// </summary>
public class AnalyzedField
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public string FieldType { get; set; } = ""; // "Number", "String", "DateTime", "Boolean"
    public bool IsArray { get; set; }
    public int ArrayLength { get; set; }
    public List<string> SampleValues { get; set; } = new();

    /// <summary>
    /// Heuristic: is this a time/date field?
    /// </summary>
    public bool IsTimeField =>
        FieldType == "DateTime" ||
        Name.Contains("time", StringComparison.OrdinalIgnoreCase) ||
        Name.Contains("date", StringComparison.OrdinalIgnoreCase) ||
        Name.Contains("timestamp", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Heuristic: is this a numeric field?
    /// </summary>
    public bool IsNumeric => FieldType == "Number";
}

/// <summary>
/// Description of a chart type option for the UI selector.
/// </summary>
public record ChartTypeOption(
    ChartType Type,
    string DisplayName,
    string Description,
    string Icon
);
