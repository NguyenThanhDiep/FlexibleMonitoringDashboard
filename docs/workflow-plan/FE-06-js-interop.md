# Task FE-06: JavaScript Interop Helpers

## Metadata
- **ID**: FE-06-js-interop
- **Layer**: 1
- **Dependencies**: `01-project-scaffolding`
- **Blocks**: `UI-09`, `UI-10`
- **Parallel With**: BE-01, BE-02, BE-03, FE-01, FE-04, FE-05, UI-12, DOC-*

---

## Files Created

| File | Path |
|------|------|
| `interop.js` | `src/FlexibleMonitoringDashboard.Client/wwwroot/js/interop.js` |

---

## Step-by-Step Implementation

### Step 1: Create `interop.js`

This JavaScript file provides browser-specific functionality that Blazor WASM cannot do natively:
1. **File download** — create a Blob and trigger download (no server roundtrip)
2. **Beforeunload warning** — prompt user when closing/navigating away with unsaved changes
3. **File read** — read uploaded file contents as text

```javascript
// src/FlexibleMonitoringDashboard.Client/wwwroot/js/interop.js

window.dashboardInterop = {

    /**
     * Triggers a file download in the browser from base64 content.
     * @param {string} fileName - Name of the file to download.
     * @param {string} base64Content - Base64-encoded file content.
     * @param {string} mimeType - MIME type (e.g., "application/json").
     */
    downloadFile: function (fileName, base64Content, mimeType) {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: mimeType });

        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    },

    /**
     * Registers the beforeunload event to warn user about unsaved changes.
     * @param {object} dotNetRef - .NET object reference for callback.
     */
    registerBeforeUnload: function (dotNetRef) {
        window._dashboardDotNetRef = dotNetRef;
        window._beforeUnloadHandler = function (e) {
            // Call .NET to check if there are unsaved changes
            // Since this is synchronous, we use a flag that .NET sets
            if (window._dashboardHasUnsavedChanges) {
                e.preventDefault();
                // Modern browsers require returnValue to be set
                e.returnValue = 'You have unsaved changes. Export your configuration before leaving?';
                return e.returnValue;
            }
        };
        window.addEventListener('beforeunload', window._beforeUnloadHandler);
    },

    /**
     * Updates the unsaved changes flag.
     * @param {boolean} hasChanges - Whether there are unsaved changes.
     */
    setUnsavedChanges: function (hasChanges) {
        window._dashboardHasUnsavedChanges = hasChanges;
    },

    /**
     * Unregisters the beforeunload event.
     */
    unregisterBeforeUnload: function () {
        if (window._beforeUnloadHandler) {
            window.removeEventListener('beforeunload', window._beforeUnloadHandler);
            window._beforeUnloadHandler = null;
        }
        window._dashboardDotNetRef = null;
        window._dashboardHasUnsavedChanges = false;
    },

    /**
     * Reads a file as text from an input element.
     * @param {object} inputElement - The file input element reference.
     * @returns {Promise<string>} The file content as text.
     */
    readFileAsText: function (inputElement) {
        return new Promise((resolve, reject) => {
            const file = inputElement.files[0];
            if (!file) {
                reject('No file selected.');
                return;
            }

            const reader = new FileReader();
            reader.onload = function (e) {
                resolve(e.target.result);
            };
            reader.onerror = function () {
                reject('Failed to read file.');
            };
            reader.readAsText(file);
        });
    },

    /**
     * Scrolls to an element by ID.
     * @param {string} elementId - The DOM element ID to scroll to.
     */
    scrollToElement: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
};
```

### Step 2: Ensure Script Reference in `App.razor`

The script tag should already be in `App.razor` from the scaffolding task:

```html
<script src="js/interop.js"></script>
```

If using the Client project's `wwwroot/`, the path might need to be:

```html
<script src="_content/FlexibleMonitoringDashboard.Client/js/interop.js"></script>
```

> **Note**: Test both paths. In a Blazor Web App with WASM, static files from the `.Client` project are served under `_content/{AssemblyName}/`. However, if the file is in the server's `wwwroot/`, it's served directly.

**Recommended approach**: Place `interop.js` in the **server** project's `wwwroot/js/` instead, so the path is simply `js/interop.js`. Or copy during build.

---

## C# JSInterop Usage Examples

### Download a file:
```csharp
await JSRuntime.InvokeVoidAsync("dashboardInterop.downloadFile",
    "dashboard.json", base64Content, "application/json");
```

### Register beforeunload:
```csharp
await JSRuntime.InvokeVoidAsync("dashboardInterop.registerBeforeUnload",
    DotNetObjectReference.Create(this));
```

### Update unsaved changes flag:
```csharp
await JSRuntime.InvokeVoidAsync("dashboardInterop.setUnsavedChanges", isDirty);
```

### Unregister on dispose:
```csharp
await JSRuntime.InvokeVoidAsync("dashboardInterop.unregisterBeforeUnload");
```

---

## Verification

- [ ] `interop.js` loads without syntax errors in browser console
- [ ] `downloadFile()` triggers a file download with correct content
- [ ] `registerBeforeUnload()` shows browser confirmation when `_dashboardHasUnsavedChanges` is true
- [ ] `setUnsavedChanges(false)` suppresses the warning
- [ ] `unregisterBeforeUnload()` removes the event listener cleanly
- [ ] `readFileAsText()` resolves with file content for a `.json` file
- [ ] `scrollToElement()` smoothly scrolls to the target element
