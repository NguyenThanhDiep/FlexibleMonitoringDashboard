// src/FlexibleMonitoringDashboard/Services/JsonSchemaAnalyzer.cs
using System.Globalization;
using System.Text.Json;
using FlexibleMonitoringDashboard.Models;

namespace FlexibleMonitoringDashboard.Services;

/// <summary>
/// Analyzes JSON API responses to detect fields, types, and suggest chart configurations.
/// </summary>
public class JsonSchemaAnalyzer
{
    private const int MaxSampleValues = 5;
    private const int MaxDepth = 10;
    private const int MaxFields = 100;

    /// <summary>
    /// Analyzes a JSON element and returns detected fields.
    /// </summary>
    public List<JsonFieldInfo> AnalyzeFields(JsonElement root)
    {
        var fields = new List<JsonFieldInfo>();
        AnalyzeElement(root, "", fields, 0);
        return fields.Take(MaxFields).ToList();
    }

    /// <summary>
    /// Suggests chart type and axis fields based on detected fields.
    /// </summary>
    public (string ChartType, string? XField, List<string> YFields) SuggestChart(List<JsonFieldInfo> fields)
    {
        // Find array fields (data series)
        var arrayFields = fields.Where(f => f.IsArray).ToList();
        var scalarFields = fields.Where(f => !f.IsArray).ToList();

        // Case 1: Has arrays of data → time series or categorical chart
        if (arrayFields.Count >= 2)
        {
            // Look for a time/date array field for X-axis
            var timeField = arrayFields.FirstOrDefault(f =>
                f.FieldType == JsonFieldType.DateTime ||
                f.Name.Contains("time", StringComparison.OrdinalIgnoreCase) ||
                f.Name.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                f.Name.Contains("timestamp", StringComparison.OrdinalIgnoreCase));

            // Look for a string array field for X-axis (categorical)
            var categoryField = timeField ?? arrayFields.FirstOrDefault(f =>
                f.FieldType == JsonFieldType.String);

            // Numeric array fields for Y-axis
            var numericFields = arrayFields
                .Where(f => f.FieldType == JsonFieldType.Number)
                .ToList();

            if (timeField != null && numericFields.Count > 0)
            {
                return ("Line", timeField.Path, numericFields.Select(f => f.Path).ToList());
            }

            if (categoryField != null && numericFields.Count > 0)
            {
                return ("Bar", categoryField.Path, numericFields.Select(f => f.Path).Take(3).ToList());
            }
        }

        // Case 2: Array of objects (e.g., [{name: "a", value: 1}, ...])
        var objectArrayField = fields.FirstOrDefault(f => f.IsArray && f.FieldType == JsonFieldType.Object);
        if (objectArrayField != null)
        {
            // Look for child fields of this array
            List<JsonFieldInfo> childFields;
            if (string.IsNullOrEmpty(objectArrayField.Path))
            {
                // Root-level array of objects: children have simple paths with no dot
                childFields = fields.Where(f => !string.IsNullOrEmpty(f.Path) && !f.Path.Contains('.')).ToList();
            }
            else
            {
                var childPrefix = objectArrayField.Path + ".";
                childFields = fields.Where(f => f.Path.StartsWith(childPrefix)).ToList();
            }

            var childCategory = childFields.FirstOrDefault(f =>
                f.FieldType == JsonFieldType.String || f.FieldType == JsonFieldType.DateTime);
            var childNumeric = childFields.Where(f => f.FieldType == JsonFieldType.Number).ToList();

            if (childCategory != null && childNumeric.Count > 0)
            {
                var chartType = childCategory.FieldType == JsonFieldType.DateTime ? "Line" : "Bar";
                return (chartType, childCategory.Path, childNumeric.Select(f => f.Path).Take(3).ToList());
            }
        }

        // Case 3: Flat object with only numeric values → Gauge or single KPI
        var scalarNumerics = scalarFields.Where(f => f.FieldType == JsonFieldType.Number).ToList();
        if (scalarNumerics.Count == 1)
        {
            return ("RadialBar", null, new List<string> { scalarNumerics[0].Path });
        }

        if (scalarNumerics.Count >= 2 && scalarNumerics.Count <= 6)
        {
            return ("Bar", null, scalarNumerics.Select(f => f.Path).ToList());
        }

        if (scalarNumerics.Count > 6)
        {
            return ("Line", null, scalarNumerics.Select(f => f.Path).Take(10).ToList());
        }

        // Fallback
        return ("Bar", null, fields.Where(f => f.FieldType == JsonFieldType.Number).Select(f => f.Path).Take(3).ToList());
    }

    /// <summary>
    /// Recursively walks JSON structure to detect fields.
    /// </summary>
    private void AnalyzeElement(JsonElement element, string currentPath, List<JsonFieldInfo> fields, int depth)
    {
        if (depth > MaxDepth || fields.Count >= MaxFields) return;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var path = string.IsNullOrEmpty(currentPath)
                        ? property.Name
                        : $"{currentPath}.{property.Name}";

                    AnalyzeElement(property.Value, path, fields, depth + 1);
                }
                break;

