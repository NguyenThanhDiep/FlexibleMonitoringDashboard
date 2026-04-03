# Task BE-04: Proxy Endpoints & Server Wiring

## Metadata
- **ID**: BE-04-proxy-endpoints
- **Layer**: 2
- **Dependencies**: `BE-01`, `BE-02`, `BE-03`
- **Blocks**: `TEST-01`
- **Parallel With**: FE-02, FE-03

---

## Files Created/Modified

| File | Action |
|------|--------|
| `DataProxyEndpoints.cs` | **Create** at `src/FlexibleMonitoringDashboard/Endpoints/DataProxyEndpoints.cs` |
| `Program.cs` | **Modify** at `src/FlexibleMonitoringDashboard/Program.cs` — register services + map endpoints |

---

## Step-by-Step Implementation

### Step 1: Create `DataProxyEndpoints.cs`

```csharp
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
```

### Step 2: Update Server `Program.cs`

Modify `src/FlexibleMonitoringDashboard/Program.cs` to register services and map endpoints.

**Add these lines in the services section** (after `builder.Services.AddHttpClient();`):

```csharp
// Register external API services
builder.Services.AddSingleton<JsonSchemaAnalyzer>();
builder.Services.AddScoped<ExternalApiService>();
```

**Add the usings at the top**:

```csharp
using FlexibleMonitoringDashboard.Endpoints;
using FlexibleMonitoringDashboard.Services;
```

**Uncomment/add the endpoint mapping** (before `app.MapRazorComponents<...>()`):

```csharp
// API endpoints
app.MapDataProxyEndpoints();
```

### Step 3: Final `Program.cs` Should Look Like

```csharp
using FlexibleMonitoringDashboard.Components;
using FlexibleMonitoringDashboard.Endpoints;
using FlexibleMonitoringDashboard.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// MudBlazor
builder.Services.AddMudServices();

// HttpClient for external API calls
builder.Services.AddHttpClient();

// Application services
builder.Services.AddSingleton<JsonSchemaAnalyzer>();
builder.Services.AddScoped<ExternalApiService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseCors();

// API endpoints
app.MapDataProxyEndpoints();

// Blazor SPA
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FlexibleMonitoringDashboard.Client._Imports).Assembly);

app.Run();
```

---

## API Contract

### `POST /api/proxy/fetch`

**Request:**
```json
{
  "url": "https://api.coingecko.com/api/v3/simple/price?vs_currencies=usd&ids=bitcoin",
  "headers": { "X-Custom": "value" },
  "timeoutSeconds": 30
}
```

**Response (success):**
```json
{
  "success": true,
  "data": { "bitcoin": { "usd": 50000 } },
  "statusCode": 200,
  "responseTimeMs": 245,
  "error": null
}
```

**Response (error):**
```json
{
  "success": false,
  "data": null,
  "statusCode": 0,
  "responseTimeMs": 0,
  "error": "URLs pointing to private/internal networks are not allowed."
}
```

### `POST /api/proxy/analyze`

**Request:** Same as fetch.

**Response:**
```json
{
  "success": true,
  "data": { "hourly": { "time": ["..."], "temperature_2m": [20.1, 21.5] } },
  "fields": [
    { "path": "hourly.time", "name": "time", "fieldType": "DateTime", "isArray": true, "arrayLength": 168, "sampleValues": ["2024-01-01T00:00", "..."] },
    { "path": "hourly.temperature_2m", "name": "temperature_2m", "fieldType": "Number", "isArray": true, "arrayLength": 168, "sampleValues": ["20.1", "21.5", "..."] }
  ],
  "suggestedChartType": "Line",
  "suggestedXField": "hourly.time",
  "suggestedYFields": ["hourly.temperature_2m"]
}
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] `POST /api/proxy/fetch` with a valid public URL returns JSON data
- [ ] `POST /api/proxy/fetch` with `http://localhost` returns SSRF error
- [ ] `POST /api/proxy/analyze` returns fields + chart suggestions
- [ ] Empty URL returns 400 Bad Request
- [ ] Server `Program.cs` compiles with all services registered
