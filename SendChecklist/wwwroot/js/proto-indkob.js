// PROTO indkøb: the draggable divider between the table and the details.
// Throwaway. The drag lives here, not in Blazor: a re-render per pointermove
// would be a render storm for what is only a layout change. Same behaviour
// as app.splitter in OnboardingChecklist.
const MIN = 340;       // the details are never narrower than this
const LIST_MIN = 520;  // nor the table
const STEP = 16;
const KEY = 'proto.indkob.detailWidth.3';
let off;

export function attach(handle, split) {
  off?.abort();
  if (!handle || !split) return;
  off = new AbortController();
  const signal = off.signal;

  const pane = handle.nextElementSibling;
  const max = () => Math.max(MIN, split.getBoundingClientRect().width - LIST_MIN);
  const clamp = w => Math.min(Math.max(w, MIN), max());
  const read = () => pane.getBoundingClientRect().width;
  const tell = () => {
    handle.setAttribute('aria-valuenow', String(Math.round(read())));
    handle.setAttribute('aria-valuemax', String(Math.round(max())));
  };
  // Until it is dragged, the width is the stylesheet's share of the page and
  // follows the window. A dragged width is fixed in pixels and remembered.
  let custom = false;
  const write = (w, save = true) => {
    custom = true;
    const c = Math.round(clamp(w));
    split.style.setProperty('--detail-w', c + 'px');
    tell();
    if (save) try { localStorage.setItem(KEY, String(c)); } catch { /* private mode */ }
  };
  const reset = () => {
    custom = false;
    split.style.removeProperty('--detail-w');
    try { localStorage.removeItem(KEY); } catch { /* private mode */ }
    tell();
  };

  let saved = NaN;
  try { saved = parseInt(localStorage.getItem(KEY), 10); } catch { /* private mode */ }
  if (Number.isFinite(saved)) write(saved, false); else tell();

  handle.addEventListener('pointerdown', e => {
    handle.setPointerCapture(e.pointerId);
    handle.setAttribute('data-dragging', '');
    document.body.style.userSelect = 'none';
    const move = ev => write(split.getBoundingClientRect().right - ev.clientX);
    const up = () => {
      handle.removeAttribute('data-dragging');
      document.body.style.userSelect = '';
      handle.removeEventListener('pointermove', move);
      handle.removeEventListener('pointerup', up);
      handle.removeEventListener('pointercancel', up);
    };
    handle.addEventListener('pointermove', move);
    handle.addEventListener('pointerup', up);
    handle.addEventListener('pointercancel', up);
    e.preventDefault();
  }, { signal });

  // Double-click puts the divider back where it started.
  handle.addEventListener('dblclick', reset, { signal });

  // The same range from the keyboard.
  handle.addEventListener('keydown', e => {
    const w = read();
    if (e.key === 'ArrowLeft') write(w + STEP);
    else if (e.key === 'ArrowRight') write(w - STEP);
    else if (e.key === 'Home') write(max());
    else if (e.key === 'End') write(MIN);
    else return;
    e.preventDefault();
  }, { signal });

  // A window resize can leave the saved width wider than the room left.
  window.addEventListener('resize', () => { if (custom) write(read(), false); else tell(); }, { signal });
}

export function detach() { off?.abort(); off = undefined; colsOff?.abort(); colsOff = undefined; }

// ---------------------------------------------------------------- columns
// The headings' dividing lines. One set of listeners on the table; widths are
// written straight onto the <th>, and kept per view.
const COL_MIN = 44, COL_MAX = 520, NAME_MIN = 200;
const colKey = view => 'proto.indkob.cols.' + view;
let colsOff, colsView;

const load = view => { try { return JSON.parse(localStorage.getItem(colKey(view))) || {}; } catch { return {}; } };
const save = (view, w) => { try { localStorage.setItem(colKey(view), JSON.stringify(w)); } catch { /* private mode */ } };

function fit(table) {
  // The table may not be squeezed below its fixed columns plus a readable name.
  let sum = NAME_MIN;
  table.querySelectorAll('thead th').forEach(th => { if (th.dataset.col !== 'name') sum += th.getBoundingClientRect().width; });
  table.style.minWidth = Math.ceil(sum) + 'px';
}

function apply(table, view) {
  const w = load(view);
  table.style.minWidth = '';
  table.querySelectorAll('thead th[data-col]').forEach(th => { th.style.width = w[th.dataset.col] ? w[th.dataset.col] + 'px' : ''; });
  fit(table);
}

export function cols(table, view) {
  if (!table) return;
  colsView = view;
  apply(table, view);
  if (colsOff) return;
  colsOff = new AbortController();
  const signal = colsOff.signal;

  const target = grip => grip.dataset.size === 'self' ? grip.closest('th') : grip.closest('th').nextElementSibling;
  const set = (th, px) => {
    const c = Math.round(Math.min(Math.max(px, COL_MIN), COL_MAX));
    th.style.width = c + 'px';
    const w = load(colsView); w[th.dataset.col] = c; save(colsView, w);
    fit(table);
  };

  table.addEventListener('pointerdown', e => {
    const grip = e.target.closest('.ib-grip'); if (!grip) return;
    const th = target(grip), sign = grip.dataset.size === 'self' ? 1 : -1;
    const x0 = e.clientX, w0 = th.getBoundingClientRect().width;
    grip.setPointerCapture(e.pointerId);
    grip.setAttribute('data-dragging', '');
    document.body.style.userSelect = 'none';
    const move = ev => set(th, w0 + sign * (ev.clientX - x0));
    const up = () => {
      grip.removeAttribute('data-dragging');
      document.body.style.userSelect = '';
      grip.removeEventListener('pointermove', move);
      grip.removeEventListener('pointerup', up);
      grip.removeEventListener('pointercancel', up);
    };
    grip.addEventListener('pointermove', move);
    grip.addEventListener('pointerup', up);
    grip.addEventListener('pointercancel', up);
    e.preventDefault();
  }, { signal });

  // A click that ends on a grip must not also pick the row under the heading.
  table.addEventListener('click', e => { if (e.target.closest('.ib-grip')) e.stopPropagation(); }, { signal });

  // Double-click gives the column its own width back.
  table.addEventListener('dblclick', e => {
    const grip = e.target.closest('.ib-grip'); if (!grip) return;
    const th = target(grip);
    th.style.width = '';
    const w = load(colsView); delete w[th.dataset.col]; save(colsView, w);
    fit(table);
  }, { signal });

  table.addEventListener('keydown', e => {
    const grip = e.target.closest?.('.ib-grip'); if (!grip) return;
    if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
    const th = target(grip), sign = grip.dataset.size === 'self' ? 1 : -1;
    set(th, th.getBoundingClientRect().width + sign * (e.key === 'ArrowRight' ? STEP : -STEP));
    e.preventDefault();
  }, { signal });
}
