# Master Plan: Flexible Monitoring Dashboard Web Application

## Product Vision

A real-time monitoring dashboard SPA that aggregates and visualizes data from any public JSON API. Users paste URLs, the system auto-detects fields, suggests chart types, and renders instantly. Supports cross-source correlation (overlay two APIs in one chart), section-based layout with custom headers, and JSON config export/import for sharing dashboards.

---

## Tech Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| Framework | .NET 8 | 8.0 | Runtime + SDK |
| Project Type | Blazor Web App | Interactive WASM | SPA with server-side API host |
| UI Library | MudBlazor | 8.x (supports .NET 8) | Material Design components |
| Charts | Blazor-ApexCharts | 6.x | Interactive chart rendering |
| Backend | ASP.NET Core Minimal APIs | 8.0 | Proxy endpoints for external APIs |
| Testing | xUnit + Moq | Latest | Unit + integration tests |
| JSON | System.Text.Json | Built-in | Serialization + schema analysis |

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                     Browser (WASM)                     │
│                                                        │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Dashboard    │  │ Chart Widget │  │ Correlation  │ │
│  │ Container    │  │ (ApexCharts) │  │ Panel        │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │
│         │                 │                  │         │
│  ┌──────┴─────────────────┴──────────────────┴──────┐ │
│  │           Client Services Layer                    │ │
│  │  DashboardStateService | ConfigurationService     │ │
│  │  ChartTypeRecommender  | DataProxyClient          │ │
│  └──────────────────────┬────────────────────────────┘ │
│                         │ HTTP                          │
└─────────────────────────┼──────────────────────────────┘
                          │
┌─────────────────────────┼──────────────────────────────┐
│               ASP.NET Core Server                       │
│                         │                               │
│  ┌──────────────────────┴─────────────────────────────┐│
│  │              Minimal API Endpoints                  ││
│  │  POST /api/proxy/fetch    POST /api/proxy/analyze  ││
│  └──────────────────────┬─────────────────────────────┘│
│                         │                               │
│  ┌──────────────────────┴─────────────────────────────┐│
│  │              Server Services                        ││
│  │  ExternalApiService  |  JsonSchemaAnalyzer          ││
│  └──────────────────────┬─────────────────────────────┘│
│                         │ HTTP                          │
└─────────────────────────┼──────────────────────────────┘
                          │
              ┌───────────┴────────────┐
              │   Public JSON APIs      │
              │  (CoinGecko, Meteo...)  │
              └────────────────────────┘
```

---

## Solution Structure

```
FlexibleMonitoringDashboard/
├── FlexibleMonitoringDashboard.sln
├── src/
│   ├── FlexibleMonitoringDashboard/                    # Server project
│   │   ├── Program.cs
│   │   ├── Endpoints/
│   │   │   └── DataProxyEndpoints.cs
│   │   ├── Services/
│   │   │   ├── ExternalApiService.cs
│   │   │   └── JsonSchemaAnalyzer.cs
│   │   ├── Models/
│   │   │   ├── ProxyRequest.cs
│   │   │   ├── ProxyResponse.cs
│   │   │   └── JsonFieldInfo.cs
│   │   └── Components/
│   │       ├── App.razor
│   │       ├── Routes.razor
│   │       ├── _Imports.razor
│   │       └── Layout/
│   │           └── MainLayout.razor
│   │
│   └── FlexibleMonitoringDashboard.Client/             # Client project
│       ├── Program.cs
│       ├── _Imports.razor
│       ├── Pages/
│       │   └── Dashboard.razor
│       ├── Components/
│       │   ├── Dashboard/
│       │   │   ├── DashboardContainer.razor
│       │   │   ├── DashboardSection.razor
│       │   │   └── ChartWidget.razor
│       │   ├── DataSource/
│       │   │   ├── AddDataSourceDialog.razor
│       │   │   ├── FieldSelector.razor
│       │   │   └── ChartTypeSelector.razor
│       │   ├── Correlation/
│       │   │   └── CorrelationDialog.razor
│       │   ├── Configuration/
│       │   │   ├── ExportButton.razor
│       │   │   ├── ImportButton.razor
│       │   │   └── UnsavedChangesGuard.razor
│       │   └── Threshold/
│       │       └── ThresholdDialog.razor
│       ├── Services/
│       │   ├── DashboardStateService.cs
│       │   ├── DataProxyClient.cs
│       │   ├── ChartTypeRecommender.cs
│       │   └── ConfigurationService.cs
│       ├── Models/
│       │   ├── DashboardConfig.cs
│       │   ├── SectionConfig.cs
│       │   ├── WidgetConfig.cs
│       │   ├── DataSourceConfig.cs
│       │   ├── CorrelationConfig.cs
│       │   └── ThresholdConfig.cs
│       └── wwwroot/
│           ├── css/
│           │   └── app.css
│           └── js/
│               └── interop.js
│
├── tests/
│   └── FlexibleMonitoringDashboard.Tests/
│       ├── Services/
│       │   ├── JsonSchemaAnalyzerTests.cs
│       │   ├── ChartTypeRecommenderTests.cs
│       │   └── ConfigurationServiceTests.cs
│       └── Endpoints/
│           └── DataProxyEndpointsTests.cs
│
├── docs/
│   ├── README.md
│   ├── architecture.md
│   ├── user-guide.md
│   ├── configuration-schema.md
│   └── ai-assistance/
│       └── README.md
│
└── .github/
    └── workflows/
        └── ci.yml
