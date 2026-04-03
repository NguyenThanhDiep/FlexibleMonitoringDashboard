# Task UI-11: Threshold Dialog (Nice-to-Have)

## Metadata
- **ID**: UI-11-threshold-dialog
- **Layer**: 7 (nice-to-have, implement last)
- **Dependencies**: `UI-05`
- **Blocks**: None
- **Parallel With**: None

---

## Files Created

| File | Path |
|------|------|
| `ThresholdDialog.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Threshold/ThresholdDialog.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `ThresholdDialog.razor`

Dialog to configure a threshold alarm on a chart widget.

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Threshold/ThresholdDialog.razor *@
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@inject DashboardStateService State
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center">
            <MudIcon Icon="@Icons.Material.Filled.NotificationsActive" Class="mr-2" />
            <MudText Typo="Typo.h6">Configure Threshold Alarm</MudText>
        </MudStack>
    </TitleContent>
    <DialogContent>
        <MudStack Spacing="3">
            <MudText Typo="Typo.body2" Style="opacity: 0.7;">
                Set a threshold to receive an alert when the chart value crosses it.
            </MudText>

            <MudSwitch @bind-Value="_enabled" Color="Color.Primary" Label="Enable Threshold Alarm" />

            @if (_enabled)
            {
                <MudNumericField T="double" @bind-Value="_value"
                                 Label="Threshold Value"
                                 Variant="Variant.Outlined"
                                 Step="0.01" />

                <MudSelect T="ThresholdDirection" @bind-Value="_direction"
                           Label="Trigger When"
                           Variant="Variant.Outlined">
                    <MudSelectItem Value="ThresholdDirection.Above">
                        Value goes ABOVE threshold
                    </MudSelectItem>
                    <MudSelectItem Value="ThresholdDirection.Below">
                        Value goes BELOW threshold
                    </MudSelectItem>
                </MudSelect>

                <MudTextField @bind-Value="_label"
                              Label="Threshold Label"
                              Variant="Variant.Outlined"
                              Placeholder="e.g., Target Price" />

                <MudColorPicker @bind-Value="_mudColor"
                                Label="Alarm Color"
                                Variant="Variant.Outlined"
                                DisableAlpha="true" />
            }
        </MudStack>
    </DialogContent>
    <DialogActions>
        @if (Widget.Threshold != null)
        {
            <MudButton Color="Color.Error" OnClick="RemoveThreshold">Remove</MudButton>
        }
        <MudSpacer />
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Save">
            Save
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [Parameter] public WidgetConfig Widget { get; set; } = null!;
    [Parameter] public string SectionId { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private bool _enabled;
    private double _value;
    private ThresholdDirection _direction = ThresholdDirection.Above;
    private string _label = "Threshold";
    private MudColor _mudColor = new("#FF0000");

    protected override void OnInitialized()
    {
        if (Widget.Threshold != null)
        {
            _enabled = Widget.Threshold.Enabled;
            _value = Widget.Threshold.Value;
            _direction = Widget.Threshold.Direction;
            _label = Widget.Threshold.Label;
            _mudColor = new MudColor(Widget.Threshold.Color);
        }
    }

    private void Save()
    {
        Widget.Threshold = new ThresholdConfig
        {
            Enabled = _enabled,
            Value = _value,
            Direction = _direction,
            Label = _label,
            Color = _mudColor.Value
        };

        State.UpdateWidget(SectionId, Widget);
        Snackbar.Add("Threshold configured!", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void RemoveThreshold()
    {
        Widget.Threshold = null;
        State.UpdateWidget(SectionId, Widget);
        Snackbar.Add("Threshold removed.", Severity.Info);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
```

### Step 2: Add Menu Item in ChartWidget

In `ChartWidget.razor` (UI-05), add a menu item to open this dialog:

```razor
<MudMenuItem Icon="@Icons.Material.Filled.NotificationsActive" OnClick="ConfigureThreshold">
    Threshold
</MudMenuItem>
```

And in the `@code` block:

```csharp
private async Task ConfigureThreshold()
{
    var parameters = new DialogParameters
    {
        { "Widget", WidgetConfig },
        { "SectionId", SectionId }
    };
    var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
    var dialog = await DialogService.ShowAsync<ThresholdDialog>("Threshold", parameters, options);
    await dialog.Result;
}
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Dialog opens from ChartWidget's menu
- [ ] Toggle enables/disables threshold fields
- [ ] Saving updates the widget's `Threshold` property
- [ ] Removing threshold sets `Threshold = null`
- [ ] Color picker works and persists hex color
- [ ] Existing threshold values are pre-populated when editing
- [ ] ChartWidget shows alarm alert when threshold is breached
