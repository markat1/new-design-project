// PROTO: sliders for the Tjekliste's size and the table's text size. They write CSS variables on <html>
// directly, so dragging never re-renders Blazor. Copy gives the numbers.
window.ckDial = (() => {
  const dials = [
    { key: '--v3-w', label: 'Bredde', min: 640, max: 1920, step: 20, value: 860, unit: 'px', show: v => v >= 1920 ? 'fuld' : v + 'px', css: v => v >= 1920 ? '100%' : v + 'px' },
    { key: '--v3-pad', label: 'Luft om siden', min: 0, max: 64, step: 4, value: 48, unit: 'px' },
    { key: '--tbl-fs', label: 'Tabel, tekst', min: 13, max: 20, step: 1, value: 15, unit: 'px' },
    { key: '--v3-h', label: 'Åbent trin, højde', min: 0, max: 100, step: 5, value: 0, unit: 'vh', show: v => v == 0 ? 'efter indhold' : v + 'vh' },
  ];
  let panel;
  const root = document.documentElement;
  const fmt = (d, v) => d.show ? d.show(+v) : v + d.unit;
  const apply = (d, v) => root.style.setProperty(d.key, d.css ? d.css(+v) : v + d.unit);
  function mount() {
    if (panel) return;
    panel = document.createElement('aside');
    panel.className = 'dial'; panel.setAttribute('aria-label', 'Størrelse');
    panel.innerHTML = '<p class="dial-h">Størrelse</p>' + dials.map((d, i) => `
      <label class="dial-row" for="dial-${i}"><span>${d.label}</span><output id="dial-o-${i}">${fmt(d, d.value)}</output></label>
      <input id="dial-${i}" type="range" min="${d.min}" max="${d.max}" step="${d.step}" value="${d.value}">`).join('') +
      '<div class="dial-acts"><button type="button" data-reset>Nulstil</button><button type="button" data-copy>Kopiér tal</button></div>';
    document.body.append(panel);
    dials.forEach((d, i) => {
      const input = panel.querySelector('#dial-' + i), out = panel.querySelector('#dial-o-' + i);
      apply(d, d.value);
      input.addEventListener('input', () => { apply(d, input.value); out.textContent = fmt(d, input.value); });
    });
    panel.querySelector('[data-reset]').addEventListener('click', () => dials.forEach((d, i) => {
      const input = panel.querySelector('#dial-' + i); input.value = d.value; input.dispatchEvent(new Event('input'));
    }));
    panel.querySelector('[data-copy]').addEventListener('click', async e => {
      const text = dials.map((d, i) => `${d.label.toLowerCase()}: ${fmt(d, panel.querySelector('#dial-' + i).value)}`).join(' · ');
      try { await navigator.clipboard.writeText(text); } catch { }
      e.target.textContent = 'Kopieret'; setTimeout(() => e.target.textContent = 'Kopiér tal', 1500);
    });
  }
  function unmount() { panel?.remove(); panel = null; dials.forEach(d => root.style.removeProperty(d.key)); }
  return { mount, unmount };
})();
