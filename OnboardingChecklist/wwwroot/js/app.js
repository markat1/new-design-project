// Blazor's FocusAsync() needs an ElementReference per field; one helper that
// finds the first input in a container is far less ceremony.
window.app = {
  focusFirst(container) {
    if (!container) return;
    const el = container.querySelector('input:not([type=hidden]), textarea');
    (el || container).focus({ preventScroll: true });
  },

  // First control in a panel that just opened — buttons included, unlike
  // focusFirst, which is for forms.
  focusIn(container) {
    if (!container) return;
    const el = container.querySelector('button, input, textarea, [tabindex="0"]');
    (el || container).focus({ preventScroll: true });
  },

  focusId(id) {
    document.getElementById(id)?.focus({ preventScroll: true });
  },

  // The workbook is built in C# with the Open XML SDK; this only hands the
  // bytes to the browser as a file. Blazor sends byte[] over as a Uint8Array.
  saveFile(name, bytes, type) {
    const url = URL.createObjectURL(new Blob([bytes], { type }));
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    a.click();
    URL.revokeObjectURL(url);
  },

    // The drag lives in JS, not Blazor: a re-render per pointermove would be a
  // render storm for something that is only ever a layout change.
  splitter: {
    attach(handle, split, key) {
      if (!handle || !split) return;

      const MIN = 280;        // detail pane never narrower than this
      const LIST_MIN = 660;   // matches .t's min-width, so the table never clips
      const STEP = 16;
      const KEY = key || 'sent.detailWidth';

      const max = () => Math.max(MIN, split.getBoundingClientRect().width - LIST_MIN);
      const clamp = w => Math.min(Math.max(w, MIN), max());

      const read = () => {
        const v = parseInt(getComputedStyle(split).getPropertyValue('--detail-w'), 10);
        return Number.isFinite(v) ? v : 400;
      };

      const write = w => {
        const c = Math.round(clamp(w));
        split.style.setProperty('--detail-w', c + 'px');
        handle.setAttribute('aria-valuenow', String(c));
        try { localStorage.setItem(KEY, String(c)); } catch { /* private mode */ }
      };

      try {
        const saved = parseInt(localStorage.getItem(KEY), 10);
        if (Number.isFinite(saved)) write(saved);
      } catch { /* private mode */ }

      handle.addEventListener('pointerdown', e => {
        handle.setPointerCapture(e.pointerId);
        handle.setAttribute('data-dragging', '');

        const move = ev => write(split.getBoundingClientRect().right - ev.clientX);
        const up = ev => {
          handle.releasePointerCapture(ev.pointerId);
          handle.removeAttribute('data-dragging');
          handle.removeEventListener('pointermove', move);
          handle.removeEventListener('pointerup', up);
        };

        handle.addEventListener('pointermove', move);
        handle.addEventListener('pointerup', up);
        e.preventDefault();
      });

      // Dragging with a mouse is not an option for everyone, so the same range
      // is reachable from the keyboard.
      handle.addEventListener('keydown', e => {
        const w = read();
        if (e.key === 'ArrowLeft') write(w + STEP);
        else if (e.key === 'ArrowRight') write(w - STEP);
        else if (e.key === 'Home') write(max());
        else if (e.key === 'End') write(MIN);
        else return;
        e.preventDefault();
      });

      // A window resize can leave the saved width wider than the room left.
      window.addEventListener('resize', () => write(read()));
    },
  },
};
