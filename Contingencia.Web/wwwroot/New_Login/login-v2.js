/* Imperium · login v2 — comportamiento compartido (Login, Login3, LoginTUT, LoginLab).
   No reemplaza la lógica del servidor: solo anima, valida campos vacíos y muestra/oculta vistas. */
(function () {
'use strict';
const REDUCED = matchMedia('(prefers-reduced-motion: reduce)').matches;
const $ = id => document.getElementById(id);
const usr = document.querySelector('[data-role="usuario"]');
const pwd = document.querySelector('[data-role="clave"]');
const form = usr ? usr.form : null;

/* ---- estado que gobierna el campo de datos ---- */
const S = { captcha: !document.querySelector('.captcha-slot'), success: false, error: 0, focus: false };
function captchaDone() {
  const r = document.querySelector('textarea[name="g-recaptcha-response"],input[name="g-recaptcha-response"]');
  return r ? !!r.value : true;      // sin widget detectable no se condiciona la animación
}
if (document.querySelector('.captcha-slot')) setInterval(() => { S.captcha = captchaDone(); updateReady(); }, 500);
function target() {
  if (S.success) return 1;
  if (S.error > 0) return .06;
  return .06 + (usr && usr.value.trim() ? .26 : 0) + (pwd && pwd.value ? .26 : 0) + (S.captcha ? .24 : 0) + (S.focus ? .06 : 0);
}

/* ================= campo de datos: red de nodos que se ordena en retícula =================
   Optimizado: sin shadowBlur, enlaces agrupados por transparencia (4 trazos por cuadro), ~33 fps,
   resolución limitada y calidad adaptativa (si el equipo no da abasto, simplifica y al final se detiene). */
(function(){
  const cv=$('field'); if(!cv) return;
  const ctx=cv.getContext('2d');
  let W,H,pts=[],cols=0,rows=0,gap=100,order=.06,pulses=[],last=0,acc=[],cost=0;
  let quality=((navigator.hardwareConcurrency||4)<=4)?1:2;        // 2 completo · 1 reducido · 0 estático
  const mouse={x:-999,y:-999};
  addEventListener('pointermove',e=>{mouse.x=e.clientX;mouse.y=e.clientY;},{passive:true});
  document.addEventListener('pointerleave',()=>{mouse.x=mouse.y=-999;});

  function build(){
    const dpr=Math.min(devicePixelRatio||1,1.25);
    W=innerWidth;H=innerHeight;cv.width=Math.round(W*dpr);cv.height=Math.round(H*dpr);ctx.setTransform(dpr,0,0,dpr,0,0);
    gap=Math.max(84,Math.min(120,W/11));
    cols=Math.max(4,Math.round(W/gap));rows=Math.max(4,Math.round(H/gap));
    const gx=W/cols,gy=H/rows;pts=[];pulses=[];
    for(let r=0;r<rows;r++)for(let c=0;c<cols;c++){
      pts.push({hx:gx*(c+.5),hy:gy*(r+.5),ax:Math.random()*W,ay:Math.random()*H,
        px:Math.random()*6.28,py:Math.random()*6.28,fx:.12+Math.random()*.25,fy:.12+Math.random()*.25,
        amp:26+Math.random()*70,rad:1.3+Math.random()*1.4,delay:Math.random()*.3,x:0,y:0,o:0});
    }
  }
  const ease=x=>x<0?0:x>1?1:x*x*(3-2*x);
  function draw(t){
    order+=(target()-order)*(REDUCED?1:.06);
    ctx.clearRect(0,0,W,H);
    const n=pts.length, drift=quality>0?t:0;
    for(let i=0;i<n;i++){
      const p=pts[i],o=ease(order*1.32-p.delay);
      const nx=p.ax+Math.sin(drift*p.fx+p.px)*p.amp, ny=p.ay+Math.cos(drift*p.fy+p.py)*p.amp;
      let x=nx+(p.hx-nx)*o,y=ny+(p.hy-ny)*o;
      if(quality>1){const dx=x-mouse.x,dy=y-mouse.y,d2=dx*dx+dy*dy;if(d2<10000&&d2>0){const d=Math.sqrt(d2),f=(1-d/100)*(1-o*.7)*22;x+=dx/d*f;y+=dy/d*f;}}
      p.x=x;p.y=y;p.o=o;
    }
    /* vínculos: 4 niveles de transparencia, un trazo por nivel */
    const D=150+(gap*1.2-150)*Math.min(1,order*1.1),D2=D*D,B=[[],[],[],[]];
    for(let i=0;i<n;i++){
      const a=pts[i];
      for(let j=i+1;j<n;j++){
        const b=pts[j],dx=a.x-b.x,dy=a.y-b.y,d2=dx*dx+dy*dy;
        if(d2<D2){const k=(1-Math.sqrt(d2)/D)*(.16+.3*Math.min(a.o,b.o));B[Math.min(3,(k*10)|0)].push(a.x,a.y,b.x,b.y);}
      }
    }
    ctx.lineWidth=1;
    for(let l=0;l<4;l++){
      const s=B[l];if(!s.length)continue;
      ctx.strokeStyle='rgba(120,190,255,'+(.07+l*.09)+')';ctx.beginPath();
      for(let k=0;k<s.length;k+=4){ctx.moveTo(s[k],s[k+1]);ctx.lineTo(s[k+2],s[k+3]);}
      ctx.stroke();
    }
    if(quality>1&&mouse.x>0){
      ctx.strokeStyle='rgba(190,228,255,.32)';ctx.beginPath();
      for(let i=0;i<n;i++){const a=pts[i],dx=a.x-mouse.x,dy=a.y-mouse.y;if(dx*dx+dy*dy<28900){ctx.moveTo(a.x,a.y);ctx.lineTo(mouse.x,mouse.y);}}
      ctx.stroke();
    }
    /* nodos: halo suave + núcleo, cada uno en un solo relleno */
    ctx.fillStyle='rgba(110,185,255,.16)';ctx.beginPath();
    for(let i=0;i<n;i++){const a=pts[i],r=(a.rad+a.o*1.2)*2.6;ctx.moveTo(a.x+r,a.y);ctx.arc(a.x,a.y,r,0,6.283);}
    ctx.fill();
    ctx.fillStyle='rgba(185,225,255,.9)';ctx.beginPath();
    for(let i=0;i<n;i++){const a=pts[i],r=a.rad+a.o*1.2;ctx.moveTo(a.x+r,a.y);ctx.arc(a.x,a.y,r,0,6.283);}
    ctx.fill();
    /* pulsos de datos */
    if(quality>1&&!REDUCED&&n&&Math.random()<.03+order*.1&&pulses.length<14){
      const i=(Math.random()*n)|0,c=i%cols,r=(i/cols)|0;
      const j=Math.random()<.5?(c<cols-1?i+1:i-1):(r<rows-1?i+cols:i-cols);
      if(pts[j])pulses.push({i,j,t:0});
    }
    if(pulses.length){
      ctx.fillStyle='#fff';ctx.beginPath();
      for(let k=pulses.length-1;k>=0;k--){
        const p=pulses[k],a=pts[p.i],b=pts[p.j];p.t+=.04;
        if(!a||!b||p.t>=1){pulses.splice(k,1);continue;}
        const x=a.x+(b.x-a.x)*p.t,y=a.y+(b.y-a.y)*p.t;ctx.moveTo(x+2.4,y);ctx.arc(x,y,2.4,0,6.283);
      }
      ctx.fill();
    }
    if(S.error>0)S.error=Math.max(0,S.error-1/60);
  }
  function frame(now){
    requestAnimationFrame(frame);
    if(document.hidden)return;
    if(quality===0&&Math.abs(target()-order)<.002)return;               // estático y estable: no se dibuja
    if(now-last<(quality===2?30:quality===1?42:80))return;               // ~33 / ~24 / ~12 fps
    last=now;const t0=performance.now();draw(now/1000);cost=performance.now()-t0;
    if(quality>0){acc.push(cost);if(acc.length>=40){const avg=acc.reduce((s,v)=>s+v,0)/acc.length;acc.length=0;if(avg>8)quality--;}}
  }
  let rz;addEventListener('resize',()=>{clearTimeout(rz);rz=setTimeout(build,150);});
  document.fonts&&document.fonts.ready.then(build);
  build();requestAnimationFrame(frame);
})();


/* ---- formulario ---- */
const btn = form ? form.querySelector('.submit') : null;
function updateReady() { if (btn) btn.classList.toggle('ready', !!(usr && usr.value.trim() && pwd && pwd.value && S.captcha)); }
function setErr(input, text) {
  const m = document.querySelector('.msg[data-for="' + input.dataset.role + '"]');
  if (m) { m.textContent = text; m.classList.toggle('show', !!text); }
  input.setAttribute('aria-invalid', text ? 'true' : 'false');
}
document.querySelectorAll('.control input').forEach(i => {
  i.addEventListener('focus', () => S.focus = true);
  i.addEventListener('blur', () => S.focus = false);
  i.addEventListener('input', () => { S.error = 0; if (i.dataset.role) setErr(i, ''); updateReady(); });
});

const tg = document.querySelector('.toggle');
if (tg && pwd) tg.addEventListener('click', () => {
  const show = pwd.type === 'password';
  pwd.type = show ? 'text' : 'password';
  tg.setAttribute('aria-pressed', show);
  tg.setAttribute('aria-label', show ? 'Ocultar contraseña' : 'Mostrar contraseña');
  pwd.focus();
});
const caps = $('caps');
if (pwd && caps) {
  pwd.addEventListener('keyup', e => caps.classList.toggle('show', !!(e.getModifierState && e.getModifierState('CapsLock'))));
  pwd.addEventListener('blur', () => caps.classList.remove('show'));
}

/* validación mínima en el cliente; el resto lo decide el servidor (alerta()) */
if (form) form.addEventListener('submit', e => {
  /* solo se valida cuando envía el botón de inicio de sesión; "Olvidé mi clave", 2FA, etc. envían el mismo formulario con otros campos */
  const sub = e.submitter, ae = document.activeElement;
  if ((sub && btn && sub !== btn) || (ae && ae.closest && ae.closest('.modal-overlay'))) return;
  let bad = null;
  if (!usr.value.trim()) { setErr(usr, 'Ingrese su usuario.'); bad = bad || usr; }
  if (!pwd.value) { setErr(pwd, 'Ingrese su contraseña.'); bad = bad || pwd; }
  if (bad) { e.preventDefault(); S.error = 1; bad.focus(); return; }
  if (btn) {
    btn.setAttribute('aria-busy', 'true');                 // no se deshabilita: el botón debe viajar en el postback
    const t = btn.querySelector('.go-t'); if (t) t.textContent = 'Verificando credenciales…';
  }
  S.success = true;
});
addEventListener('pageshow', ev => {                         // volver con "atrás" no debe dejar el botón ocupado
  if (!ev.persisted) return;
  S.success = false;
  if (btn) { btn.removeAttribute('aria-busy'); const t = btn.querySelector('.go-t'); if (t) t.textContent = 'Iniciar sesión'; }
});

/* ---- vista de recuperación (solo Login.aspx) ---- */
(function () {
  const toRec = $('to-recover'), back = $('back-to-login'), rec = $('recoverform'), panel = document.querySelector('.panel-in');
  if (!toRec || !rec || !form) return;
  function swap(showRec) {
    form.hidden = showRec; rec.hidden = !showRec;
    panel.classList.toggle('recovering', showRec);
    const v = showRec ? rec : form; v.classList.remove('view-swap'); void v.offsetWidth; v.classList.add('view-swap');
    const f = showRec ? $('txtemail') : usr; if (f) f.focus();
  }
  toRec.addEventListener('click', () => swap(true));
  if (back) back.addEventListener('click', () => swap(false));
})();
/* ---- modales propios: Escape y clic fuera los cierran ---- */
function closeOverlay(m) { m.classList.remove('modal-show'); if (!document.querySelector('.modal-overlay.modal-show')) document.body.classList.remove('modal-open'); }
document.addEventListener('keydown', e => { if (e.key === 'Escape') { const m = document.querySelector('.modal-overlay.modal-show'); if (m) closeOverlay(m); } });
document.addEventListener('click', e => { const t = e.target; if (t.classList && t.classList.contains('modal-overlay') && t.classList.contains('modal-show')) closeOverlay(t); });
/* ---- enfoque del primer campo al abrir cualquier modal (con reintentos: el navegador puede estar ocupado dibujando) ---- */
document.querySelectorAll('.modal-overlay').forEach(m => {
  new MutationObserver(() => {
    if (!m.classList.contains('modal-show')) return;
    const f = () => { if (m.classList.contains('modal-show') && !m.contains(document.activeElement)) { const i = m.querySelector('input:not([type=hidden])'); if (i) i.focus(); } };
    [120, 450, 900].forEach(t => setTimeout(f, t));
  }).observe(m, { attributes: true, attributeFilter: ['class'] });
});
updateReady();
})();
