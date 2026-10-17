// Animaciones y navegación de Opciones.razor (migrado del <script> de Opciones.aspx, diseño 2026-10-13).
// Módulo ES: Opciones.razor lo carga con import() y llama init() tras el primer render y dispose()
// al salir. Se hace así (y no con <script> embebido) porque Blazor no ejecuta scripts de componentes.
// La distribución de la rejilla (--cols/--rows/dense) la calcula el componente en C#, no este módulo.

let limpiar = [];

export function init() {
    dispose();

    const grid = document.getElementById('grid');
    if (!grid) return;

    const REDUCED = matchMedia('(prefers-reduced-motion: reduce)').matches;
    const BASE = document.documentElement.classList.contains('nb') ? 100 : 1050;
    const cards = [...grid.querySelectorAll('.card')];
    const on = (target, type, fn, opts) => {
        target.addEventListener(type, fn, opts);
        limpiar.push(() => target.removeEventListener(type, fn, opts));
    };
    const timeout = (fn, ms) => { const id = setTimeout(fn, ms); limpiar.push(() => clearTimeout(id)); };

    // tras la entrada, habilita el tilt 3D
    cards.forEach((c, i) => timeout(() => c.classList.add('ready'), BASE + i * 90 + 850));

    on(grid, 'pointermove', e => {
        const c = e.target.closest('.card');
        if (!c || REDUCED) return;
        const r = c.getBoundingClientRect(), x = e.clientX - r.left, y = e.clientY - r.top;
        c.style.setProperty('--px', x + 'px');
        c.style.setProperty('--py', y + 'px');
        c.style.setProperty('--ry', ((x / r.width - .5) * 9) + 'deg');
        c.style.setProperty('--rx', ((.5 - y / r.height) * 9) + 'deg');
    });
    on(grid, 'pointerout', e => {
        const c = e.target.closest('.card');
        if (c) { c.style.setProperty('--rx', '0deg'); c.style.setProperty('--ry', '0deg'); }
    });

    rayos(REDUCED);
    red(REDUCED, on);
    escritura(REDUCED, BASE, timeout);
    navegacion(grid, REDUCED, on, timeout);

    on(window, 'pointermove', e => {
        const s = document.documentElement.style;
        s.setProperty('--mx', e.clientX + 'px');
        s.setProperty('--my', e.clientY + 'px');
    }, { passive: true });
}

export function dispose() {
    limpiar.forEach(f => { try { f(); } catch { /* ya liberado */ } });
    limpiar = [];
}

// rayos de datos
function rayos(REDUCED) {
    const b = document.getElementById('str');
    if (REDUCED || !b) return;
    b.textContent = '';
    for (let i = 0; i < 16; i++) {
        const e = document.createElement('i');
        e.style.cssText = `left:${Math.random() * 100}%;height:${60 + Math.random() * 140}px;animation-duration:${3 + Math.random() * 5}s;animation-delay:${-Math.random() * 8}s`;
        b.appendChild(e);
    }
}

