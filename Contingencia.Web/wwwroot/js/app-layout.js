(function () {
    var BREAKPOINT = 900;
    var esAngosto = function () { return window.matchMedia('(max-width: ' + BREAKPOINT + 'px)').matches; };
    var ultimoAngosto = null;

    function elementos() {
        return {
            shell: document.querySelector('.app-shell'),
            sidebar: document.querySelector('.app-sidebar'),
            backdrop: document.querySelector('.app-sidebar-backdrop'),
        };
    }

    function cerrar(el) {
        el.shell.classList.add('sidebar-closed');
        el.sidebar.style.marginLeft = '-' + el.sidebar.offsetWidth + 'px';
        el.backdrop?.classList.remove('visible');
    }

    function abrir(el) {
        el.shell.classList.remove('sidebar-closed');
        el.sidebar.style.marginLeft = '0';
        if (esAngosto()) el.backdrop?.classList.add('visible');
    }

    // Modo colapsado (solo íconos) del sidebar en escritorio — unificado con el botón
    // "Colapsar" de IMPERIUM 3. Persistido en localStorage para sobrevivir recargas; se
    // reaplica en cada enhanced navigation porque esos reemplazan el DOM del sidebar sin
    // volver a ejecutar este <script>.
    var CLAVE_COLAPSADO = 'contingenciaSidebarCollapsed';

    function aplicarColapsado() {
        var sidebar = document.querySelector('.app-sidebar');
        if (!sidebar) return;
        if (!esAngosto() && localStorage.getItem(CLAVE_COLAPSADO) === '1') {
            sidebar.classList.add('app-sidebar-collapsed');
        } else {
            sidebar.classList.remove('app-sidebar-collapsed');
        }
    }

    window.ImperiumLayout = {
        toggleSidebar: function () {
            var el = elementos();
            if (!el.shell || !el.sidebar) return;
            if (el.shell.classList.contains('sidebar-closed')) {
                abrir(el);
            } else {
                cerrar(el);
            }
        },
        closeSidebar: function () {
            var el = elementos();
            if (el.shell && el.sidebar) cerrar(el);
        },
        toggleCollapse: function () {
            if (esAngosto()) {
                // En móvil no hay modo "ícono": el botón de colapsar cierra el sidebar.
                this.toggleSidebar();
                return;
            }
            var sidebar = document.querySelector('.app-sidebar');
            if (!sidebar) return;
            var colapsado = sidebar.classList.toggle('app-sidebar-collapsed');
            localStorage.setItem(CLAVE_COLAPSADO, colapsado ? '1' : '0');
        }
    };

    // No dependemos solo del evento "change" de matchMedia (algunas herramientas de
    // emulación no lo disparan): en cada resize se vuelve a consultar el ancho real.
    function revisarBreakpoint() {
        var el = elementos();
        if (!el.shell || !el.sidebar) return;
        var angosto = esAngosto();
        if (angosto === ultimoAngosto) return;
        ultimoAngosto = angosto;
        if (angosto) {
            cerrar(el);
        } else {
            abrir(el);
        }
    }

    // Al navegar entre páginas con un <a href> del menú, Blazor usa "enhanced navigation":
    // reemplaza el <body> completo (incluido este layout) pero NUNCA re-ejecuta <script>, así
    // que este IIFE solo corre una vez, en la primera carga real. El servidor siempre renderiza
    // el sidebar en su estado HTML por defecto ("abierto", sin el margin-left/backdrop que pone
    // este archivo) — en pantallas angostas, cada navegación deja el sidebar montado encima del
    // contenido sin el fondo oscuro que lo acompaña, porque nada vuelve a aplicar el estado
    // "cerrado" sobre los nodos nuevos. revisarBreakpoint() por sí solo NO alcanza para este
    // caso: solo actúa cuando el ancho de pantalla CAMBIÓ de un breakpoint a otro, pero acá lo
    // que cambió fue el DOM (se reinició), no el ancho — por eso hay que forzar el estado
    // correcto sin esa comparación cada vez que Blazor termina de parchear la página.
    function aplicarEstadoActual() {
        var el = elementos();
        if (!el.shell || !el.sidebar) return;
        if (esAngosto()) {
            cerrar(el);
        } else {
            abrir(el);
        }
        aplicarColapsado();
    }

    window.addEventListener('resize', revisarBreakpoint);
    revisarBreakpoint();
    aplicarColapsado();

    // Este <script> queda ANTES que blazor.web.js en el HTML final (AppLayout.razor forma
    // parte de lo que <Routes /> renderiza, y ese tag va antes del <script blazor.web.js>
    // en App.razor) — verificado en vivo: "window.Blazor" todavía NO existe en el momento en
    // que este IIFE corre. El guard "if (window.Blazor && ...)" de abajo evaluaba falso
    // SIEMPRE, en cada carga real, así que el enganche a "enhancedload" nunca llegaba a
    // registrarse — ni una sola vez. Por eso se registra recién en "DOMContentLoaded": para
    // ese momento el documento entero ya se parseó, incluidos los <script> que vienen
    // después de este (blazor.web.js entre ellos), así que "window.Blazor" ya existe.
    function registrarEnhancedload() {
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', aplicarEstadoActual);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', registrarEnhancedload);
    } else {
        registrarEnhancedload();
    }
})();
