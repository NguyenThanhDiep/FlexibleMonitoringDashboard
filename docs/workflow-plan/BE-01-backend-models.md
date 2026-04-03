# Task BE-01: Backend Models

## Metadata
- **ID**: BE-01-backend-models
- **Layer**: 1
- **Dependencies**: `01-project-scaffolding`
- **Blocks**: `BE-04`
- **Parallel With**: BE-02, BE-03, FE-01, FE-04, FE-05, FE-06, UI-12, DOC-*

---

## Files Created

| File | Path |
|------|------|
| `ProxyRequest.cs` | `src/FlexibleMonitoringDashboard/Models/ProxyRequest.cs` |
| `ProxyResponse.cs` | `src/FlexibleMonitoringDashboard/Models/ProxyResponse.cs` |
| `JsonFieldInfo.cs` | `src/FlexibleMonitoringDashboard/Models/JsonFieldInfo.cs` |
| `AnalyzeResponse.cs` | `src/FlexibleMonitoringDashboard/Models/AnalyzeResponse.cs` |

---

## Step-by-Step Implementation

### Step 1: Create `ProxyRequest.cs`

This model represents the incoming request to fetch data from an external API.

```csharp
// src/FlexibleMonitoringDashboard/Models/ProxyRequest.cs
namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Request payload for the proxy fetch endpoint.
/// </summary>
public class ProxyRequest
{
    /// <summary>
    /// The public JSON API URL to fetch data from.
    /// Must be http:// or https:// scheme.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom headers to include in the request (e.g., API keys).
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional timeout in seconds. Default: 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
```

### Step 2: Create `ProxyResponse.cs`

This model wraps the response from the external API.

```csharp
// src/FlexibleMonitoringDashboard/Models/ProxyResponse.cs
using System.Text.Json;

namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Response payload from the proxy fetch endpoint.
/// </summary>
public class ProxyResponse
{
    /// <summary>
    /// Whether the external API call was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The raw JSON data returned by the external API.
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Error message if the call failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// HTTP status code from the external API.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }
}
```

### Step 3: Create `JsonFieldInfo.cs`

This model describes a detected field in the JSON response.

```csharp
// src/FlexibleMonitoringDashboard/Models/JsonFieldInfo.cs
namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Describes a field detected in a JSON API response.
/// </summary>
public class JsonFieldInfo
{
    /// <summary>
    /// Dot-separated path to the field (e.g., "hourly.temperature_2m").
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Simple field name (e.g., "temperature_2m").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detected data type of the field values.
    /// </summary>
    public JsonFieldType FieldType { get; set; }

    /// <summary>
    /// Whether this field contains an array of values.
    /// </summary>
    public bool IsArray { get; set; }

    /// <summary>
    /// Number of values in the array (if IsArray is true).
    /// </summary>
    public int ArrayLength { get; set; }

    /// <summary>
    /// Up to 5 sample values for preview.
    /// </summary>
    public List<string> SampleValues { get; set; } = new();
}

/// <summary>
/// Detected type of a JSON field.
/// </summary>
public enum JsonFieldType
{
    String,
    Number,
    Boolean,
    DateTime,
    Object,
    Unknown
}
```

### Step 4: Create `AnalyzeResponse.cs`

This model combines the proxy response with schema analysis.

```csharp
// src/FlexibleMonitoringDashboard/Models/AnalyzeResponse.cs
using System.Text.Json;

namespace FlexibleMonitoringDashboard.Models;

/// <summary>
/// Response from the analyze endpoint — includes raw data + detected fields.
/// </summary>
public class AnalyzeResponse
{
    /// <summary>
    /// Whether the external API call and analysis were successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The raw JSON data from the API.
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// List of detected fields in the JSON response.
    /// </summary>
    public List<JsonFieldInfo> Fields { get; set; } = new();

    /// <summary>
    /// Suggested chart type based on the detected fields.
    /// </summary>
    public string? SuggestedChartType { get; set; }

    /// <summary>
    /// Suggested X-axis field path.
    /// </summary>
    public string? SuggestedXField { get; set; }

    /// <summary>
    /// Suggested Y-axis field path(s).
    /// </summary>
    public List<string> SuggestedYFields { get; set; } = new();
}
```

---

## Verification

- [ ] All 4 files compile without errors
- [ ] `dotnet build` succeeds from solution root
- [ ] All models are in the `FlexibleMonitoringDashboard.Models` namespace
- [ ] `JsonFieldType` enum covers all needed types
- [ ] `ProxyRequest` has URL validation-ready properties
- [ ] `ProxyResponse.Data` uses `JsonElement?` (not string) to preserve JSON structure
