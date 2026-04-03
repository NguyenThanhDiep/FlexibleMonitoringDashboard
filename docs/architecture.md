# Architecture & Design Decisions

## Overview

FlexBoard is a Blazor WebAssembly application hosted on an ASP.NET Core server. The client runs entirely in the browser; the server acts as a secure proxy to external JSON APIs.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                        Browser                          │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │          Blazor WebAssembly (Client)              │  │
│  │                                                  │  │
│  │  Pages/Dashboard ──► DashboardStateService       │  │
│  │         │                    │                   │  │
│  │  UI Components          ConfigurationService     │  │
│  │  (MudBlazor)             (localStorage)          │  │
│  │         │                                        │  │
│  │  ApexCharts ◄── ChartTypeRecommender             │  │
│  │                         │                        │  │
│  │               DataProxyClient (HttpClient)        │  │
│  └──────────────────────┬───────────────────────────┘  │
└─────────────────────────┼───────────────────────────────┘
                          │ HTTPS
┌─────────────────────────▼───────────────────────────────┐
│              ASP.NET Core Server                        │
│                                                         │
│   /api/proxy  ──► ExternalApiService                   │
│                          │                              │
│   /api/analyze ──► JsonSchemaAnalyzer                  │
│                          │                              │
│                    UrlValidator                         │
└─────────────────────────┬───────────────────────────────┘
                          │ HTTPS
                 External JSON APIs
              (CoinGecko, Open-Meteo, etc.)
```

## Project Structure

```
FlexibleMonitoringDashboard.sln
├── src/
│   ├── FlexibleMonitoringDashboard/          # Server (ASP.NET Core host)
│   │   ├── Endpoints/                        # Minimal API route handlers
│   │   ├── Models/                           # Proxy request/response DTOs
│   │   ├── Services/
│   │   │   ├── ExternalApiService.cs         # Outbound HTTP calls
│   │   │   ├── JsonSchemaAnalyzer.cs         # Field detection from JSON
│   │   │   └── UrlValidator.cs               # SSRF prevention
│   │   └── Program.cs
│   └── FlexibleMonitoringDashboard.Client/   # WASM client
│       ├── Models/                           # Shared config models
│       ├── Services/
│       │   ├── ChartTypeRecommender.cs       # Recommends chart types
│       │   └── DataProxyClient.cs            # Calls backend proxy
│       ├── Pages/                            # Routable pages
│       ├── Components/                       # Reusable UI components
│       └── Layout/                           # App shell / navigation
└── tests/
    └── FlexibleMonitoringDashboard.Tests/    # xUnit test project
```

## Tech Stack Rationale

### Blazor WebAssembly (.NET 9)
- Single language (C#) across frontend and backend eliminates context switching
- Strong typing for configuration models shared between client and server logic
- No JavaScript framework toolchain complexity (no Node, Webpack, etc.)
- Interactive mode allows client to run entirely in browser after initial load

### MudBlazor
- Production-ready Material Design component library for Blazor
- Rich component set (dialogs, grids, drawers, themes) with minimal custom CSS
- Built-in dark/light theme switching support
- Active community and up-to-date .NET 9 support

### Blazor-ApexCharts
- Thin Blazor wrapper around the popular ApexCharts.js library
- Supports all required chart types: Line, Bar, Area, Pie, Donut, RadialBar, Scatter
- Dual Y-axis support for correlation charts
- Threshold annotations for alarm visualization
- No DI registration required — just `@using ApexCharts`

### ASP.NET Core Minimal APIs
- Lightweight, low-ceremony API definition
- Built-in CORS, dependency injection, and middleware pipeline
- Serves as a security boundary: the client never calls external APIs directly

### xUnit + Moq
- xUnit is the de-facto standard test framework for .NET
- Moq provides clean interface mocking without boilerplate

## Data Flow

```
1. User pastes a URL into the "Add Chart" dialog
2. Client calls POST /api/analyze with the URL
3. Server's UrlValidator validates the URL (no private IPs, allowed schemes)
4. ExternalApiService fetches the JSON from the external API
5. JsonSchemaAnalyzer introspects the JSON, returns field names + types
6. Client receives field list, ChartTypeRecommender suggests a chart type
7. User confirms field mappings and chart type, widget is added to dashboard
8. On a configurable interval (default 30 s), DataProxyClient calls POST /api/proxy
9. Server fetches fresh data, returns it to the client
10. ApexCharts renders the updated data
```

## Security

### SSRF Prevention
The `UrlValidator` class enforces:
- Only `http://` and `https://` schemes are permitted
- Hostnames that resolve to private/loopback IP ranges are rejected
  (`10.x.x.x`, `172.16–31.x.x`, `192.168.x.x`, `127.x.x.x`, `::1`, `169.254.x.x`)
- The URL must have a valid host component

### No Authentication
FlexBoard targets public JSON APIs that require no credentials. API keys passed via custom headers are stored only in browser memory (never persisted to localStorage) and never logged server-side.

### CORS
The development CORS policy is permissive for localhost convenience. In production, restrict `AllowAnyOrigin` to the specific host.

## Design Decisions

### Polling vs WebSocket
Polling was chosen because:
- External APIs are stateless HTTP endpoints; they do not push data
- Per-chart configurable intervals (5 s – 300 s) cover most use cases
- Simpler implementation with no persistent connection state
- WebSockets would require the external APIs to support them

### No Database
Dashboard configuration is stored as a JSON file that the user downloads and re-uploads. This eliminates infrastructure requirements (no database, no auth), making the app runnable anywhere with `dotnet run`.

### JSON Export/Import
Export serializes the in-memory `DashboardConfig` to a pretty-printed JSON file. Import deserializes and validates it. This pattern supports sharing dashboards via version control or email without any server-side storage.

### Blazor Interactive WebAssembly Mode
The `@rendermode InteractiveWebAssembly` render mode is applied globally so all components run in the browser. Server-side prerendering is available for the initial HTML shell but is not required for correctness.

## Limitations

- **No authentication**: not suitable for private/internal APIs without modification
- **Polling only**: no support for streaming or push-based data sources
- **No persistence**: dashboard state is lost on page refresh unless exported and re-imported (or stored in localStorage via `ConfigurationService`)
- **Public APIs only**: SSRF controls intentionally block private-network URLs
