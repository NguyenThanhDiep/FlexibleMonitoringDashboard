# Task UI-09: Export & Import Buttons

## Metadata
- **ID**: UI-09-export-import
- **Layer**: 3
- **Dependencies**: `FE-03`, `FE-06`
- **Blocks**: None
- **Parallel With**: UI-01, UI-07, UI-10

---

## Files Created

| File | Path |
|------|------|
| `ExportButton.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Configuration/ExportButton.razor` |
| `ImportButton.razor` | `src/FlexibleMonitoringDashboard.Client/Components/Configuration/ImportButton.razor` |

---

## Step-by-Step Implementation

### Step 1: Create `ExportButton.razor`

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Configuration/ExportButton.razor *@
@using FlexibleMonitoringDashboard.Client.Services
@inject ConfigurationService ConfigService
@inject DashboardStateService State
@inject ISnackbar Snackbar

<MudButton Variant="@Variant"
           Color="@Color"
           StartIcon="@Icons.Material.Filled.FileDownload"
           OnClick="Export"
           Size="@Size"
           Disabled="@(State.Config.Sections.Count == 0)">
    @ChildContent
</MudButton>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public Variant Variant { get; set; } = Variant.Text;
    [Parameter] public Color Color { get; set; } = Color.Inherit;
    [Parameter] public Size Size { get; set; } = Size.Small;

    private async Task Export()
    {
        try
        {
            await ConfigService.ExportAsync();
            Snackbar.Add("Configuration exported successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
    }
}
```

### Step 2: Create `ImportButton.razor`

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Configuration/ImportButton.razor *@
@using FlexibleMonitoringDashboard.Client.Services
@using Microsoft.AspNetCore.Components.Forms
@inject ConfigurationService ConfigService
@inject DashboardStateService State
@inject ISnackbar Snackbar
@inject IDialogService DialogService

<MudButton Variant="@Variant"
           Color="@Color"
           StartIcon="@Icons.Material.Filled.FileUpload"
           OnClick="OpenImportDialog"
           Size="@Size">
    @ChildContent
</MudButton>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public Variant Variant { get; set; } = Variant.Text;
    [Parameter] public Color Color { get; set; } = Color.Inherit;
    [Parameter] public Size Size { get; set; } = Size.Small;

    private async Task OpenImportDialog()
    {
        // Warn if there are unsaved changes
        if (State.IsDirty)
        {
            var confirm = await DialogService.ShowMessageBox(
                "Unsaved Changes",
                "You have unsaved changes. Importing a new configuration will replace the current dashboard. Continue?",
                yesText: "Import Anyway",
                cancelText: "Cancel");

            if (confirm != true) return;
        }

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<ImportDialog>("Import Configuration", options);
        await dialog.Result;
    }
}
```

### Step 3: Create `ImportDialog.razor`

```razor
@* src/FlexibleMonitoringDashboard.Client/Components/Configuration/ImportDialog.razor *@
@using FlexibleMonitoringDashboard.Client.Services
@using Microsoft.AspNetCore.Components.Forms
@inject ConfigurationService ConfigService
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center">
            <MudIcon Icon="@Icons.Material.Filled.FileUpload" Class="mr-2" />
            <MudText Typo="Typo.h6">Import Configuration</MudText>
        </MudStack>
    </TitleContent>
    <DialogContent>
        <MudStack Spacing="3">
            <MudText Typo="Typo.body2" Style="opacity: 0.7;">
                Upload a JSON configuration file to rebuild your dashboard instantly.
            </MudText>

            <MudFileUpload T="IBrowserFile"
                           Accept=".json"
                           FilesChanged="OnFileSelected"
                           MaximumFileCount="1">
                <ActivatorContent>
                    <MudPaper Elevation="0"
                              Class="d-flex align-center justify-center pa-8"
                              Style="border: 2px dashed; border-color: var(--mud-palette-lines-default); border-radius: 8px; cursor: pointer;">
                        <MudStack AlignItems="AlignItems.Center">
                            <MudIcon Icon="@Icons.Material.Filled.CloudUpload" Size="Size.Large"
                                     Style="opacity: 0.5;" />
                            <MudText Typo="Typo.body2">
                                Click to upload or drag a .json file here
                            </MudText>
                        </MudStack>
                    </MudPaper>
                </ActivatorContent>
            </MudFileUpload>

            @if (_selectedFile != null)
            {
                <MudAlert Severity="Severity.Info" Dense="true">
                    Selected: @_selectedFile.Name (@FormatSize(_selectedFile.Size))
                </MudAlert>
            }

            @if (_error != null)
            {
                <MudAlert Severity="Severity.Error" Dense="true">@_error</MudAlert>
            }

            @if (_isImporting)
            {
                <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
            }
        </MudStack>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="Import"
                   Disabled="@(_selectedFile == null || _isImporting)">
            Import
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private IBrowserFile? _selectedFile;
    private string? _error;
    private bool _isImporting;

    private void OnFileSelected(IBrowserFile file)
    {
        _selectedFile = file;
        _error = null;

        // Validate file type
        if (!file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            _error = "Please select a .json file.";
            _selectedFile = null;
        }

        // Validate file size (5 MB max)
        if (file.Size > 5 * 1024 * 1024)
        {
            _error = "File is too large (max 5 MB).";
            _selectedFile = null;
        }
    }

    private async Task Import()
    {
        if (_selectedFile == null) return;

        _isImporting = true;
        _error = null;
        StateHasChanged();

        try
        {
            using var stream = _selectedFile.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            var error = await ConfigService.ImportFromStreamAsync(stream);

            if (error != null)
            {
                _error = error;
            }
            else
            {
                Snackbar.Add("Configuration imported successfully!", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
        }
        catch (Exception ex)
        {
            _error = $"Import failed: {ex.Message}";
        }
        finally
        {
            _isImporting = false;
            StateHasChanged();
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
```

---

## Verification

- [ ] `dotnet build` succeeds
- [ ] Export button triggers JSON file download with dashboard config
- [ ] Export is disabled when dashboard is empty
- [ ] Import button opens file upload dialog
- [ ] Only `.json` files are accepted
- [ ] File size limit (5 MB) is enforced
- [ ] Successful import rebuilds the entire dashboard
- [ ] Import errors show a clear error message
- [ ] Unsaved changes warning shows before import overwrites
- [ ] Snackbar notifications for success/failure
