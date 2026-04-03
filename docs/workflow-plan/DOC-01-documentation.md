# Task DOC-01: Documentation

## Metadata
- **ID**: DOC-01-documentation
- **Layer**: 1 (can start immediately, parallel with all tasks)
- **Dependencies**: `01-project-scaffolding` (for accurate paths)
- **Blocks**: None
- **Parallel With**: Everything

---

## Files Created

| File | Path |
|------|------|
| `README.md` | `docs/README.md` |
| `architecture.md` | `docs/architecture.md` |
| `user-guide.md` | `docs/user-guide.md` |
| `configuration-schema.md` | `docs/configuration-schema.md` |
| `README.md` | `docs/ai-assistance/README.md` |
| `README.md` | Root `README.md` |

---

## Step-by-Step Implementation

### Step 1: Create Root `README.md`

```markdown
# FlexBoard — Flexible Monitoring Dashboard

A real-time monitoring dashboard web application that displays dynamic charts
from any public JSON API. Built with .NET 8, Blazor WebAssembly, MudBlazor,
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
| Frontend | Blazor WebAssembly (.NET 8) |
| UI | MudBlazor (Material Design) |
| Charts | Blazor-ApexCharts |
| Backend | ASP.NET Core Minimal APIs |
| Tests | xUnit + Moq |

## License

MIT
```

### Step 2: Create `docs/architecture.md`

Document:
- Architecture diagram (ASCII or Mermaid)
- Tech stack rationale (why Blazor WASM, why MudBlazor, why ApexCharts)
- Project structure explanation
- Data flow: User → UI → Client Services → Backend Proxy → External API → back
- Security: SSRF prevention, URL validation, no auth decisions
- Design decisions: polling vs WebSocket, no database, JSON export/import
- Trade-offs and limitations

### Step 3: Create `docs/user-guide.md`

Document:
- Getting started (prerequisites, build, run)
- Adding your first chart (step-by-step with screenshots)
- Working with sections (add, edit, reorder, collapse)
- Chart types explained (when to use each)
- Cross-source correlation (step-by-step)
- Exporting config (how and what's saved)
- Importing config (how to share dashboards)
- Threshold alarms (how to configure)
- Keyboard shortcuts and tips

### Step 4: Create `docs/configuration-schema.md`

Document:
- Complete JSON schema for `DashboardConfig`
- All properties with types, defaults, descriptions
- Example configs:
  - Minimal (one section, one chart)
  - Full (multiple sections, correlation, thresholds)
  - CoinGecko BTC price gauge
  - Open-Meteo weather line chart
- Validation rules

### Step 5: Create `docs/ai-assistance/README.md`

```markdown
# AI Assistance

This project used AI tools during development.
All AI conversation threads are stored in this folder.

## Chat Threads

| # | Topic | Tool | Link |
|---|-------|------|------|
| 1 | Initial architecture and planning | Claude | [Thread 1](./thread-01-architecture.md) |
| 2 | Implementation guidance | Claude | [Thread 2](./thread-02-implementation.md) |

## How AI Was Used

- Architecture planning and tech stack selection
- Code generation and review
- Documentation assistance
- Test case design
```

### Step 6: Create GitHub CI Workflow

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal
```

---

## Verification

- [ ] Root README.md provides quick start in 4 steps
- [ ] `docs/architecture.md` explains all design decisions
- [ ] `docs/user-guide.md` is usable by a non-developer
- [ ] `docs/configuration-schema.md` documents every field in the JSON config
- [ ] `docs/ai-assistance/` folder exists with README
- [ ] GitHub Actions CI pipeline builds and tests on push
- [ ] All documentation uses proper Markdown formatting
- [ ] No broken internal links
