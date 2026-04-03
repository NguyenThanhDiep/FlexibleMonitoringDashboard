# Task FE-03: Configuration Service (Export/Import)

## Metadata
- **ID**: FE-03-configuration-service
- **Layer**: 2
- **Dependencies**: `FE-01`
- **Blocks**: `UI-09`, `TEST-02`
- **Parallel With**: FE-02, BE-04

---

## Files Created

| File | Path |
|------|------|
| `ConfigurationService.cs` | `src/FlexibleMonitoringDashboard.Client/Services/ConfigurationService.cs` |

---

## Step-by-Step Implementation

### Step 1: Create `ConfigurationService.cs`

This service handles serialization/deserialization of `DashboardConfig` to JSON, and provides methods to trigger file download (export) and parse uploaded files (import).

```csharp
// src/FlexibleMonitoringDashboard.Client/Services/ConfigurationService.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using FlexibleMonitoringDashboard.Client.Models;
using Microsoft.JSInterop;

namespace FlexibleMonitoringDashboard.Client.Services;

/// <summary>
/// Handles dashboard configuration export (JSON download) and import (JSON upload).
/// </summary>
public class ConfigurationService
{
    private readonly IJSRuntime _jsRuntime;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ConfigurationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // ───────── Export ─────────

    /// <summary>
    /// Serializes the dashboard config to JSON and triggers a browser file download.
    /// </summary>
    public async Task ExportAsync(DashboardConfig config)
    {
        // Set export timestamp
        config.ExportedAt = DateTime.UtcNow;

        var json = SerializeConfig(config);
        var fileName = SanitizeFileName(config.Name) + ".json";

        // Trigger browser download via JS interop
        await _jsRuntime.InvokeVoidAsync("dashboardInterop.downloadFile", fileName, json);
    }

    /// <summary>
    /// Serializes config to a JSON string (without triggering download).
    /// Useful for clipboard copy or preview.
    /// </summary>
    public string SerializeConfig(DashboardConfig config)
    {
        return JsonSerializer.Serialize(config, JsonOptions);
    }

    // ───────── Import ─────────

    /// <summary>
    /// Deserializes a JSON string into a DashboardConfig.
    /// Returns (config, null) on success, or (null, errorMessage) on failure.
    /// </summary>
    public (DashboardConfig? Config, string? Error) ImportFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (null, "The file is empty.");

        try
        {
            var config = JsonSerializer.Deserialize<DashboardConfig>(json, JsonOptions);

            if (config == null)
                return (null, "Failed to parse configuration file.");

            // Validate the config
            var validationError = ValidateConfig(config);
            if (validationError != null)
                return (null, validationError);

            // Ensure all IDs are unique (regenerate if needed)
            EnsureUniqueIds(config);

            return (config, null);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid JSON format: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads a browser-uploaded file stream and imports it.
    /// </summary>
    public async Task<(DashboardConfig? Config, string? Error)> ImportFromStreamAsync(Stream stream)
    {
        const int maxSizeBytes = 5 * 1024 * 1024; // 5 MB max config file

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        if (json.Length > maxSizeBytes)
            return (null, $"Configuration file is too large (max {maxSizeBytes / 1024 / 1024} MB).");

        return ImportFromJson(json);
    }

    // ───────── Validation ─────────

    /// <summary>
    /// Validates the structure of a deserialized config.
    /// Returns null if valid, error message if not.
    /// </summary>
    private static string? ValidateConfig(DashboardConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Version))
            return "Configuration is missing a version number.";

        if (config.Sections == null)
            return "Configuration is missing sections array.";

        foreach (var section in config.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Title))
                return $"Section '{section.Id}' is missing a title.";

            if (section.Widgets == null)
                return $"Section '{section.Title}' is missing widgets array.";

            foreach (var widget in section.Widgets)
            {
                if (widget.DataSource == null || string.IsNullOrWhiteSpace(widget.DataSource.Url))
                {
                    // Correlation widgets might have URL on secondary source
                    if (widget.Correlation?.SecondaryDataSource?.Url == null)
                        return $"Widget '{widget.Title}' is missing a data source URL.";
                }

                if (!Uri.TryCreate(widget.DataSource?.Url ?? "", UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    if (widget.DataSource?.Url != null)
                        return $"Widget '{widget.Title}' has an invalid URL: {widget.DataSource.Url}";
                }
            }
        }

        return null; // All good
    }

    /// <summary>
    /// Ensures all IDs within the config are unique.
    /// Regenerates any duplicates.
    /// </summary>
    private static void EnsureUniqueIds(DashboardConfig config)
    {
        var usedIds = new HashSet<string>();

        foreach (var section in config.Sections)
        {
            if (string.IsNullOrEmpty(section.Id) || !usedIds.Add(section.Id))
                section.Id = Guid.NewGuid().ToString("N");

            foreach (var widget in section.Widgets)
            {
                if (string.IsNullOrEmpty(widget.Id) || !usedIds.Add(widget.Id))
                    widget.Id = Guid.NewGuid().ToString("N");
            }
        }
    }

    /// <summary>
    /// Sanitizes a string for use as a file name.
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "dashboard-config";

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name
            .Where(c => !invalid.Contains(c))
            .ToArray())
            .Replace(' ', '-')
            .ToLowerInvariant();

        return string.IsNullOrWhiteSpace(sanitized) ? "dashboard-config" : sanitized;
    }
}
```

### Step 2: Register in Client `Program.cs`

Add to `src/FlexibleMonitoringDashboard.Client/Program.cs`:

```csharp
builder.Services.AddScoped<ConfigurationService>();
```

---

## JS Interop Dependency

This service requires the `dashboardInterop.downloadFile` function defined in `FE-06-js-interop.md`. The function should:

```javascript
// Expected signature (implemented in FE-06):
window.dashboardInterop.downloadFile = function(fileName, content) {
    // Create blob, URL, anchor click, cleanup
};
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] `SerializeConfig()` produces valid pretty-printed JSON with camelCase keys
- [ ] `ImportFromJson(json)` round-trips: serialize → deserialize produces identical config
- [ ] Invalid JSON returns meaningful error message
- [ ] Missing URL in widget returns validation error
- [ ] Duplicate IDs are regenerated during import
- [ ] File size limit (5 MB) is enforced
- [ ] `SanitizeFileName("My Dashboard!")` → `"my-dashboard"`
- [ ] Enum values serialize as strings (e.g., `"line"` not `0`)
