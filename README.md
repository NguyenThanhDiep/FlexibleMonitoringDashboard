# FlexBoard — Flexible Monitoring Dashboard

A real-time monitoring dashboard web application that displays dynamic charts
from any public JSON API. Built with .NET 9, Blazor WebAssembly, MudBlazor,
and ApexCharts.

## Quick Start

1. Clone the repository
2. `dotnet run --project src/FlexibleMonitoringDashboard`
3. Open `https://localhost:5001` in your browser
4. Add a section → Add a chart → Paste any JSON API URL → Done!

## Features

- **Universal URL Connector**: Paste any public JSON API URL, auto-detect fields, render instantly
- **Smart Chart Suggestions**: Automatic chart type recommendation based on data structure
- **Cross-Source Correlation**: Overlay data from two APIs on one dual-axis chart
- **JSON Config Export/Import**: Save and share dashboards via JSON configuration files
- **Threshold Alarms**: Get notified when values cross configured thresholds
- **Beautiful UI**: Material Design with dark/light theme, responsive layout

## Documentation

- [Product Overview & Architecture](docs/architecture.md)
- [User Guide](docs/user-guide.md)
- [Configuration Schema Reference](docs/configuration-schema.md)
- [AI Assistance Logs](docs/ai-assistance/)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor WebAssembly (.NET 9) |
| UI | MudBlazor (Material Design) |
| Charts | Blazor-ApexCharts |
| Backend | ASP.NET Core Minimal APIs |
| Tests | xUnit + Moq |

## License

MIT
