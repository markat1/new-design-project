// The only JS in the project. Blazor has no layout-measurement API and no
// document-level key handling, so the picker chrome needs these three things.
// Everything below the picker is plain Blazor.
window.protoPicker = {
  _ref: null,
  _nav: null,
  _current: 0,

  init(nav, dotNetRef) {
    this._nav = nav;
    this._ref = dotNetRef;

    document.addEventListener('keydown', this._onKey);
    window.addEventListener('resize', this._onResize);

    // Enable the slide only after first paint, so load doesn't animate.
    requestAnimationFrame(() => requestAnimationFrame(() => nav.setAttribute('data-ready', '')));
  },

  dispose() {
    document.removeEventListener('keydown', this._onKey);
    window.removeEventListener('resize', this._onResize);
    this._ref = null;
    this._nav = null;
  },

  // Measures the active item and parks the highlight on it. Blazor never
  // renders a `style` attribute on the highlight, so this won't be diffed away.
  move(index) {
    const nav = this._nav;
    if (!nav) return;
    this._current = index;
    const items = nav.querySelectorAll('.proto-picker-item:not(.proto-picker-replay)');
    const el = items[index];
    if (!el) return;
    const highlight = nav.querySelector('.proto-picker-highlight');
    highlight.style.width = el.offsetWidth + 'px';
    highlight.style.transform = `translateX(${el.offsetLeft}px)`;
  },

  _onResize: () => window.protoPicker.move(window.protoPicker._current),

  _onKey: (e) => {
    const self = window.protoPicker;
    if (!self._ref) return;
    if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName) || e.target.isContentEditable) return;
    if (e.metaKey || e.ctrlKey || e.altKey) return;
    self._ref.invokeMethodAsync('HandleKey', e.key);
  },

  // Blazor's FocusAsync() needs an ElementReference per branch; one helper that
  // finds the first field (falling back to the container) is far less ceremony.
  focusFirst(container) {
    if (!container) return;
    const el = container.querySelector('input:not([type=hidden]), textarea');
    (el || container).focus();
  },
};
