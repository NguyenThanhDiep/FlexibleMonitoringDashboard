// src/FlexibleMonitoringDashboard/wwwroot/js/interop.js

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
            if (window._dashboardHasUnsavedChanges) {
                e.preventDefault();
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
