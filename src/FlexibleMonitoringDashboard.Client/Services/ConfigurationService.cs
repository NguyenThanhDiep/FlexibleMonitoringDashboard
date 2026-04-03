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
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        // Trigger browser download via JS interop
        await _jsRuntime.InvokeVoidAsync("dashboardInterop.downloadFile", fileName, base64, "application/json");
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
