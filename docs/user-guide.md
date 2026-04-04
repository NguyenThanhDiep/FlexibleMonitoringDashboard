# User Guide

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- A modern browser (Chrome, Edge, Firefox, Safari)
- Internet access (to reach public JSON APIs)

## Getting Started

### 1. Clone and Run

```bash
git clone https://github.com/your-org/FlexibleMonitoringDashboard.git
cd FlexibleMonitoringDashboard
dotnet run --project src/FlexibleMonitoringDashboard
```

Open `http://localhost:5125` in your browser.

### 2. First Launch

You will see an empty dashboard with a prompt to add your first section. The left navigation drawer can be toggled with the hamburger icon in the top-left corner.

---

## Working with Sections

Sections group related charts under a common header.

### Add a Section
1. Click **+ Add Section** on the empty dashboard (or the FAB button).
2. Enter a **Title** (required) and optional **Description**.
3. Click **Save**.

### Edit a Section
1. Click the pencil icon on the section header.
2. Modify the title or description.
3. Click **Save**.

### Collapse / Expand a Section
Click the chevron icon on the section header to toggle visibility of its charts.

### Delete a Section
1. Click the trash icon on the section header.
2. Confirm in the dialog. **This also deletes all charts in the section.**

---

## Adding Your First Chart

1. Click **+ Add Chart** inside any section.
2. The **Add Data Source** dialog opens.

### Step 1 — Paste the API URL
Enter any public JSON API URL. Examples:
```
https://api.open-meteo.com/v1/forecast?latitude=10.823&longitude=106.6296&hourly=temperature_2m,relative_humidity_2m,wind_speed_10m,precipitation&forecast_days=7
```
```
https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd
```
Click **Analyze**.

### Step 2 — Review Detected Fields
The app fetches the JSON and lists all detected fields with their inferred types (`number`, `string`, `boolean`, `array`). If the API returns an array at the root or at a nested path, specify the **JSON Path** (e.g., `hourly`) to drill into it.

### Step 3 — Configure Field Mappings
- **X Axis**: the field used as categories or time labels
- **Y Axis**: the numeric field(s) to plot

For simple scalar APIs (e.g., `{"bitcoin": {"usd": 65000}}`), map the nested numeric field as the Y value.

### Step 4 — Choose a Chart Type
A chart type is pre-selected based on the detected data shape. You can override it using the **Chart Type** dropdown. See [Chart Types](#chart-types) for guidance.

### Step 5 — Set Widget Options
- **Title**: display name shown above the chart
- **Description**: optional subtitle
- **Column Span**: width (1–12 grid columns; 6 = half width, 12 = full width)
- **Refresh Interval**: seconds between automatic data refreshes (0 = manual only)

Click **Add** to add the chart to the section.

---

## Chart Types

| Type | Best For | Notes |
|------|----------|-------|
| **Line** | Time-series trends, continuous data | Use with an X-axis time/category field |
| **Area** | Same as line, emphasizes volume | Filled area under the line |
| **Bar** | Comparisons across categories | Vertical bars |
| **Pie** | Part-of-whole breakdown | Works best with 2–6 categories |
| **Donut** | Same as pie with center label | Good for a single key metric |
| **RadialBar** | Percentage / progress toward a goal | Single value 0–100 |
| **Scatter** | Correlation between two numeric fields | Requires two numeric fields |

---

## Cross-Source Correlation

Overlay data from two different APIs on one dual-axis chart.

1. Add a chart normally (this becomes the **primary** data source).
2. Open the chart's edit menu (three-dot icon) → **Add Correlation**.
3. In the **Correlation** dialog:
   - Paste the **secondary API URL** and click **Analyze**.
   - Map fields for the secondary series.
   - Toggle **Dual Y-Axis** if the two series have different units/scales.
   - Enter optional axis labels.
4. Click **Save**. Both series now appear on the same chart.

---

## Threshold Alarms

Thresholds draw a horizontal reference line on a chart and change the widget's border color when the current value crosses the threshold.

1. Open the chart's edit menu → **Configure Threshold**.
2. Set:
   - **Value**: the numeric threshold (e.g., `70000`)
   - **Direction**: `Above` (alarm when value exceeds threshold) or `Below`
   - **Label**: text shown on the threshold line
   - **Color**: alarm indicator color (HEX color, e.g., `#FF5252`)
3. Toggle **Enabled** on.
4. Click **Save**.

When the latest data point crosses the threshold, the chart widget border turns the configured alarm color.

---

## Exporting Your Dashboard

Export saves the entire dashboard configuration — all sections, charts, field mappings, thresholds, and refresh settings — as a JSON file.

1. Click the **Export** button in the top app bar (download icon).
2. A JSON file named `dashboard-config.json` is downloaded to your browser's default download folder.
3. Store this file in version control, share it by email, or commit it to a repository.

> **Note**: The export does **not** include cached API data, only configuration.

---

## Importing a Dashboard

1. Click the **Import** button in the top app bar (upload icon).
2. Select a previously exported `dashboard-config.json` file.
3. The current dashboard is replaced by the imported configuration. Any unsaved changes will be lost (you will be warned first).

---

## Keyboard Shortcuts and Tips

| Action | Tip |
|--------|-----|
| Collapse all sections | Click each section header chevron |
| Resize a chart | Change **Column Span** in the widget's edit dialog |
| Pause auto-refresh | Set **Refresh Interval** to `0` |
| Force a refresh now | Click the refresh icon on the chart widget |
| Switch theme | Click the sun/moon icon in the top-right app bar |
| Unsaved changes | A banner appears at the top when you have unsaved changes; click **Save** to persist to localStorage |

---

## Troubleshooting

### "Could not reach URL"
- Confirm the URL is publicly accessible from your browser
- Some APIs require CORS headers — if the URL works in the browser but not here, the server proxy may be blocking it
- Check that the URL uses `http://` or `https://`

### "Field not found in response"
- The API response structure may have changed since the widget was configured
- Re-open the widget's edit dialog, click **Re-analyze**, and update field mappings

### "Chart shows no data"
- Check the browser developer console for network errors
- The external API may be rate-limiting requests; increase the refresh interval