            case JsonValueKind.Array:
                AnalyzeArray(element, currentPath, fields, depth);
                break;

            case JsonValueKind.Number:
                fields.Add(new JsonFieldInfo
                {
                    Path = currentPath,
                    Name = GetFieldName(currentPath),
                    FieldType = JsonFieldType.Number,
                    IsArray = false,
                    SampleValues = new List<string> { element.GetRawText() }
                });
                break;

            case JsonValueKind.String:
                var strValue = element.GetString() ?? "";
                var fieldType = DetectStringType(strValue);
                fields.Add(new JsonFieldInfo
                {
                    Path = currentPath,
                    Name = GetFieldName(currentPath),
                    FieldType = fieldType,
                    IsArray = false,
                    SampleValues = new List<string> { strValue }
                });
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                fields.Add(new JsonFieldInfo
                {
                    Path = currentPath,
                    Name = GetFieldName(currentPath),
                    FieldType = JsonFieldType.Boolean,
                    IsArray = false,
                    SampleValues = new List<string> { element.GetRawText() }
                });
                break;
        }
    }

    /// <summary>
    /// Analyzes array elements to determine field info.
    /// </summary>
    private void AnalyzeArray(JsonElement array, string currentPath, List<JsonFieldInfo> fields, int depth)
    {
        var arrayLength = array.GetArrayLength();
        if (arrayLength == 0) return;

        var firstElement = array[0];

        switch (firstElement.ValueKind)
        {
            case JsonValueKind.Number:
                fields.Add(new JsonFieldInfo
                {
                    Path = currentPath,
                    Name = GetFieldName(currentPath),
                    FieldType = JsonFieldType.Number,
                    IsArray = true,
                    ArrayLength = arrayLength,
                    SampleValues = array.EnumerateArray()
                        .Take(MaxSampleValues)
                        .Select(e => e.GetRawText())
                        .ToList()
                });
                break;

            case JsonValueKind.String:
                var samples = array.EnumerateArray()
                    .Take(MaxSampleValues)
                    .Select(e => e.GetString() ?? "")
                    .ToList();
                var type = DetectStringType(samples.FirstOrDefault() ?? "");
                fields.Add(new JsonFieldInfo
                {
                    Path = currentPath,
                    Name = GetFieldName(currentPath),
                    FieldType = type,
                    IsArray = true,
                    ArrayLength = arrayLength,
                    SampleValues = samples
                });
                break;

            case JsonValueKind.Object:
                // Register the array itself
                fields.Add(new JsonFieldInfo
                {
                    Path = currentPath,
                    Name = GetFieldName(currentPath),
                    FieldType = JsonFieldType.Object,
                    IsArray = true,
                    ArrayLength = arrayLength
                });

                // Analyze the first object's fields as child fields
                foreach (var property in firstElement.EnumerateObject())
                {
                    var childPath = string.IsNullOrEmpty(currentPath)
                        ? property.Name
                        : $"{currentPath}.{property.Name}";

                    // Collect sample values from all array elements for this property
                    var childSamples = array.EnumerateArray()
                        .Take(MaxSampleValues)
                        .Where(e => e.TryGetProperty(property.Name, out _))
                        .Select(e => e.GetProperty(property.Name).GetRawText())
                        .ToList();

                    var childType = DetectElementType(property.Value);

                    fields.Add(new JsonFieldInfo
                    {
                        Path = childPath,
                        Name = property.Name,
                        FieldType = childType,
                        IsArray = true,
                        ArrayLength = arrayLength,
                        SampleValues = childSamples
                    });
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                fields.Add(new JsonFieldInfo
                {
                    Path = currentPath,
                    Name = GetFieldName(currentPath),
                    FieldType = JsonFieldType.Boolean,
                    IsArray = true,
                    ArrayLength = arrayLength,
                    SampleValues = array.EnumerateArray()
                        .Take(MaxSampleValues)
                        .Select(e => e.GetRawText())
                        .ToList()
                });
                break;
        }
    }

    /// <summary>
    /// Detects whether a string value is actually a DateTime.
    /// </summary>
    private static JsonFieldType DetectStringType(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return JsonFieldType.DateTime;

        // ISO 8601 check
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return JsonFieldType.DateTime;

        return JsonFieldType.String;
    }

    /// <summary>
    /// Detects the type of a JsonElement.
    /// </summary>
    private static JsonFieldType DetectElementType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => JsonFieldType.Number,
            JsonValueKind.String => DetectStringType(element.GetString() ?? ""),
            JsonValueKind.True or JsonValueKind.False => JsonFieldType.Boolean,
            JsonValueKind.Object => JsonFieldType.Object,
            _ => JsonFieldType.Unknown
        };
    }

    /// <summary>
    /// Extracts the last segment from a dot-separated path.
    /// </summary>
    private static string GetFieldName(string path)
    {
        var lastDot = path.LastIndexOf('.');
        return lastDot >= 0 ? path[(lastDot + 1)..] : path;
    }
}