```

---

## Task Breakdown & Dependency Graph

### Parallelization Layers

Tasks within the same layer can run **fully in parallel** without conflicts.

```
Layer 0:  [01-project-scaffolding]
               │
               ▼
Layer 1:  [BE-01] [BE-02] [BE-03] [FE-01] [FE-04] [FE-05] [FE-06] [UI-12] [DOC-*]
               │      │      │       │        │       │       │
               ▼      ▼      ▼       ▼        ▼       ▼       ▼
Layer 2:  [BE-04]           [FE-02] [FE-03]
               │              │       │
               ▼              ▼       ▼
Layer 3:  [UI-01] [UI-07] [UI-09] [UI-10]
               │      │
               ▼      ▼
Layer 4:  [UI-02] [UI-05] [UI-06]
               │      │
               ▼      ▼
Layer 5:  [UI-03] [UI-08] [UI-11]
               │
               ▼
Layer 6:  [UI-04]
               │
               ▼
Layer 7:  [UI-11-threshold] (nice-to-have)
               │
               ▼
Layer 8:  [TEST-01] [TEST-02] (after their implementation targets)
```

### Task List

| ID | Task | Files Created | Dependencies | Layer |
|----|------|--------------|-------------|-------|
| 01 | Project Scaffolding | Solution, both projects, packages | None | 0 |
| BE-01 | Backend Models | ProxyRequest.cs, ProxyResponse.cs, JsonFieldInfo.cs | 01 | 1 |
| BE-02 | External API Service | ExternalApiService.cs | 01 | 1 |
| BE-03 | JSON Schema Analyzer | JsonSchemaAnalyzer.cs | 01 | 1 |
| BE-04 | Proxy Endpoints + Server Wiring | DataProxyEndpoints.cs, Program.cs updates | BE-01,02,03 | 2 |
| FE-01 | Client Config Models | All model .cs files in Client/Models | 01 | 1 |
| FE-02 | Dashboard State Service | DashboardStateService.cs | FE-01 | 2 |
| FE-03 | Configuration Service | ConfigurationService.cs | FE-01 | 2 |
| FE-04 | Chart Type Recommender | ChartTypeRecommender.cs | 01 | 1 |
| FE-05 | Data Proxy Client | DataProxyClient.cs | 01 | 1 |
| FE-06 | JS Interop Helpers | interop.js | 01 | 1 |
| UI-01 | Layout & Theme | MainLayout.razor, App.razor | 01 | 3 |
| UI-02 | Dashboard Page | Dashboard.razor | UI-01 | 4 |
| UI-03 | Dashboard Container | DashboardContainer.razor | FE-02, UI-02 | 5 |
| UI-04 | Dashboard Section | DashboardSection.razor | UI-03 | 6 |
| UI-05 | Chart Widget | ChartWidget.razor | FE-05, FE-01 | 4 |
| UI-06 | Add Data Source Dialog | AddDataSourceDialog.razor | FE-04, FE-05 | 4 |
| UI-07 | Field & Chart Selectors | FieldSelector.razor, ChartTypeSelector.razor | FE-01 | 3 |
| UI-08 | Correlation Dialog | CorrelationDialog.razor | UI-05 | 5 |
| UI-09 | Export/Import Buttons | ExportButton.razor, ImportButton.razor | FE-03, FE-06 | 3 |
| UI-10 | Unsaved Changes Guard | UnsavedChangesGuard.razor | FE-06, FE-02 | 3 |
| UI-11 | Threshold Dialog (Nice-to-have) | ThresholdDialog.razor | UI-05 | 7 |
| UI-12 | Custom CSS | app.css | 01 | 1 |
| TEST-01 | Backend Tests | All test files in tests/ | BE-03, BE-04 | 8 |
| TEST-02 | Frontend Tests | Client service test files | FE-03, FE-04 | 8 |
| DOC-01 | Documentation | All files in docs/ | 01 | 1 |
| DOC-02 | GitHub CI Setup | ci.yml | 01 | 1 |

---

## Key Design Decisions

1. **Backend proxy for external APIs** — Browser CORS prevents direct API calls from WASM; proxy also provides SSRF protection
2. **MudBlazor for UI** — Material Design, 10k+ stars, rich component set
3. **Blazor-ApexCharts** — Supports dual Y-axis (critical for correlation), annotations (for thresholds), 18+ chart types
4. **No database / no auth** — V1 stateless; config persistence via JSON export/import
5. **Polling, not WebSocket** — Configurable per-widget interval (default 30s)
6. **In-memory state only** — All dashboard state lost on page close; beforeunload warning guards against data loss

---

## Verification Checklist

- [ ] `dotnet build` — zero errors/warnings
- [ ] `dotnet test` — all unit tests pass
- [ ] CoinGecko single-value API → Gauge chart renders
- [ ] Open-Meteo time series API → Line chart renders
- [ ] Cross-source correlation → dual Y-axis chart renders
- [ ] Export → close tab (warning shown) → reopen → import → all charts rebuild
- [ ] Section headers/descriptions display and edit correctly
- [ ] Responsive at 375px / 768px / 1440px
- [ ] Dark/light theme toggle works
- [ ] Invalid URL / malformed JSON → error state with retry