// red de nodos con pulsos de datos
function red(REDUCED, on) {
    const cv = document.getElementById('net');
    if (REDUCED || !cv) return;
    const ctx = cv.getContext('2d');
    let W, H, N = [], pulses = [], raf = 0, activo = true;
    const m = { x: -999, y: -999 };
    on(window, 'pointermove', e => { m.x = e.clientX; m.y = e.clientY; }, { passive: true });
    function size() {
        const r = Math.min(devicePixelRatio || 1, 2);
        W = innerWidth; H = innerHeight; cv.width = W * r; cv.height = H * r; ctx.setTransform(r, 0, 0, r, 0, 0);
        N = Array.from({ length: Math.min(90, Math.floor(W * H / 24000)) }, () => ({ x: Math.random() * W, y: Math.random() * H * .8, vx: (Math.random() - .5) * .4, vy: (Math.random() - .5) * .4, r: 1 + Math.random() * 1.6 }));
        pulses = [];
    }
    size();
    on(window, 'resize', size);
    (function loop() {
        if (!activo) return;
        ctx.clearRect(0, 0, W, H);
        for (const n of N) { n.x += n.vx; n.y += n.vy; if (n.x < 0 || n.x > W) n.vx *= -1; if (n.y < 0 || n.y > H * .85) n.vy *= -1; }
        const L = [];
        for (let i = 0; i < N.length; i++) for (let j = i + 1; j < N.length; j++) {
            const d = Math.hypot(N[i].x - N[j].x, N[i].y - N[j].y);
            if (d < 150) { L.push([i, j]); ctx.strokeStyle = `rgba(10,120,200,${.28 * (1 - d / 150)})`; ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(N[i].x, N[i].y); ctx.lineTo(N[j].x, N[j].y); ctx.stroke(); }
        }
        for (const n of N) {
            const dm = Math.hypot(n.x - m.x, n.y - m.y);
            if (dm < 190) { ctx.strokeStyle = `rgba(10,110,200,${.5 * (1 - dm / 190)})`; ctx.beginPath(); ctx.moveTo(n.x, n.y); ctx.lineTo(m.x, m.y); ctx.stroke(); }
            ctx.fillStyle = 'rgba(10,130,210,.75)'; ctx.shadowColor = '#0a8fd1'; ctx.shadowBlur = 5; ctx.beginPath(); ctx.arc(n.x, n.y, n.r, 0, 6.3); ctx.fill();
        }
        ctx.shadowBlur = 0;
        if (L.length && Math.random() < .12 && pulses.length < 26) pulses.push({ l: L[Math.floor(Math.random() * L.length)], t: 0 });
        for (let k = pulses.length - 1; k >= 0; k--) {
            const p = pulses[k], a = N[p.l[0]], b = N[p.l[1]]; p.t += .022;
            if (p.t >= 1) { pulses.splice(k, 1); continue; }
            const x = a.x + (b.x - a.x) * p.t, y = a.y + (b.y - a.y) * p.t;
            ctx.fillStyle = '#0a8fd1'; ctx.shadowColor = '#0a8fd1'; ctx.shadowBlur = 8; ctx.beginPath(); ctx.arc(x, y, 2, 0, 6.3); ctx.fill();
        }
        ctx.shadowBlur = 0;
        raf = requestAnimationFrame(loop);
    })();
    limpiar.push(() => { activo = false; cancelAnimationFrame(raf); });
}

// escritura + reloj
function escritura(REDUCED, BASE, timeout) {
    const t = document.getElementById('typed'), c = document.getElementById('clk');
    if (!t || !c) return;
    const msg = 'Selecciona una aplicación para continuar';
    let i = 0;
    t.textContent = '';
    if (REDUCED) t.textContent = msg;
    else timeout(function k() { t.textContent = msg.slice(0, ++i); if (i < msg.length) timeout(k, 38); }, BASE + 50);
    function tick() {
        const d = new Date();
        c.textContent = d.toLocaleDateString('es-CO', { weekday: 'short', day: '2-digit', month: 'short' }) + ' · ' + d.toLocaleTimeString('es-CO', { hour12: false });
    }
    tick();
    const id = setInterval(tick, 1000);
    limpiar.push(() => clearInterval(id));
}

// clic: onda + transición de pantalla; teclas 1-9
function navegacion(grid, REDUCED, on, timeout) {
    const wipe = document.getElementById('wipe');
    function go(card, x, y) {
        const url = card.getAttribute('href');
        const r = card.getBoundingClientRect(), sz = Math.max(r.width, r.height) * 2;
        const d = document.createElement('span');
        d.className = 'rip';
        d.style.cssText = `width:${sz}px;height:${sz}px;left:${x - r.left - sz / 2}px;top:${y - r.top - sz / 2}px`;
        card.appendChild(d);
        timeout(() => d.remove(), 750);
        if (!url || url === '#') return;
        wipe.style.setProperty('--wx', x + 'px');
        wipe.style.setProperty('--wy', y + 'px');
        if (REDUCED) { location.href = url; return; }
        wipe.style.clipPath = `circle(0 at ${x}px ${y}px)`;
        wipe.getBoundingClientRect();
        wipe.classList.add('on');
        timeout(() => location.href = url, 520);
        timeout(() => wipe.classList.remove('on'), 1800);
    }
    on(grid, 'click', e => {
        const c = e.target.closest('.card');
        if (!c || e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) return;
        e.preventDefault();
        go(c, e.clientX || innerWidth / 2, e.clientY || innerHeight / 2);
    });
    on(window, 'keydown', e => {
        if (e.ctrlKey || e.metaKey || e.altKey || /INPUT|TEXTAREA/.test(e.target.tagName)) return;
        const n = parseInt(e.key, 10), cards = document.querySelectorAll('#grid .card');
        if (n >= 1 && n <= cards.length) { const c = cards[n - 1], r = c.getBoundingClientRect(); c.focus(); go(c, r.left + r.width / 2, r.top + r.height / 2); }
    });
}
