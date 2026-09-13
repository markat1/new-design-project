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

  // A search field takes focus on a mouse, where it saves a click, and not on
  // touch, where it would throw the keyboard up the screen unasked.
  focusSearch(container) {
    if (!container || !matchMedia('(pointer: fine)').matches) return;
    container.querySelector('input')?.focus({ preventScroll: true });
  },

  // Held back a frame on purpose. This is how a panel hands focus to the
  // control that opened it, and the key that closed the panel is often Enter
  // — put the focus on a button inside that same keypress and the button
  // catches its activation, so the panel opens right back up.
  focusId(id) {
    requestAnimationFrame(() => document.getElementById(id)?.focus({ preventScroll: true }));
  },

  // The letter's toolbar. Wired once for the whole document, so a re-render of
  // the form needs nothing from Blazor. Every change goes through
  // execCommand('insertText'): the browser records it for Ctrl+Z, and fires
  // the input event Blazor's @oninput is already listening for.
  editor: (() => {
    let lastField = 'f-body';

    function edit(field, change) {
      if (!field) return;
      const [from, to, text, selFrom, selTo] = change(field.value, field.selectionStart, field.selectionEnd);
      field.focus();
      field.setSelectionRange(from, to);
      if (!document.execCommand('insertText', false, text)) {
        field.setRangeText(text, from, to, 'end');
        field.dispatchEvent(new Event('input', { bubbles: true }));
      }
      field.setSelectionRange(selFrom, selTo);
    }

    const wrap = (field, mark) => edit(field, (v, a, z) => {
      const picked = v.slice(a, z) || (mark === '**' ? 'fed tekst' : 'kursiv');
      return [a, z, mark + picked + mark, a + mark.length, a + mark.length + picked.length];
    });

    // Every line the selection touches, on or off together.
    const list = field => edit(field, (v, a, z) => {
      const start = v.lastIndexOf('\n', a - 1) + 1;
      const end = v.indexOf('\n', z);
      const stop = end === -1 ? v.length : end;
      const lines = v.slice(start, stop).split('\n');
      const off = lines.every(l => l.startsWith('- '));
      const block = lines.map(l => off ? l.slice(2) : '- ' + l).join('\n');
      return [start, stop, block, start, start + block.length];
    });

    const insert = (field, token) => edit(field, (v, a, z) => [a, z, token, a + token.length, a + token.length]);

    document.addEventListener('focusin', e => {
      if (e.target.matches?.('[data-ed-field]')) lastField = e.target.id;
    });

    // A toolbar click must not take the caret out of the text it acts on.
    document.addEventListener('mousedown', e => {
      if (e.target.closest?.('.ed-b')) e.preventDefault();
    });

    document.addEventListener('click', e => {
      const b = e.target.closest?.('.ed-b');
      if (!b) return;
      const body = document.getElementById('f-body');
      if (b.dataset.wrap) wrap(body, b.dataset.wrap);
      else if (b.hasAttribute('data-list')) list(body);
      else if (b.dataset.insert) insert(document.getElementById(lastField) || body, b.dataset.insert);
    });

    document.addEventListener('keydown', e => {
      if (e.target.id !== 'f-body' || !(e.ctrlKey || e.metaKey) || e.altKey) return;
      const key = e.key.toLowerCase();
      if (key !== 'b' && key !== 'i') return;
      e.preventDefault();
      wrap(e.target, key === 'b' ? '**' : '_');
    });

    return {};
  })(),

  // A native <dialog>, opened modal: the focus trap, Escape and the top layer
  // come with showModal(), and closing hands focus back to whatever opened it.
  // The backdrop and the dialog's own padding are both the <dialog> as a click
  // target, so only a click outside its box counts as a click on the backdrop.
  dialog: {
    open(el) {
      if (!el) return;
      if (!el.dataset.wired) {
        el.dataset.wired = '1';
        el.addEventListener('click', e => {
          if (e.target !== el) return;
          const r = el.getBoundingClientRect();
          if (e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom) el.close();
        });
      }
      if (!el.open) el.showModal();
    },
    close(el) {
      if (el?.open) el.close();
    },
  },

  // The arrow keys walk a hundred-row list, and the row they are standing on
  // has to stay in sight — 'nearest' scrolls the list, never the page.
  showRow(id) {
    document.getElementById(id)?.scrollIntoView({ block: 'nearest' });
  },

  // The tab underline travels from the tab you left to the one you opened. It
  // is measured here because only the browser knows how wide a label is; the
  // move itself is a CSS transition on transform, so nothing renders per frame.
  tabs: {
    mark(bar) {
      if (!bar) return;

      const place = () => {
        const open = bar.querySelector('[aria-selected="true"]');
        if (!open) return;

        bar.style.setProperty('--tab-x', open.offsetLeft + 'px');
        bar.style.setProperty('--tab-w', open.offsetWidth);
      };

      // A tab changes width when its count appears or grows, and the line has
      // to follow — measuring only on a tab switch left it pointing at a width
      // the tab no longer had.
      if (!bar.dataset.watched) {
        bar.dataset.watched = '1';
        new ResizeObserver(place).observe(bar);
      }

      place();
    },
  },

  // Column widths are dragged in the sheet's own header, the way Excel does
  // it. The drag lives here rather than in Blazor: a render per pointermove
  // would be a render storm for what is only a column getting wider. One call
  // goes back at the end, so the width lands in the workbook and in the file.
  sheet: {
    attach(grid, ref) {
      if (!grid || grid.dataset.resizable) return;
      grid.dataset.resizable = '1';

      let drag = null;
      const widthOf = index => grid.querySelectorAll('.xl-head td')[index + 1]?.getBoundingClientRect().width ?? 0;

      grid.addEventListener('pointerdown', e => {
        const grip = e.target.closest('.xl-grip');
        if (!grip) return;

        const index = Number(grip.dataset.col);
        const col = grid.querySelectorAll('col')[index + 1];   // the row numbers come first
        if (!col) return;

        drag = { index, col, from: e.clientX, was: widthOf(index) };
        grip.setPointerCapture(e.pointerId);
        e.preventDefault();
      });

      grid.addEventListener('pointermove', e => {
        if (!drag) return;
        drag.col.style.width = Math.max(24, Math.round(drag.was + e.clientX - drag.from)) + 'px';
      });

      grid.addEventListener('pointerup', () => {
        if (!drag) return;
        ref.invokeMethodAsync('ColumnResized', drag.index, parseInt(drag.col.style.width, 10) || Math.round(drag.was));
        drag = null;
      });
    },
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
