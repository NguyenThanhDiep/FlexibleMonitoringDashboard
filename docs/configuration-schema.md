# Configuration Schema Reference

FlexBoard dashboards are stored and exchanged as JSON. This document describes every field in the configuration schema.

---

## Root Object — `DashboardConfig`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `version` | `string` | `"1.0"` | Schema version for forward/backward compatibility |
| `name` | `string` | `"My Dashboard"` | Dashboard display name |
| `description` | `string \| null` | `null` | Optional dashboard description |
| `sections` | `SectionConfig[]` | `[]` | Ordered list of sections |
| `exportedAt` | `string (ISO 8601) \| null` | `null` | Timestamp when the config was last exported |
| `darkMode` | `boolean` | `false` | Whether to use dark theme |

---

## `SectionConfig`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `id` | `string` | auto-generated GUID | Unique identifier for this section |
| `title` | `string` | `"New Section"` | Section header text |
| `description` | `string \| null` | `null` | Optional subtitle shown below the header |
| `widgets` | `WidgetConfig[]` | `[]` | Ordered list of chart widgets |
| `isCollapsed` | `boolean` | `false` | Whether the section is collapsed in the UI |

---

## `WidgetConfig`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `id` | `string` | auto-generated GUID | Unique identifier for this widget |
| `title` | `string` | `"Untitled Chart"` | Display title shown above the chart |
| `description` | `string \| null` | `null` | Optional subtitle |
| `chartType` | `ChartType` | `"Line"` | Chart type enum (see below) |
| `dataSource` | `DataSourceConfig` | — | Primary data source (required) |
| `fieldMappings` | `FieldMapping[]` | `[]` | Maps JSON fields to chart axes |
| `columnSpan` | `integer (1–12)` | `6` | Widget width in MudGrid columns |
| `threshold` | `ThresholdConfig \| null` | `null` | Optional alarm threshold |
| `correlation` | `CorrelationConfig \| null` | `null` | Optional cross-source overlay |

---

## `ChartType` Enum

| Value | Description |
|-------|-------------|
| `"Line"` | Line chart — time-series trends |
| `"Bar"` | Bar chart — category comparisons |
| `"Area"` | Area chart — filled line |
| `"Pie"` | Pie chart — part-of-whole |
| `"Donut"` | Donut chart — pie with center label |
| `"RadialBar"` | Radial bar — percentage / progress |
| `"Scatter"` | Scatter plot — two-numeric correlation |

---

## `DataSourceConfig`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `id` | `string` | auto-generated GUID | Unique identifier |
| `url` | `string` | `""` | Public JSON API URL (required) |
| `headers` | `object \| null` | `null` | Optional request headers (`{"Key": "Value"}`) |
| `refreshIntervalSeconds` | `integer` | `30` | Auto-refresh interval in seconds; `0` = disabled |
| `jsonPath` | `string \| null` | `null` | Dot-path to drill into nested data (e.g., `"hourly"`) |

---

## `FieldMapping`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `fieldPath` | `string` | `""` | JSON dot-path to the field (e.g., `"hourly.temperature_2m"`) |
| `label` | `string` | `""` | Display label in chart legend |
| `role` | `FieldRole` | `"XAxis"` | Role of this field (see below) |

### `FieldRole` Enum

| Value | Description |
|-------|-------------|
| `"XAxis"` | Horizontal axis (categories, timestamps) |
| `"YAxis"` | Vertical axis (numeric values) |
| `"Category"` | Category label for pie/donut/radialBar charts |
| `"Value"` | Numeric value for pie/donut/radialBar charts |

---

## `ThresholdConfig`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `value` | `number` | — | Threshold value to compare against |
| `direction` | `ThresholdDirection` | `"Above"` | Trigger when value is `"Above"` or `"Below"` the threshold |
| `color` | `string` | `"#FF5252"` | HEX color for the alarm indicator |
| `label` | `string` | `"Threshold"` | Text shown on the threshold annotation line |
| `enabled` | `boolean` | `true` | Whether the threshold alarm is active |

### `ThresholdDirection` Enum

| Value | Description |
|-------|-------------|
| `"Above"` | Alarm when current value exceeds the threshold |
| `"Below"` | Alarm when current value falls below the threshold |

---

## `CorrelationConfig`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `secondaryDataSource` | `DataSourceConfig` | — | Secondary data source (required for correlation) |
| `secondaryFieldMappings` | `FieldMapping[]` | `[]` | Field mappings for the secondary series |
| `useDualYAxis` | `boolean` | `true` | Use a separate right-side Y-axis for the secondary series |
| `secondaryYAxisLabel` | `string \| null` | `null` | Label for the secondary (right) Y-axis |
| `primaryYAxisLabel` | `string \| null` | `null` | Label for the primary (left) Y-axis |

