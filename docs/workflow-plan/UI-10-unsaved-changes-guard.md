# Task UI-10: Unsaved Changes Guard

## Metadata
- **ID**: UI-10-unsaved-changes-guard
- **Layer**: 3
- **Dependencies**: `FE-06`, `FE-02`
- **Blocks**: None
- **Parallel With**: UI-01, UI-07, UI-09

---

## Files Created

| File | Path |
|------|------|
| `UnsavedChangesGuard.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Configuration/UnsavedChangesGuard.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `UnsavedChangesGuard.razor`

This component registers a `beforeunload` event listener via JS interop. When the user has unsaved changes (added/modified widgets but not exported), the browser will show a native confirmation dialog before allowing the page to close.

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Configuration/UnsavedChangesGuard.razor *@
@using FlexibleMonitoringDashboard.Client.Services
@inject DashboardStateService State
@inject IJSRuntime JSRuntime
@implements IAsyncDisposable

@* This component renders nothing visible — it only manages the JS event listener *@

@code {
    private bool _isRegistered = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Register the beforeunload handler
            await JSRuntime.InvokeVoidAsync("dashboardInterop.registerBeforeUnload", null);
            _isRegistered = true;

            // Subscribe to state changes to keep JS flag in sync
            State.OnStateChanged += OnStateChanged;
        }
    }

    private async void OnStateChanged()
    {
        try
        {
            // Update the JS-side flag so beforeunload knows whether to warn
            await JSRuntime.InvokeVoidAsync("dashboardInterop.setUnsavedChanges", State.IsDirty);
        }
        catch (JSDisconnectedException)
        {
            // Blazor circuit disconnected — ignore
        }
    }

    public async ValueTask DisposeAsync()
    {
        State.OnStateChanged -= OnStateChanged;

        if (_isRegistered)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("dashboardInterop.unregisterBeforeUnload");
            }
            catch (JSDisconnectedException)
            {
                // Circuit already disconnected — ignore
            }
        }
    }
}
```

### Step 2: Place in Dashboard Page

The `UnsavedChangesGuard` is placed in `Dashboard.razor` (already referenced in UI-02 task):

```razor
@* At the bottom of Dashboard.razor *@
<UnsavedChangesGuard />
```

---

## How It Works

```
1. Dashboard page renders → UnsavedChangesGuard mounts
2. OnAfterRenderAsync registers JS beforeunload event
3. User adds/edits widgets → State.IsDirty = true
4. OnStateChanged callback → JS: window._dashboardHasUnsavedChanges = true
5. User tries to close tab/navigate away
6. Browser shows native "Leave site?" confirmation dialog
7. User exports config → State.MarkClean() → IsDirty = false
8. JS flag updated → no warning on close
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Adding a widget → closing tab shows browser confirmation dialog
- [ ] Exporting config → closing tab does NOT show confirmation
- [ ] Empty dashboard → no confirmation on close
- [ ] `JSDisconnectedException` is caught silently on dispose
- [ ] Component renders nothing visible (invisible guard)
- [ ] `DisposeAsync` properly unregisters the event listener
