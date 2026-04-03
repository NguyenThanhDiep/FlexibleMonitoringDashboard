# Task 01: Project Scaffolding & Package Installation

## Metadata
- **ID**: 01-project-scaffolding
- **Layer**: 0 (must be completed first)
- **Dependencies**: None
- **Blocks**: ALL other tasks
- **Estimated Scope**: Solution creation, NuGet packages, basic file configuration

---

## Files Created/Modified

| File | Action |
|------|--------|
| `FlexibleMonitoringDashboard.sln` | Created by template |
| `src/FlexibleMonitoringDashboard/` | Created by template (server project) |
| `src/FlexibleMonitoringDashboard.Client/` | Created by template (client project) |
| `tests/FlexibleMonitoringDashboard.Tests/` | Created manually |
| `src/FlexibleMonitoringDashboard/Program.cs` | Modified — add base services |
| `src/FlexibleMonitoringDashboard.Client/Program.cs` | Modified — add MudBlazor + ApexCharts |
| `src/FlexibleMonitoringDashboard.Client/_Imports.razor` | Modified — add usings |
| `src/FlexibleMonitoringDashboard/Components/App.razor` | Modified — add CSS/JS refs |
| `src/FlexibleMonitoringDashboard/Components/_Imports.razor` | Modified — add usings |

---

## Step-by-Step Implementation

### Step 1: Create Solution from Template

```powershell
# Navigate to your workspace root
cd d:\

# Create the Blazor Web App with WebAssembly interactivity
dotnet new blazor --name FlexibleMonitoringDashboard --interactivity WebAssembly --output FlexibleMonitoringDashboard --all-interactive

# Verify structure
cd FlexibleMonitoringDashboard
dir
# Should see:
#   FlexibleMonitoringDashboard.sln
#   FlexibleMonitoringDashboard/          (server project)
#   FlexibleMonitoringDashboard.Client/   (client project)
```

### Step 2: Restructure into `src/` Folder

```powershell
# Create src directory
mkdir src

# Move projects into src
Move-Item FlexibleMonitoringDashboard src/FlexibleMonitoringDashboard
Move-Item FlexibleMonitoringDashboard.Client src/FlexibleMonitoringDashboard.Client

# Update .sln file references (or recreate)
dotnet sln FlexibleMonitoringDashboard.sln remove FlexibleMonitoringDashboard/FlexibleMonitoringDashboard.csproj
dotnet sln FlexibleMonitoringDashboard.sln remove FlexibleMonitoringDashboard.Client/FlexibleMonitoringDashboard.Client.csproj
dotnet sln FlexibleMonitoringDashboard.sln add src/FlexibleMonitoringDashboard/FlexibleMonitoringDashboard.csproj
dotnet sln FlexibleMonitoringDashboard.sln add src/FlexibleMonitoringDashboard.Client/FlexibleMonitoringDashboard.Client.csproj

# Update project reference in server .csproj to point to new Client location
# In src/FlexibleMonitoringDashboard/FlexibleMonitoringDashboard.csproj,
# change the ProjectReference path from:
#   ..\FlexibleMonitoringDashboard.Client\FlexibleMonitoringDashboard.Client.csproj
# to:
#   ..\FlexibleMonitoringDashboard.Client\FlexibleMonitoringDashboard.Client.csproj
# (should still be relative — verify it's correct after the move)
```

> **Note**: If moving to `src/` causes issues with project references, it's acceptable to skip this step and keep the default flat structure. The solution still works the same way.

### Step 3: Install NuGet Packages on Client Project

```powershell
cd src/FlexibleMonitoringDashboard.Client

# MudBlazor — Material Design UI components
dotnet add package MudBlazor

# Blazor-ApexCharts — Chart rendering
dotnet add package Blazor-ApexCharts
```

### Step 4: Create Test Project

```powershell
cd ../../
mkdir tests
cd tests

dotnet new xunit --name FlexibleMonitoringDashboard.Tests
cd FlexibleMonitoringDashboard.Tests

# Add references
dotnet add reference ../../src/FlexibleMonitoringDashboard/FlexibleMonitoringDashboard.csproj
dotnet add reference ../../src/FlexibleMonitoringDashboard.Client/FlexibleMonitoringDashboard.Client.csproj

# Add test packages
dotnet add package Moq
dotnet add package Microsoft.AspNetCore.Mvc.Testing

# Add to solution
cd ../../
dotnet sln add tests/FlexibleMonitoringDashboard.Tests/FlexibleMonitoringDashboard.Tests.csproj
```

### Step 5: Configure Client `_Imports.razor`