---

## Example Configurations

### Minimal — Single section, single scalar chart

```json
{
  "version": "1.0",
  "name": "Bitcoin Price",
  "sections": [
    {
      "id": "sec-001",
      "title": "Crypto",
      "widgets": [
        {
          "id": "wgt-001",
          "title": "BTC / USD",
          "chartType": "RadialBar",
          "dataSource": {
            "id": "ds-001",
            "url": "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd",
            "refreshIntervalSeconds": 60
          },
          "fieldMappings": [
            { "fieldPath": "bitcoin.usd", "label": "BTC Price USD", "role": "Value" }
          ],
          "columnSpan": 6
        }
      ]
    }
  ]
}
```

### Open-Meteo Weather Line Chart (Ho Chi Minh City)

```json
{
  "version": "1.0",
  "name": "Weather Dashboard",
  "sections": [
    {
      "id": "sec-weather",
      "title": "Ho Chi Minh Weather",
      "widgets": [
        {
          "id": "wgt-temp",
          "title": "Hourly Weather",
          "chartType": "Line",
          "dataSource": {
            "id": "ds-meteo",
            "url": "https://api.open-meteo.com/v1/forecast?latitude=10.823&longitude=106.6296&hourly=temperature_2m,relative_humidity_2m,wind_speed_10m,precipitation&forecast_days=7",
            "refreshIntervalSeconds": 30
          },
          "fieldMappings": [
            { "fieldPath": "hourly.time", "label": "time", "role": "XAxis" },
            { "fieldPath": "hourly.temperature_2m", "label": "temperature_2m", "role": "YAxis" },
            { "fieldPath": "hourly.relative_humidity_2m", "label": "relative_humidity_2m", "role": "YAxis" },
            { "fieldPath": "hourly.wind_speed_10m", "label": "wind_speed_10m", "role": "YAxis" },
            { "fieldPath": "hourly.precipitation", "label": "precipitation", "role": "YAxis" }
          ],
          "columnSpan": 10,
          "threshold": {
            "value": 35,
            "direction": "Above",
            "color": "#FF5252",
            "label": "Heat Alert",
            "enabled": true
          }
        }
      ]
    }
  ]
}
```

### Cross-Source Correlation — BTC Price vs Temperature

```json
{
  "version": "1.0",
  "name": "Correlation Demo",
  "sections": [
    {
      "id": "sec-corr",
      "title": "BTC vs Weather",
      "widgets": [
        {
          "id": "wgt-corr",
          "title": "BTC Price & Berlin Temperature",
          "chartType": "Line",
          "dataSource": {
            "id": "ds-btc",
            "url": "https://api.coingecko.com/api/v3/coins/bitcoin/market_chart?vs_currency=usd&days=1",
            "refreshIntervalSeconds": 300,
            "jsonPath": "prices"
          },
          "fieldMappings": [
            { "fieldPath": "[0]", "label": "Timestamp", "role": "XAxis" },
            { "fieldPath": "[1]", "label": "BTC (USD)", "role": "YAxis" }
          ],
          "columnSpan": 12,
          "correlation": {
            "secondaryDataSource": {
              "id": "ds-weather",
              "url": "https://api.open-meteo.com/v1/forecast?latitude=52.52&longitude=13.41&hourly=temperature_2m&forecast_days=1",
              "refreshIntervalSeconds": 600,
              "jsonPath": "hourly"
            },
            "secondaryFieldMappings": [
              { "fieldPath": "time", "label": "Hour", "role": "XAxis" },
              { "fieldPath": "temperature_2m", "label": "Temp (°C)", "role": "YAxis" }
            ],
            "useDualYAxis": true,
            "primaryYAxisLabel": "BTC Price (USD)",
            "secondaryYAxisLabel": "Temperature (°C)"
          }
        }
      ]
    }
  ]
}
```

---

## Validation Rules

- `dataSource.url` must be a valid `http://` or `https://` URL pointing to a **public** host
- `dataSource.url` must **not** resolve to a private/loopback IP range (SSRF protection)
- `columnSpan` must be between `1` and `12` inclusive
- `dataSource.refreshIntervalSeconds` must be `≥ 0`
- `threshold.value` must be a finite number
- `fieldMappings` must contain at least one entry for the chart to render

---

## Full Example Dashboard

For a complete, ready-to-import dashboard configuration featuring weather data, country statistics, and cryptocurrency prices, see [`docs/examples/my-dashboard.json`](examples/my-dashboard.json).
