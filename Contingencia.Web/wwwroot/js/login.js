function alerta(msg) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg,
        confirmButtonText: "Ok",
        showCancelButton: false
    });
}

// El cambio entre "Iniciar sesión" y "Recuperar contraseña" (#to-recover / #back-to-login) lo hace ahora
// New_Login/login-v2.js (diseño nuevo); aquí solo quedan la carga del captcha y alerta().
// Mismo problema y misma solución que Recobros con reCAPTCHA (ver el login.js de
// Imperium.Web para la explicación larga): el widget de Cloudflare Turnstile se renderiza
// una sola vez de forma explícita (turnstile.render), no automática, porque una enhanced
// navigation de Blazor reutiliza el mismo nodo del DOM en vez de reemplazarlo — volver a
// renderizar sobre el mismo nodo lanza error. El contenedor lleva "data-permanent"
// (Login.razor) para que Blazor lo deje intacto entre navegaciones.
function initTurnstile() {
    var contenedor = document.getElementById('turnstile-container');
    if (!contenedor || contenedor.childElementCount > 0) return;

    var siteKey = contenedor.getAttribute('data-sitekey');
    if (!siteKey) return;

    if (typeof turnstile !== 'undefined' && turnstile.render) {
        turnstile.render(contenedor, { sitekey: siteKey });
        return;
    }

    cargarScriptTurnstile();
}

// Igual que con reCAPTCHA: el <script> se inserta vía DOM (no como <script src> fijo en el
// markup) para garantizar que el navegador lo ejecute sin importar si la carga fue una
// recarga real o un parche de enhanced navigation.
function cargarScriptTurnstile() {
    if (document.getElementById('contingencia-turnstile-script')) return;
    var script = document.createElement('script');
    script.id = 'contingencia-turnstile-script';
    script.src = 'https://challenges.cloudflare.com/turnstile/v0/api.js?onload=onTurnstileCargado&render=explicit';
    script.async = true;
    script.defer = true;
    document.head.appendChild(script);
}

function onTurnstileCargado() {
    initTurnstile();
}

function initLogin() {
    initTurnstile();
}

// Mismo motivo que en Recobros: este archivo se carga desde App.razor (global, todas las
// páginas), no desde un <script src="js/login.js"> dentro de Login.razor — un <script>
// propio de esa página nunca se ejecutaría si el usuario llega a /login por primera vez en
// su pestaña vía enhanced navigation (ver login.js de Imperium.Web para el detalle
// completo). Viviendo en App.razor corre una sola vez de verdad, y desde ahí el listener de
// "enhancedload" queda armado para el resto de la sesión.
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initLogin);
} else {
    initLogin();
}

if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
    window.Blazor.addEventListener('enhancedload', initLogin);
}
