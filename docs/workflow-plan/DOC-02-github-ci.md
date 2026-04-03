# Task DOC-02: GitHub Repository Setup

## Metadata
- **ID**: DOC-02-github-ci
- **Layer**: 1 (can start immediately)
- **Dependencies**: `01-project-scaffolding`
- **Blocks**: None
- **Parallel With**: Everything

---

## Files Created

| File | Path |
|------|------|
| `ci.yml` | `.github/workflows/ci.yml` |
| `.gitignore` | Root `.gitignore` |
| `.editorconfig` | Root `.editorconfig` |
| `Directory.Build.props` | Root `Directory.Build.props` |

---

## Step-by-Step Implementation

### Step 1: Create `.gitignore`

```gitignore
## .NET
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates
*.userprefs

## VS Code
.vscode/

## Visual Studio
.vs/
*.sln.local

## Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Oo]ut/
[Ll]og/
[Ll]ogs/

## NuGet
*.nupkg
**/[Pp]ackages/*
!**/[Pp]ackages/build/
*.nuget.props
*.nuget.targets
project.lock.json
project.fragment.lock.json

## Test results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*
*.trx
TestResult.xml

## OS files
.DS_Store
Thumbs.db
ehthumbs.db
Desktop.ini

## Secrets
appsettings.Development.json
```

### Step 2: Create `.editorconfig`

```editorconfig
# EditorConfig
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.{csproj,json,yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

### Step 3: Create `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### Step 4: Create `.github/workflows/ci.yml`

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
    strategy:
      matrix:
        dotnet-version: ['8.0.x']

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET ${{ matrix.dotnet-version }}
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet-version }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Run unit tests
        run: dotnet test --no-build --configuration Release --verbosity normal --logger "trx;LogFileName=test-results.trx"

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/TestResults/*.trx'
```

### Step 5: Create Solution File Structure

After scaffolding (Task 01), initialize the repository:

```bash
# Initialize git repo
git init

# Create solution file
dotnet new sln -n FlexibleMonitoringDashboard

# Add projects to solution
dotnet sln add src/FlexibleMonitoringDashboard/FlexibleMonitoringDashboard.csproj
dotnet sln add src/FlexibleMonitoringDashboard.Client/FlexibleMonitoringDashboard.Client.csproj
dotnet sln add tests/FlexibleMonitoringDashboard.Tests/FlexibleMonitoringDashboard.Tests.csproj

# Initial commit
git add .
git commit -m "Initial project scaffold"
```

### Step 6: Create GitHub Repository (Manual)

1. Go to [github.com/new](https://github.com/new)
2. Repository name: `flexible-monitoring-dashboard`
3. Visibility: **Public**
4. Do NOT initialize with README (we already have one)
5. Push:

```bash
git remote add origin https://github.com/<USERNAME>/flexible-monitoring-dashboard.git
git branch -M main
git push -u origin main
```

---

## Verification

- [ ] `.gitignore` prevents `bin/`, `obj/`, `.vs/`, secrets from being committed
- [ ] `.editorconfig` enforces consistent formatting
- [ ] `Directory.Build.props` sets shared build properties
- [ ] CI workflow builds and tests on push/PR to `main`
- [ ] CI uploads test results as artifacts
- [ ] Solution file includes all 3 projects
- [ ] `git status` shows clean working tree after initial commit
