// Blazor's FocusAsync() needs an ElementReference per field; one helper that
// finds the first input in a container is far less ceremony.
window.app = {
  focusFirst(container) {
    if (!container) return;
    const el = container.querySelector('input:not([type=hidden]), textarea');
    (el || container).focus();
  },
};