Edit `src/FlexibleMonitoringDashboard.Client/_Imports.razor`:

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using MudBlazor
@using ApexCharts
@using FlexibleMonitoringDashboard.Client
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@using FlexibleMonitoringDashboard.Client.Components.Dashboard
@using FlexibleMonitoringDashboard.Client.Components.DataSource
@using FlexibleMonitoringDashboard.Client.Components.Correlation
@using FlexibleMonitoringDashboard.Client.Components.Configuration
@using FlexibleMonitoringDashboard.Client.Components.Threshold
```

### Step 6: Configure Client `Program.cs`

Edit `src/FlexibleMonitoringDashboard.Client/Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// MudBlazor
builder.Services.AddMudServices();

// ApexCharts
builder.Services.AddApexCharts();

// HttpClient for backend API calls
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Client services (will be registered as tasks are completed)
// builder.Services.AddScoped<DashboardStateService>();
// builder.Services.AddScoped<ConfigurationService>();
// builder.Services.AddScoped<ChartTypeRecommender>();
// builder.Services.AddScoped<DataProxyClient>();

await builder.Build().RunAsync();
```

### Step 7: Configure Server `_Imports.razor`

Edit `src/FlexibleMonitoringDashboard/Components/_Imports.razor` — add:

```razor
@using MudBlazor
```

### Step 8: Configure Server `App.razor`

Edit `src/FlexibleMonitoringDashboard/Components/App.razor` to include MudBlazor and ApexCharts CSS/JS:

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />

    <!-- MudBlazor CSS -->
    <link href="https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

    <!-- App CSS -->
    <link rel="stylesheet" href="FlexibleMonitoringDashboard.Client.styles.css" />
    <link rel="stylesheet" href="css/app.css" />

    <HeadOutlet @rendermode="InteractiveWebAssembly" />
</head>
<body>
    <Routes @rendermode="InteractiveWebAssembly" />

    <!-- MudBlazor JS -->
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>

    <!-- ApexCharts JS (loaded automatically by Blazor-ApexCharts) -->

    <!-- Custom JS interop -->
    <script src="js/interop.js"></script>

    <!-- Blazor framework -->
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

### Step 9: Configure Server `Program.cs`

Edit `src/FlexibleMonitoringDashboard/Program.cs`:

```csharp
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// MudBlazor (server-side prerender support)
builder.Services.AddMudServices();

// HttpClient for external API calls (will be configured in BE tasks)
builder.Services.AddHttpClient();

// CORS (for development)
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

// API endpoints (will be mapped in BE-04)
// app.MapDataProxyEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FlexibleMonitoringDashboard.Client._Imports).Assembly);

app.Run();
```

> **Note**: Add `using FlexibleMonitoringDashboard.Components;` at the top if needed for `App` reference.

### Step 10: Create Required Directories

```powershell
# From solution root
mkdir src/FlexibleMonitoringDashboard/Endpoints
mkdir src/FlexibleMonitoringDashboard/Services
mkdir src/FlexibleMonitoringDashboard/Models
mkdir src/FlexibleMonitoringDashboard.Client/Services
mkdir src/FlexibleMonitoringDashboard.Client/Models
mkdir src/FlexibleMonitoringDashboard.Client/Components/Dashboard
mkdir src/FlexibleMonitoringDashboard.Client/Components/DataSource
mkdir src/FlexibleMonitoringDashboard.Client/Components/Correlation
mkdir src/FlexibleMonitoringDashboard.Client/Components/Configuration
mkdir src/FlexibleMonitoringDashboard.Client/Components/Threshold
mkdir src/FlexibleMonitoringDashboard.Client/wwwroot/css
mkdir src/FlexibleMonitoringDashboard.Client/wwwroot/js
mkdir tests/FlexibleMonitoringDashboard.Tests/Services
mkdir tests/FlexibleMonitoringDashboard.Tests/Endpoints
mkdir docs
mkdir docs/ai-assistance
```

### Step 11: Create Placeholder JS Interop File

Create `src/FlexibleMonitoringDashboard.Client/wwwroot/js/interop.js`:

```javascript
// JS Interop helpers — implemented in FE-06
window.dashboardInterop = {};
```

### Step 12: Create Placeholder CSS File

Create `src/FlexibleMonitoringDashboard.Client/wwwroot/css/app.css`:

```css
/* Custom styles — implemented in UI-12 */
```

### Step 13: Verify Build

```powershell
cd <solution-root>
dotnet build
# Must succeed with zero errors
```

---

## Verification

- [ ] `dotnet build` succeeds with zero errors
- [ ] Solution has 3 projects: Server, Client, Tests
- [ ] MudBlazor and Blazor-ApexCharts NuGet packages installed on Client
- [ ] `dotnet run --project src/FlexibleMonitoringDashboard` starts and shows default Blazor page
- [ ] All required directories exist
