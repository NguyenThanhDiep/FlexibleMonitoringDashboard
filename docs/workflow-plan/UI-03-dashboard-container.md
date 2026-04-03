# Task UI-03: Dashboard Container

## Metadata
- **ID**: UI-03-dashboard-container
- **Layer**: 5
- **Dependencies**: `FE-02`, `UI-02`
- **Blocks**: `UI-04`
- **Parallel With**: UI-08, UI-11

---

## Files Created

| File | Path |
|------|------|
| `DashboardContainer.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Dashboard/DashboardContainer.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `DashboardContainer.razor`

This component iterates all sections from `DashboardStateService` and renders `DashboardSection` for each.

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Dashboard/DashboardContainer.razor *@
@using FlexibleMonitoringDashboard.Client.Models
@using FlexibleMonitoringDashboard.Client.Services
@inject DashboardStateService State
@implements IDisposable

<MudStack Spacing="0">
    @foreach (var section in State.Config.Sections.OrderBy(s => s.Order))
    {
        <DashboardSection SectionConfig="section"
                          OnEdit="() => EditSection(section.Id)"
                          OnDelete="() => ConfirmDeleteSection(section.Id)"
                          OnMoveUp="() => State.MoveSection(section.Id, -1)"
                          OnMoveDown="() => State.MoveSection(section.Id, 1)"
                          IsFirst="section == State.Config.Sections.First()"
                          IsLast="section == State.Config.Sections.Last()" />
    }
</MudStack>

@* Confirm delete dialog *@
<MudMessageBox @ref="_deleteBox"
               Title="Delete Section"
               CancelText="Cancel">
    <MessageContent>
        Are you sure you want to delete this section and all its charts?
        This action cannot be undone.
    </MessageContent>
    <YesButton>
        <MudButton Variant="Variant.Filled" Color="Color.Error" StartIcon="@Icons.Material.Filled.Delete">
            Delete
        </MudButton>
    </YesButton>
</MudMessageBox>

@code {
    private MudMessageBox _deleteBox = null!;
    private string? _pendingDeleteSectionId;

    protected override void OnInitialized()
    {
        State.OnStateChanged += StateHasChanged;
    }

    private void EditSection(string sectionId)
    {
        // Section editing is handled inline by DashboardSection component
        // This callback exists for future expansion (e.g., modal editor)
    }

    private async Task ConfirmDeleteSection(string sectionId)
    {
        _pendingDeleteSectionId = sectionId;
        var result = await _deleteBox.ShowAsync();
        if (result == true)
        {
            State.RemoveSection(sectionId);
        }
    }

    public void Dispose()
    {
        State.OnStateChanged -= StateHasChanged;
    }
}
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Container renders all sections from state in order
- [ ] Adding a section in state triggers container re-render
- [ ] Delete confirmation dialog appears before section removal
- [ ] Move up/down buttons correctly reorder sections
- [ ] First section's "move up" is disabled (via `IsFirst` prop)
- [ ] Last section's "move down" is disabled (via `IsLast` prop)
