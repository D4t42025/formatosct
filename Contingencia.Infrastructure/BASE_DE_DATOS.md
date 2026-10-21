# Base de datos usada por el módulo Formato de Contingencia (Contingencia.Web)

Este documento lista **todo lo que la migración a Blazor toca hoy en SQL Server**
(base `DQUALITY`), siguiendo la misma disciplina que
`Imperium 2026 core/Imperium.Infrastructure/BASE_DE_DATOS.md` (el documento
equivalente para el módulo Recobros, ya migrado). Se completa a medida que se
migra cada página — **7 páginas ya están migradas**: las 6 del flujo Mipres/
Formato de Contingencia (Opciones, Login, Inicio, Cargar formato, Consultar
formato con su modal de Gestionar, y Buzón PQR) más el módulo **Junta
Profesional** (rol 60, integrado 2026-09-02) — ver "## Resumen ejecutivo"
para la lista consolidada con rutas, y "Estado actual" abajo para el detalle
página por página.

> **Por qué existe un repo/solución separada de Recobros**: se decidió
> explícitamente NO usar la misma solución que Recobros (`Imperium.Core.slnx`)
> porque este módulo necesita versionamiento independiente y usa un
> master/layout distinto al de Recobros (aunque la lógica de negocio interna
> es similar en varios puntos). Lo único que se **duplica a propósito** en vez
> de compartir vía librería es la plomería de autenticación/sesión (ver
> "Relación con Recobros" abajo) — es poco código, ya está probado, y evita
> acoplar el ciclo de release de los dos módulos.

---

## Resumen ejecutivo (recopilado 2026-09-02, actualizado 2026-09-03 con los modales de Cargas/Firma, 2026-09-29 con Firma en el acta y la página Profesionales, y 2026-09-29 con el wizard "Nueva Contingencia")

Consolidado de todo lo migrado hasta ahora, para no tener que leer el
documento completo cada vez — el detalle línea por línea de cada punto está
en las secciones de abajo. Verificado por script automático (`grep` de
todos los `sp_*`/tablas invocados desde C# contra lo documentado aquí): **0
objetos usados sin documentar, 0 objetos documentados sin usar**.

**Módulos/páginas migrados (8, cubriendo 2 flujos independientes)**:

| # | Página | Ruta | Legado | Roles que la usan |
|---|---|---|---|---|
| 1 | Opciones | `/` | `Opciones.aspx` | Todos (pública) |
| 2 | Login | `/login` | `Login3.aspx` | Todos |
| 3 | Inicio | `/inicio` | `JuntasFormatoCnt.aspx` | 601 |
| 4 | Cargar formato | `/formatos/cargar` | `JuntasFormatoCntC.aspx` | 601 |
| 5 | Consultar formato (+ modal Gestionar) | `/formatos/consultar` | `JuntasFormatoCntCO.aspx` + `JuntasFormatoCntCO2.aspx` | 601 (consulta propia), 27/29/30 (consulta + gestión de toda la Cía) |
| 6 | Buzón PQR | `/sugerencias` | `Sugerencias.aspx` | Todos los autenticados |
| 7 | Junta Profesional | `/juntas` | `JuntaProfesional.aspx` | 60 |
| 8 | Profesionales | `/profesionales` | `MIP_PROFESIONALES.aspx` | 60 |

(8 páginas Razor — la numeración de "Página 1..10" en las secciones de abajo
cuenta los modales de Gestionar, Cargas y Firma como entradas aparte por su
alcance propio, aunque viven dentro de las páginas 5 y 8, no son rutas
nuevas).

**Tablas tocadas (13)** — ver detalle completo en `## Tablas`:
`DQS_REC_USUARIOS`, `DQS_OPT_LOGUEO`, `dbo.ImperiumSesionPuente` (compartida
con Recobros, columnas `IdRol` y `TokenMipres` propias de Contingencia),
`DQS_MIP_FORMATOS`, `DQS_MIP_COD_HAB`, `DQS_MIP_TOKEN`, `DQS_REC_ROLMODULO`,
`DQS_REC_ROLES`, `DQS_REC_MODULOS`, `DQS_MIP_CRUZAR`,
`DQS_MIP_PROFESIONALESACTA`, `DQS_MIP_PROFESIONALES`, `DQS_MIP_JUNTASC`.

**Procedimientos almacenados usados (22 invocados directo desde C#, +2 helpers internos
documentados aparte — `ResolverUsuario`/`SincronizarUsuario`, ver "Página 10")** — ver detalle completo en
`## Procedimientos almacenados`: `sp_DQS_ConsultaUsuario`,
`sp_DQS_ConsultaModxRol`, `sp_DQS_ActualizaMarcaUsuario`,
`sp_opt_ingresalogueo`, `sp_DQS_ActualizaPwd`, `sp_MIP_InsertaFormatoCnt`,
`sp_MIP_ConsultaFormatosCNT`, `sp_MIP_ActualizaFormatoCnt`,
`sp_DQS_ConsultaFiltros` (usado con `@INTVALIDA=136` y `@INTVALIDA=138`),
`sp_DQS_ConsultaProfesionalAñadido`, `sp_DQS_ConsultaProfesional`,
`sp_DQS_AsignaProfesional`, `sp_DQS_EliminaProfesional`,
`sp_DQS_ActualizaProfesional`, `sp_DQS_ActualizaJunta`,
`sp_DQS_MIP_ProfesionalesConsultar/Obtener/Insertar/Actualizar/
CambiarEstado/Resumen/Listas` (7, página Profesionales — `ResolverUsuario`/
`SincronizarUsuario` son helpers internos, invocados solo desde dentro de
`Insertar`/`Actualizar`, no desde código C#). Más 2 consultas SQL
directas parametrizadas (`BuscarPrestadorAsync` contra `DQS_MIP_COD_HAB`,
`ConsultarEmailAsync` contra `DQS_MIP_FORMATOS`).

**Dependencias externas reales (no son de DQS)**: la API del gobierno
`wsmipres.sispro.gov.co` (SISPRO/MIPRES), en dos puntos independientes —
(1) validación de prescripciones al marcar "Gestionado" en el modal de
Gestionar formato, y (2) el módulo Junta Profesional completo (consulta y
generación del acta en PDF), ver "Página 8". El envío de correo del modal
"Firma" (ver "Página 9") usa el mismo `IEmailSender`/SMTP interno de DQS,
no una dependencia externa nueva. Desde 2026-09-29, el acta también puede
pedir por HTTP una imagen de firma al sitio legado (`Legacy:BaseUrl` +
ruta) cuando esa firma no vive en el disco local de esta app — ver
`IFirmaImagenService` en "Página 10".

**Arquitectura**: `Contingencia.Core.slnx` con 4 proyectos (`Domain` →
`Infrastructure` → `Application` → `Web`), estructura y paquetes NuGet
calcados de `Imperium.Core.slnx` (Recobros) pero en **solución separada**
(versionamiento independiente, master/layout propio `SiteMipresN` vs.
`Site.Master`). Comparte con Recobros: la tabla de usuarios
(`DQS_REC_USUARIOS`), el flujo de autenticación (mismo
`sp_DQS_ConsultaUsuario`), y la tabla puente de sesión
(`dbo.ImperiumSesionPuente`, mismo objeto físico). Cookie de autenticación
propia (`ContingenciaWebAuth`), captcha propio (Cloudflare Turnstile en vez
de reCAPTCHA v2).

**Brechas/exclusiones deliberadas** (no pendientes reales, decisiones
tomadas y documentadas en su sección): `JuntasFormatoCntCO2.aspx` como
página/ventana aparte no se migró — su funcionalidad sí se migró, como
modal dentro de "Consultar formato"; el sidebar de "Formato contingencia"
sigue gateado por el módulo "Contingencia" que los roles 27/30 no tienen
asignado para esta CIA (no les hace falta, el login los manda directo a
Consultar formato); las guardas de sesión "IP registrada"/"sesión en otro
equipo" del master legado (gateadas a `Session("TOKEN")="IPS"`) no aplican
a los usuarios de esta app y no se portaron; el bloque de firmas fijas
hardcodeadas del acta para la CIA `900971006` (una EPS ajena a esta app) no
se portó por no aplicar nunca a ningún usuario real de Contingencia — ver
"Página 8"; el lienzo de captura de firma a mano alzada de
`JuntasFirma.aspx` (canvas + `WebMethod UploadImage`) está dentro de un
bloque comentado en el propio `.aspx` legado — código muerto, inalcanzable
desde la UI real — y no se migró; el campo "Sede" del buscador de
profesionales nunca tuvo efecto real en el legado (un bug confirmado del
propio SP) y no se expone en la UI migrada; el destinatario fijo de la
notificación de acta firmada (una dirección @gmail personal del legado) se
replicó tal cual, sin cambiarlo — ver "Página 9".

---

## Relación con Recobros (Imperium 2026 core)

Confirmado con el usuario: Formato de Contingencia **usa la misma tabla y el
mismo método de autenticación** que Recobros (`DQS_REC_USUARIOS`, mismo flujo
de login). Lo único que cambia después de loguearse es el destino: Recobros
redirige a `/inicio`, Contingencia (usuario de prueba `cwformatos`, rol
`601`) redirige a **`JuntasFormatoCnt.aspx`** (master `SiteMipresN`, distinto
del `Site.Master` de Recobros — confirmado con el usuario 2026-09-02),
todavía sin migrar (ver "Página 2" abajo).

Esto significa que las siguientes tablas/procedimientos, ya documentados a
fondo en `Imperium 2026 core/Imperium.Infrastructure/BASE_DE_DATOS.md`, **son
los mismos objetos de base de datos** (no una copia) y no hace falta
re-investigar su firma real, solo re-implementar el código C# que los llama
(por decisión de arquitectura, duplicado en vez de compartido vía librería):

| Objeto | Rol |
|---|---|
| `DQS_REC_USUARIOS` | Tabla de usuarios/login |
| `DQS_REC_ROLES`, `DQS_REC_MODULOS`, `DQS_REC_ROLMODULO` | Roles y módulos por rol |
| `DQS_REC_CLIENTES` | Datos de compañía/branding |
| `DQS_OPT_LOGUEO` | Control de "sesión activa en otro equipo" |
| `DQS_REC_INGRESOS` | Auditoría de ingreso a módulo |
| `sp_DQS_ConsultaUsuario` | Autentica usuario (**ojo con sus efectos secundarios** — ver nota en el documento de Recobros) |
| `sp_DQS_ConsultaModxRol` | Módulos visibles por rol |
| `sp_DQS_ActualizaMarcaUsuario` | Marca de sesión (login/logout/bloqueo) |
| `sp_DQS_ActualizaPwd` | Reseteo de clave |
| `sp_opt_ingresalogueo` / `sp_opt_consultalogueo` | Registro/consulta de sesión activa |
| `sp_DQS_InsertarIngresoMod` | Auditoría de ingreso a módulo |
| `dbo.ImperiumSesionPuente` | Tabla puente de sesión con el legado — **ojo**: si Contingencia y Recobros conviven en el mismo servidor/IIS apuntando a la misma base, hay que confirmar si esta tabla puente se puede compartir tal cual o si Contingencia necesita su propia tabla/discriminador (no investigado aún). |

**Antes de escribir el login de Contingencia**: releer la sección de Login y
la nota de `sp_DQS_ConsultaUsuario` en el documento de Recobros — el
comportamiento (desbloqueo automático, reseteo de clave hardcodeado,
limpieza de `MARCA` para rol 35) aplica igual acá porque es el mismo SP.

---

## Estado actual

**Las 7 páginas de los 2 flujos que sirve esta app están migradas**: las 6
del flujo Mipres/Formato de Contingencia (Opciones, Login, Inicio, Cargar
formato, Consultar formato con su modal de Gestionar, Buzón PQR) más Junta
Profesional (consulta + acta en PDF + los modales de Cargas y Firma — ver
"## Resumen ejecutivo" arriba para la lista con rutas). Este documento se
fue completando página por página, igual que se hizo con Recobros — cada
sección se verificó contra la base de pruebas antes de darla por buena, no
se documentó "de memoria" ni copiando el código VB sin confirmar.

### Página 1 (✅ migrada): Opciones (`Opciones.aspx` → `/`)

Es el **mismo archivo físico** `Imperium/Opciones.aspx` /
`Opciones.aspx.vb` que ya migramos para Recobros (`Imperium.Web/Components/
Pages/Opciones.razor`) — **no hay tabla ni SP involucrado**, es una lista de
tarjetas (`AppCard`: Nombre/Descripción/Href/Icono) armada en memoria en
`Page_Load`, sin `DataSource` de base de datos.

Confirmado con el usuario (2026-09-02): la variante correcta es el bloque
comentado **"Opciones SANITASMIPRES"** — el mismo que ya usa
`Imperium.Application.Menu.MenuOpcionesService` para Recobros (Recobros y
Contingencia comparten esta pantalla de entrada, mismo cliente EPS Sanitas).
Migrado tal cual a `Contingencia.Domain/Menu/AppCard.cs`,
`Contingencia.Application/Menu/IMenuOpcionesService.cs`/
`MenuOpcionesService.cs`, y `Contingencia.Web/Components/Pages/
Opciones.razor` (ruta `/`) — verificado visualmente en el navegador, igual
render que la copia de Recobros, sin errores de consola.

**Nombre confirmado** (2026-09-02): la tarjeta de esta app se llama
**"Formato Integralidad"** (`Descripción`: "Registro del cumplimiento de
acciones integrales en salud según RIAS", ícono `documento.png`) — el
usuario lo confirmó tal cual, sin cambios de texto. Se dejó `Href="/login"`
(pendiente de crear en ese momento) para esta tarjeta.

**Actualizado (2026-09-02, al iniciar la integración del módulo Juntas)**:
la tarjeta **"Gestor Juntas"** también apunta ahora a `/login` — antes
apuntaba a `Login3.aspx` (legado), el mismo archivo que ya migramos como
`/login` para "Formato Integralidad". Confirmado que ambas tarjetas
comparten literalmente el mismo login legado (`Login3.aspx`), y que lo
único que cambia entre ellas es el rol del usuario que inicia sesión: rol
`601` → Formato de Contingencia (`/inicio`), rol `60` → Junta Profesional
(`/juntas`, migrado el mismo día — ver "Página 8" y
`LoginService.DeterminarDestino`). El resto de tarjetas quedan con sus
valores originales del legado (`Login.aspx`, `Login2.aspx`, `Login7.aspx`,
`https://help.dqualitysolutions.com.co/Login.aspx`,
`https://imperiumgitsanitas.net/LoginTUT.aspx`), tal como en Recobros.

Assets estáticos copiados tal cual desde `Imperium.Web/wwwroot` (mismo
contenido, mismo look & feel): `OpcionesN/` completo, `assets/css/
sweetalert.css`, `assets/images/logo.jpg`, `assets/js/sweetalert.min.js`,
`assets/js/sweetalert2.min.js`, `js/opciones.js`, `favicon.ico`.

#### Ampliación (✅ 2026-10-13): rediseño de `Opciones.aspx` y perfil IPS

El usuario rediseñó `Opciones.aspx` en el legado y pidió pasar todos los cambios a Recobros y a Contingencia.

**Qué cambió en el legado** (`Imperium/Opciones.aspx` + `Opciones.aspx.vb`, 2026-10-13; el `.aspx` pasó de ~7 KB a
~40 KB, el `.vb` es el mismo de 2026-09-24):

1. **Diseño nuevo de la pantalla**: fondo claro con animaciones azules (blobs, red de nodos en `<canvas>` con pulsos,
   rayos de datos, piso en perspectiva, barrido), pantalla de arranque con logo, cabecera con mensaje que se escribe
   solo + reloj en vivo, tarjetas con panel visual + cuerpo (número tipo tecla, botón circular de acceso, anillo neón
   al pasar el cursor, onda al hacer clic, inclinación 3D), transición de pantalla al navegar y atajos de teclado
   (teclas `1`…`9` abren la tarjeta correspondiente). Reemplaza al diseño anterior basado en Bootstrap/WOW/jQuery.
2. **Rejilla dinámica**: ya no es 4×2 fijo; columnas = `n<=4 ? n : n<=6 ? 3 : n<=8 ? 4 : n<=15 ? 5 : 6`, filas =
   `ceil(n/columnas)`; con más de 2 filas se oculta la descripción (`dense`). La última fila queda centrada.
3. **Bloque activo del code-behind cambió** de "Opciones SANITASMIPRES" a **"IPS"**: Mipres, Help Soporte,
   Laboratorios, RIPS, Facturador, Historia Clinica, Inventarios (7 tarjetas, íconos nuevos `microscopio.png`,
   `Rips.png`, `factura.png`, `inventario.png`, ya existentes en `OpcionesN/assetsN/images`). Los demás bloques
   siguen comentados en el `.vb`: INCAPACIDADES, PLUS, SANITASMIPRES, Tutelas, Informes GF.
4. Sin tablas ni SP: igual que antes, las tarjetas se arman en memoria (`AppCard`: Nombre/Descripción/Href/Icono).
   Sin cambios de base de datos en esta ampliación.

**Cómo quedó en Blazor** (mismo código en `Imperium.Web` y `Contingencia.Web`):

- `Components/Pages/Opciones.razor` (ruta `/`): marcado nuevo (`<main id="grid">` con una `<a class="card">` por
  tarjeta). `--cols`/`--rows`/`dense` se calculan en C# (`OnInitialized`) con la misma regla del legado, para que Blazor
  controle esos atributos y un re-render no los pise. Se conservan `?Valida=1` (alerta "Usuario no corresponde al
  aplicativo escogido"), SweetAlert y `js/opciones.js` (`Tran`/`Alerta`/`Alerta2`). Se **quitaron** de esta página
  jQuery, Bootstrap, WOW y los CSS `animate/bootstrapN/superclasses/custom/responsive` (el diseño nuevo es
  autocontenido); los archivos siguen en `wwwroot/OpcionesN` por si otra pantalla los usa.
- `wwwroot/css/opciones.css` (nuevo): el `<style>` completo del `.aspx`, sin cambios salvo `#form1{display:contents}` →
  `.opc-page{display:contents}`. Se enlaza solo desde `<HeadContent>` de la página, así que sus reglas globales
  (`body`, `header`, `main`, `footer`) dejan de aplicar al navegar a otra página.
- `wwwroot/js/opciones-menu.js` (nuevo, módulo ES): la animación y navegación del `<script>` del `.aspx`
  (tilt 3D, rayos, red de nodos, escritura + reloj, onda + transición, teclas). Se carga con `import()` desde
  `OnAfterRenderAsync` y se libera en `DisposeAsync` (cancela listeners, temporizadores y el `requestAnimationFrame`),
  porque Blazor no ejecuta `<script>` embebidos en componentes.
- `Application/Menu/MenuOpcionesService.cs`: **cada bloque del `.vb` es ahora un perfil** y el activo se elige con la clave
  `Menu:Perfil` (`appsettings.json`, **por defecto `"SANITASMIPRES"`**; vacío o desconocido = SANITASMIPRES). Perfiles: `IPS`, `INCAPACIDADES`,
  `PLUS`, `SANITASMIPRES`, `TUTELAS`, `INFORMES_GF`. Equivale a "descomentar otro bloque" en el legado, sin recompilar.
  `Program.cs` registra el servicio con `new MenuOpcionesService(builder.Configuration["Menu:Perfil"])`.

**Verificación (2026-10-13)**: `dotnet build` de ambos proyectos sin errores; la app de Contingencia se levantó y se
revisó en Chrome (7 tarjetas IPS, 4 columnas, escritura y reloj funcionando, las 7 con `ready`, sin errores de consola,
clic en "Mipres" → `/login`, `?Valida=1` muestra la alerta). Compatibilidad Linux: solo HTML/CSS/JS estático y C# puro;
rutas con mayúsculas/minúsculas idénticas al archivo (`css/opciones.css`, `js/opciones-menu.js`,
`OpcionesN/assetsN/images/*.png`).

**Decisión (2026-10-13, usuario)**: aunque el legado dejó activo el bloque IPS, estas apps (Recobros, Juntas y Formatos de
Contingencia) se publican con la sección **"Opciones SANITASMIPRES"** habilitada, así que `Menu:Perfil` quedó en
`"SANITASMIPRES"` en `appsettings.json` y como valor por defecto del código. IPS sigue disponible como perfil
(`"Menu": { "Perfil": "IPS" }`).

**Particularidad de Contingencia**: en el perfil **IPS** (no es el predeterminado) la tarjeta "Mipres" apunta a **`/login`** (en el legado va a
`Login3.aspx`, que aquí ya está migrado como `/login`: rol `601` → Formato de Contingencia, rol `60` → Junta Profesional).
Es la única tarjeta del perfil IPS con ese login. En `SANITASMIPRES` y `PLUS` se conserva lo ya acordado
(2026-09-02): "Formato Integralidad" y "Gestor Juntas" → `/login`, y "Mipres" sigue en `Login3.aspx` como en el
legado. Con el perfil predeterminado `SANITASMIPRES` el menú queda como antes de este rediseño (mismas tarjetas y enlaces). Laboratorios,
RIPS, Facturador, Historia Clinica e Inventarios apuntan a sus `.aspx` del legado, aún sin migrar.

### Página 2 (✅ migrada): Login (`Login3.aspx` → `/login`)

**No es el mismo archivo que el login de Recobros** (`Login.aspx`) — son dos
páginas legadas distintas que comparten el mismo SP de autenticación
(`sp_DQS_ConsultaUsuario`, mismo objeto de base de datos). Diferencias reales
encontradas al comparar `Login3.aspx.vb` contra
`Imperium.Application.Login.LoginService` (Recobros), y decisiones tomadas:

- **Captcha modernizado, no solo migrado**: el control `GoogleReCaptcha` del
  `.aspx` original está comentado (deshabilitado) — no había nada que migrar
  ahí. Por decisión explícita del usuario (2026-09-02) se agregó **Cloudflare
  Turnstile** en su lugar (no el mismo reCAPTCHA v2 que usa Recobros) — ver
  sub-sección "Captcha (Cloudflare Turnstile)" abajo.
- **Sin 2FA**: a diferencia de Recobros (`TwoFactorAuthentication.aspx`),
  Login3 no tiene ningún flujo de doble factor.
- **"Olvidé mi clave" implementado, no solo migrado**: el enlace visible en
  `Login3.aspx` apunta a WhatsApp de soporte — el código viejo
  (`crearcorreo()`, disparado por un query string `?txtemail=...`) no era
  alcanzable desde el formulario. Por decisión explícita del usuario
  (2026-09-02) se implementó como un formulario real y accesible (mismo
  patrón que ya usa Recobros) — ver sub-sección "Recuperar clave" abajo.
- **Botón SSO/SAML oculto** (`Visible="false"` en el `.aspx`) — existe en el
  código (`btnSSOLogin_Click` → `~/Saml2/SignIn`) pero no está en uso hoy. No
  se migró.
- **Redirección post-login por rol, no por 2FA** — réplica fiel de la
  secuencia de `If` (no `ElseIf`) de `Login3.aspx.vb`, donde cada condición
  que aplica sobreescribe la anterior y por lo tanto el **orden importa**:
  1. Por defecto: `InicioMIPRES.aspx` (otra app, no migrada).
  2. Si la clave ingresada coincide con la clave temporal de referencia
     (columna 23 del SP) → `ActContraseña.aspx?msj=Debe actualizar la
     contraseña temporal`.
  3. Si el rol (columna 4 del SP, **no** la columna 1 — ver abajo) es `35`
     → `ConsultaMipresWEB.aspx`.
  4. Si el rol es `60` → `JuntaProfesional.aspx`.
  5. Si el rol es `601` → **`/inicio`** (ruta propia de esta app, aún sin
     construir — apunta ya al destino final `JuntasFormatoCnt.aspx` una vez
     se migre, misma convención que usó Recobros con `/inicio` antes de que
     existiera `Inicio.razor`).
  6. **Por último**, en un bloque `Try` separado que corre después y por eso
     tiene la última palabra incluso sobre el rol 601: si la fecha de hoy
     coincide con la fecha de vencimiento de clave (columna 24) →
     `ActContraseña.aspx?msj=Debe actualizar su contraseña por motivos de
     seguridad`.
- **`sp_opt_ingresalogueo` envuelto en `Try/Catch`** que traga cualquier
  error — a diferencia de Recobros, que no protege esa llamada. Réplica
  fiel del legado.
- **No llama** `sp_opt_consultalogueo` (no hay chequeo de "sesión activa en
  otro equipo" dentro del propio login). Sí llama `sp_DQS_ActualizaPwd` desde
  el flujo de recuperar clave agregado (ver abajo) — ese SP no lo usaba
  Login3.aspx.vb originalmente porque su propio flujo de recuperación nunca
  era alcanzable.
- **Columnas del SP usadas más allá de las que ya usaba Recobros**: Login3
  lee hasta la columna 26 del resultado de `sp_DQS_ConsultaUsuario` (Recobros
  solo llegaba hasta la 24, más la 27 para 2FA que Login3 no usa). Confirmado
  cruzando índice por índice contra `Imperium.Domain.Login.ResultadoUsuario`
  — las columnas 0-17, 23 y 24 coinciden exactamente entre ambas apps (mismo
  SP). Las columnas adicionales (18=Token, 19=EmailCliente, 20=ProCli,
  21=Cedula, 22=MarcaP, 25=MarcaP2/Regional, 26=EmailUsuario) se capturan en
  `Contingencia.Domain.Login.ResultadoUsuario` pero **todavía no se
  persisten** en la tabla puente (`dbo.ImperiumSesionPuente` no tiene esas
  columnas) — se agregarán con un `ALTER TABLE` aditivo si la página destino
  (`/inicio` → `JuntasFormatoCnt.aspx`) termina necesitándolas.
- **Bug del legado identificado, no replicado**: el índice 25 del SP se
  asigna en el `.vb` original a la vez a `Session("marcap2")` y a
  `Session("regional")` (dos variables leyendo la misma columna) — casi con
  certeza un error de copiar/pegar. Se documenta como un solo campo
  (`MarcaP2Regional`) en vez de duplicar el bug con dos propiedades
  idénticas; no cambia ningún comportamiento observable porque ninguna de
  las dos variables se usa todavía en ningún destino migrado.

**Verificado en vivo contra DQUALITYREC (2026-09-02)** con el usuario real de
prueba `cwformatos` (Cía `890330031`, rol `601`, contraseña en texto plano en
la base — `cwformatos2024**`):

- Login correcto → redirigió a `/inicio` (confirma que el rol 601 se
  resuelve bien) → `404 Not Found` esperado (la página aún no existe).
- `DQS_REC_USUARIOS.MARCA` pasó de `0` a `1` y `FECHA_SESION` se actualizó —
  restaurado a `0` después de la prueba.
- Se insertó una fila nueva en `DQS_OPT_LOGUEO` (`IP='::1_99293'` — `::1` es
  localhost, coherente con la prueba desde el navegador local).
- El token de la tabla puente se creó y se consumió/borró correctamente (se
  confirmó que las 2 filas que quedaban en `ImperiumSesionPuente` son de
  pruebas viejas de Recobros de agosto 26, ya expiradas, usuario distinto —
  no relacionadas).
- Clave incorrecta → mensaje de error idéntico al legado ("Señor usuario
  contacte a Soporte Imperium...").

Archivos: `Contingencia.Domain/Login/ResultadoUsuario.cs`,
`Contingencia.Domain/SesionPuente/DatosSesionPuente.cs`,
`Contingencia.Infrastructure/Login/{ILoginRepository,LoginRepository}.cs`,
`Contingencia.Infrastructure/SesionPuente/{ISesionPuenteRepository,
SesionPuenteRepository}.cs`, `Contingencia.Infrastructure/Data/
{IStoredProcedureExecutor,StoredProcedureExecutor}.cs` (port exacto, sin
cambios, de la plomería genérica de Recobros),
`Contingencia.Application/Login/{ILoginService,LoginService,
ResultadoIntentoLogin}.cs`, `Contingencia.Application/Auth/
{ImperiumClaimTypes,ClaimsPrincipalExtensions,
SesionClaimsPrincipalFactory}.cs`, `Contingencia.Web/Auth/
PuenteSesionMiddleware.cs`, `Contingencia.Web/Services/
{ILegacyUrlBuilder,LegacyUrlBuilder}.cs`, `Contingencia.Web/Components/
Pages/Login.razor`. Cookie de autenticación propia: `ContingenciaWebAuth`
(esquema `ContingenciaCookie`) — no comparte cookie con Recobros
(`ImperiumWebAuth`), aunque ambas apps sí comparten la cookie puente
`ImperiumBridge` de un solo uso (sin conflicto real: cada app la borra al
consumirla).

Assets estáticos copiados desde `Imperium.Web/wwwroot`: `New_Login/
{Fondo3.jpg,logo.jpg}`. A diferencia del `.aspx` legado (que carga un bundle
local `LoginN/assets/...`), se usa Bootstrap 5 + Font Awesome por CDN, misma
simplificación que ya hizo Recobros en su propio `Login.razor`.

#### Captcha (Cloudflare Turnstile)

Elegido en vez de reCAPTCHA v2 (el que usa Recobros) por ser más moderno: no
depende de una cuenta de Google, es gratuito sin límite de uso, y su
challenge "administrado" deja pasar a la mayoría de visitantes sin que
resuelvan nada (solo un ícono de carga). Implementación:

- `Contingencia.Infrastructure/Captcha/{ITurnstileValidator,
  TurnstileValidator}.cs` — valida el token contra `https://challenges.
  cloudflare.com/turnstile/v0/siteverify`, mismo patrón que
  `RecaptchaValidator` de Recobros.
- Viven las claves en `Turnstile:SiteKey`/`Turnstile:SecretKey`
  (`appsettings.json`). **Por defecto trae las claves de PRUEBA públicas y
  documentadas por Cloudflare** (`1x00000000000000000000AA` /
  `1x0000000000000000000000000000000AA`) — siempre muestran el widget y
  siempre validan con éxito, sirven para desarrollar sin credenciales reales.
  **Antes de producción hay que reemplazarlas** por un site key/secret real
  (gratis, se crean en `https://dash.cloudflare.com/?to=/:account/turnstile`).
- El widget se renderiza de forma explícita vía JS
  (`Contingencia.Web/wwwroot/js/login.js`, cargado globalmente desde
  `App.razor`, igual que el `login.js` de Recobros) por el mismo motivo que
  reCAPTCHA: una enhanced navigation de Blazor reutiliza el nodo del DOM en
  vez de reemplazarlo, y volver a renderizar sobre el mismo nodo falla — el
  contenedor lleva `data-permanent` para que Blazor no lo toque entre
  navegaciones.
- El token viaja en el campo oculto que Turnstile inyecta solo,
  `cf-turnstile-response`, capturado en `Login.razor` vía
  `[SupplyParameterFromForm(Name = "cf-turnstile-response", ...)]`.

**Verificado en vivo (2026-09-02)**: el widget renderiza correctamente (se
confirmó visualmente el aviso amarillo estándar de Cloudflare para claves de
prueba), el campo oculto recibe un token, y el login con `cwformatos`
completa exitosamente con el captcha ya activo (redirigió a `/inicio` igual
que sin captcha).

#### Recuperar clave

Implementado igual que Recobros (`Imperium.Application.Login.LoginService.
EnviarClaveTemporalAsync`), réplica fiel de `GenerarCadena(8)` en
`Login3.aspx.vb` (incluyendo sus dos rarezas: el bucle genera 9 caracteres,
no 8, y el último índice del arreglo de letras nunca se usa — mismo código
copiado/pegado que ya tiene el `GenerarClaveAleatoria` de Recobros).

- `Contingencia.Infrastructure/Correo/{IEmailSender,EmailSender,
  PlantillaRecuperacionClave}.cs` — port exacto de los de Recobros; la
  plantilla HTML es literalmente la misma que trae `crearcorreo()` en
  `Login3.aspx.vb` (mismo pie "© DQS RECOBROS", boilerplate copiado/pegado
  entre las páginas legadas de este cliente).
- `sp_DQS_ActualizaPwd` (`@STREMAIL, @STRPWD`) — mismo SP que Recobros, se
  llama aunque falle sin frenar el envío del correo (réplica fiel: el legado
  tampoco valida el resultado del `UPDATE` antes de enviar).
- `Smtp:Host/User/Password` en `appsettings.json` quedan vacíos a propósito
  (mismo criterio que Recobros) — sin configurarlos, el envío falla y se
  muestra "El mensaje no fue posible enviarlo" en vez de silenciarlo.

**Verificado en vivo (2026-09-02)**: el toggle "¿Olvidó su clave?" muestra el
formulario correcto; al enviar sin SMTP configurado, se mostró el mensaje de
error esperado.

### Página 3 (✅ migrada): Inicio (`JuntasFormatoCnt.aspx` → `/inicio`)

El `.aspx` original no tiene tabla ni SP propio — son 2 tarjetas de
navegación estáticas (`Cargar formato` → `JuntasFormatoCntC.aspx`,
`Consultar formato` → `JuntasFormatoCntCO.aspx`, ninguna migrada, quedan como
enlaces externos al legado vía `LegacyUrlBuilder`, misma convención que
Opciones/Login). El código-behind (`JuntasFormatoCnt.aspx.vb`) está
completamente vacío (`Page_Load` sin cuerpo).

Lo real de esta migración fue el **master compartido `SiteMipresN.Master`**
(3402 líneas + 808 de code-behind) — analizado a fondo antes de escribir
nada. Es el master de TODA la familia de páginas "Mipres" del legado, no
solo de esta; casi todo su contenido (chat en tiempo real, paneles de
manuales, ~50 variantes de menú por compañía/rol, un combo de super-admin)
es irrelevante para el rol 601 o directamente código muerto. **No se portó
el theme de administración completo (~37MB: owl-carousel, apexcharts,
datatables, MetisMenu)** — se reconstruyó un layout propio
(`Contingencia.Web/Components/Layout/AppLayout.razor` +
`AppLayout.razor.css` + `wwwroot/js/app-layout.js`), copiando tal cual el
CSS/JS genérico (no específico de ningún módulo) que Recobros ya construyó
para su propio `AppLayout.razor` — mismo patrón de sidebar colapsable,
mismas variables de color, mismo toggle responsive.

Lo que sí se investigó y confirmó línea por línea contra
`SiteMipresN.Master.vb` antes de reconstruir:

- **Sidebar por módulo real, no adivinado**: el menú "Mipres" con el enlace
  "Formato contingencia" se muestra cuando `Session("cia")` es `890330031`
  u `ESSC18` **y** `Session("idrol") <> "277"` **y** el usuario tiene el
  módulo `"Contingencia"` exacto en `Session("Modulos")` (código: `If
  dtline("MODULO").ToString = "Contingencia" Then liformatocotingencia.
  Visible = True`). Confirmado en vivo contra la base de pruebas: `EXEC
  sp_DQS_ConsultaModxRol @STRCIA='890330031', @STRROL='601'` devuelve
  exactamente una fila, `Contingencia` — coincide. Implementado en Blazor
  como `context.User.TieneModuloExacto("Contingencia")` (ya existía este
  helper, ver `ClaimsPrincipalExtensions`).
- **Saludo de usuario**: `lblnombre.Text = Session("nombre")` → `Nombre()`
  (mismo claim que ya existía).
- **Título del header** ("EPS SANITAS"): `lblcia.Text = Session("nomcli")`
  — es un valor literal ya almacenado así en la base para esta Cía, no hay
  concatenación que reproducir. Se usa el claim `NombreCliente()` (ya
  existía, mapea a la columna 12 del mismo SP de login).
- **Logout**: `lnkcerrar_ServerClick` llama `sp_DQS_ActualizaMarcaUsuario`
  con `intvalida=0`, limpia la sesión, y redirige a `Opciones.aspx` —
  idéntico patrón al de Recobros (`Site.Master.vb`). Implementado como
  endpoint `POST /cerrar-sesion` (ver tabla de procedimientos).
- **Guardas de sesión "sesión activa en otro equipo" e "IP registrada"**:
  existen en el legado pero están **gateadas a `Session("TOKEN") =
  "IPS"`** — no aplican a los usuarios Sanitas/Formatos (`token="EPS"` o
  vacío) que nos ocupan. No se portaron; documentado como brecha conocida,
  no como omisión accidental.
- **Campana de notificaciones y chat**: la campana sí es una consulta real
  en el legado (`consultafiltros` valida=163) pero para el módulo
  "Ambulatorio Priorizado" de Mipres, no para Contingencia — y el chat en
  tiempo real está casi todo en código muerto (bloque `<%-- --%>`) o fuera
  de alcance (abre un widget de mensajería completo). Ambos íconos quedan
  **decorativos** en el Blazor (sin badge dinámico, sin funcionalidad al
  hacer clic) — documentado como simplificación deliberada, no como
  hallazgo pendiente de implementar.
- **Selector de régimen / combo de super-admin**: no existen en este
  master (a diferencia de `Site.Master.vb` de Recobros) — nada que portar.
- **Pie de página**: literal `"Imperium-DQS © 2025 All Rights Reserved |
  by Data Quality Solutions"` — hardcodeado en el legado (no calcula el año
  dinámicamente), reproducido tal cual, sin "corregir" el año fijo.

**Desviación deliberada, no un bug**: la ruta `/inicio` con `@attribute
[Authorize]` redirige a `/login` (no a `/`, que sería la réplica literal de
`Opciones.aspx`) cuando no hay sesión — porque `options.LoginPath =
"/login"` en `Program.cs` intercepta antes de que el `<NotAuthorized>` del
propio `AppLayout` llegue a ejecutarse. Se decidió mantenerlo así (mandar a
alguien sin sesión directo al login de esta app, no al menú general de
apps) en vez de forzar la réplica exacta del legado.

**Verificado en vivo (2026-09-02)** con `cwformatos`: login → `/inicio`
renderiza el layout completo (logo, "EPS SANITAS", campana/chat/avatar,
"Hola, FORMATOS UNIVERSAL", sidebar "Mipres > Formato contingencia" visible,
"PQR", pie de página, las 2 tarjetas con sus íconos) — visualmente
equivalente a una captura real de la app legada corriendo con el mismo
usuario. Logout: redirige a `/` (Opciones), `MARCA` vuelve a `0` en BD sin
intervención manual. Acceso a `/inicio` sin sesión: redirige a `/login`
correctamente. Único hueco encontrado (dato, no código): la columna
`Session("logoempresa")` viene vacía para este usuario de prueba, así que
el logo de cliente (EPS Sanitas) no aparece en el sidebar — el código que
lo mostraría (`!string.IsNullOrWhiteSpace(context.User.LogoEmpresa())`) ya
está listo, solo falta el dato en la base para verse.

### Página 4 (✅ migrada): Cargar formato (`JuntasFormatoCntC.aspx` → `/formatos/cargar`)

Primera página con lógica de negocio real de todo este módulo (las
anteriores eran navegación/autenticación). Formulario: tipo de documento
(dropdown fijo de 14 valores), número de documento, fecha del formato,
correo IPS, "Código de Habilitación - Prestador" (con autocompletado),
observaciones, y un archivo adjunto.

- **`sp_MIP_InsertaFormatoCnt`** (`@CIA, @TIPOID, @NUMID, @NIT, @OBJ, @ext,
  @email, @fechaformato, @usuario` — 9 parámetros, confirmados) — mismo SP
  que ya usaba `JuntasFormatoCntC.aspx.vb` vía `objbll.
  insertafotmatocontigencia` (nombre con error de tipeo en el legado; el
  procedimiento real se llama distinto). Genera el **radicado** como
  `AAAAMMDD` + `COUNT(*)` de `DQS_MIP_FORMATOS` rellenado con ceros a 14
  dígitos — **no es una secuencia atómica** (dos cargas simultáneas
  podrían, en teoría, calzar en el mismo radicado); es una limitación real
  del SP compartido, no algo que se corrige aquí porque no se reescribe el
  SP, solo se llama igual que el legado. Inserta en `DQS_MIP_FORMATOS`
  (ver tabla) y devuelve el radicado en una sola fila/columna.
- **Extensiones realmente aceptadas ≠ las anunciadas en el UI**: el
  `FileUpload` legado anuncia `accept=".pdf,.tiff,.png"`, pero el
  code-behind en realidad permite `.png,.jpg,.jpeg,.tiff,.docx,.doc,.xlsx,
  .pdf` — se replicó la lista **real** (la del code-behind), no la
  anunciada, en `FormatosService.ExtensionesPermitidas`.
- **Autocompletado de prestador — inyección SQL corregida, no replicada**:
  el `WebMethod Ciudad` (nombre engañoso — no tiene nada que ver con
  ciudades, es el autocompletado del campo Prestador) concatenaba
  `prefixText` directo en el SQL contra `DQS_MIP_COD_HAB`
  (`... LIKE '" & prefixText & "%'`), con un comentario en el propio código
  que dice literalmente "Try to use parameterized inline query/sp to
  protect sql injection" y **no lo hace**. Se corrige en
  `FormatosRepository.BuscarPrestadorAsync` con parámetros reales — mismo
  criterio ya aplicado en `LiquidacionAutocompleteRepository` de Recobros.
  Verificado en vivo contra la base de pruebas que la consulta parametrizada
  devuelve los mismos resultados.
- **Orden de validaciones replicado exactamente** (`btngenerar_ServerClick`,
  mensajes idénticos, vía `Tran`/`Tran2` — agregados en
  `wwwroot/js/dialogos.js`, cargado globalmente desde `App.razor` porque
  `SiteMipresN.Master` ya no se porta completo): sin archivo → "Debe
  ingresar un documento"; extensión no permitida → "Archivo no permitido";
  sin tipo de documento → "Debe ingresar el tipo de documento"; sin número
  de documento → "Debe ingresar no documento"; sin prestador → "Debe
  ingresar el prestador"; sin email → "Debe ingresar el email"; sin fecha →
  "Debe ingresar la fecha".
- **Archivo guardado bajo `App_Data`, no bajo `wwwroot`**: el legado guarda
  en `~/MediaUploader/Formatos/{cia}/{radicado}{ext}` (carpeta pública). Se
  replica la misma estructura de carpetas pero bajo `App_Data/mediauploader/
  formatos/{cia}/{radicado}{ext}` — mismo criterio ya usado por Soportes/
  Indexación/Imágenes en Recobros (no servir archivos de usuarios sin
  autenticación). **Pendiente de una futura iteración**: no hay endpoint
  para volver a descargar el archivo ya cargado — el legado tampoco lo
  ofrece desde esta página (solo desde "Consultar formato", sin migrar
  todavía).
- **Dead code confirmado, no replicado**: el legado calcula `fileCount =
  Directory.GetFiles(path).Length + 1` antes de guardar el archivo, pero
  esa variable **nunca se usa** — el nombre real del archivo es el radicado,
  no `fileCount`. No se portó (no tiene ningún efecto observable).
- **`AllowMultiple="false"` en el legado**: aunque el code-behind itera
  `Request.Files` con un `For`, la UI solo permite seleccionar un archivo a
  la vez. Se replica como una sola selección (`InputFile` sin `multiple`).

**Verificado contra la base de pruebas (2026-09-02)**, sin usar el
navegador (ver nota de estabilidad de sesión): se ejecutó `sp_MIP_
InsertaFormatoCnt` directamente con los mismos 9 parámetros en el mismo
orden que usa `FormatosRepository.InsertarAsync` — generó el radicado
`2026090200000000005574`, insertó la fila esperada en `DQS_MIP_FORMATOS`
con `ESTADO='Cargado'`, y se confirmó el `RUTA` calculado por el propio SP.
Fila de prueba eliminada después. La consulta parametrizada de
`BuscarPrestadorAsync` se probó por separado contra `DQS_MIP_COD_HAB` y
devuelve resultados correctos. **Pendiente**: verificación visual de la UI
(formulario, mensajes de validación, subida de archivo real) — delegada al
usuario, no completada por el agente en esta ronda.

### Página 5 (✅ migrada): Consultar formato (`JuntasFormatoCntCO.aspx` → `/formatos/consultar`)

Formulario de búsqueda + grilla de resultados + exportación a Excel. Misma
disciplina de verificación que la página anterior: todo confirmado contra la
base de pruebas (`sp_MIP_ConsultaFormatosCNT` ejecutado directamente, texto
real del procedimiento leído con `OBJECT_DEFINITION`), sin usar el navegador
(ver nota de estabilidad de sesión).

- **`sp_MIP_ConsultaFormatosCNT` solo tiene 7 parámetros reales** (`@CIA,
  @TIPO, @ID, @NIT, @RADICADO, @FECHA, @USUARIO`), confirmado leyendo el
  texto del procedimiento — aunque `JuntasFormatoCntCO.aspx.vb` arma un
  objeto (`objent`) con **8 campos** (`stRCAMPO1..8`). Los campos 7 y 8
  (`txtfechafin.Value` y `ddlestado1.SelectedValue`, "Fecha Fin" y "Estado")
  **nunca llegan al SP** como parámetro. **Decisión revisada (2026-09-02,
  el usuario mostró una captura del panel real mostrando ambos filtros
  activos)**: la primera versión de esta página los omitió por completo,
  asumiendo que eran controles decorativos — pero si en la UI real sí
  filtran, la única explicación es que la BLL compilada (fuente no
  disponible, mismo caso ya documentado para `RangoFechasAct`) los aplica
  como filtro **en memoria sobre el DataTable ya devuelto por el SP**, no
  como parámetro de la consulta SQL. Se implementó así:
  `FormatosService.ConsultarAsync` llama siempre al SP con `@FECHA="%"`
  (trae todo lo que cumpla el resto de criterios) y filtra el resultado en
  C# por rango `[Fecha Inicio, Fecha Fin]` (ambos límites inclusive,
  cualquiera de los dos puede ir solo) y por `Estado` (exacto, salvo
  "Todo" = sin filtrar). "Fecha Inicio"/"Fecha Fin" y "Estado" siguen sin
  contar para la validación de "mínimo un criterio" — igual que en el
  legado, donde tampoco contaban.
- **El SP tiene una rama de comportamiento por rol**, resuelta internamente
  con `SELECT ROL FROM DQS_REC_USUARIOS WHERE USUARIO=@USUARIO`:
  - Rol `601` (el único que atiende esta app): filtra además
    `AND USUARIO LIKE @USUARIO` (cada usuario ve solo sus propias cargas) y
    la columna `Gestion` siempre llega vacía (`''`).
  - Cualquier otro rol (o un usuario que no exista en `DQS_REC_USUARIOS`,
    porque la comparación `NULL='601'` es falsa): NO filtra por usuario (ve
    **todos** los formatos de la Cía) y agrega una columna `Gestion` con un
    enlace HTML armado por concatenación de SQL, que abre
    `JuntasFormatoCntCO2.aspx` (popup de gestión). **Fuera de alcance
    deliberado**: esta app solo sirve usuarios de rol 601 (confirmado en el
    login), así que solo se implementó la rama de rol 601;
    `JuntasFormatoCntCO2.aspx` no se migra.
  - Verificado en vivo: `EXEC sp_MIP_ConsultaFormatosCNT` con
    `@USUARIO='cwformatos'` devuelve solo sus propias filas (7 con fecha
    2025-09-30); con un usuario inexistente devuelve las de **toda la Cía**
    (11 filas, rama "else") — confirma el comportamiento documentado, no un
    riesgo introducido por el código nuevo (el endpoint de descarga y el
    formulario siempre usan el `usuApp` real de la sesión autenticada, que
    en esta app siempre resuelve a rol 601).
- **Validación replicada exactamente** (`btnconsultar_ServerClick` e
  `imgexporta_Click`, idénticas, mensajes vía `Tran`): si "No documento",
  "Código de Habilitación - Prestador", "No Radicado" y "Fecha" están **los
  4 vacíos** → "Ingrese mínimo un criterio de búsqueda"; si "No documento"
  tiene valor pero "Tipo de documento" es "Ninguno" (o viceversa) → "Debe
  ingresar no documento y tipo de documento". Los campos vacíos que sí
  aplican se sustituyen por `"%"` antes de llamar al SP (igual que el
  legado); `@USUARIO` siempre es el usuario de la sesión, nunca un campo del
  formulario (el control legado `txtusuario` es engañoso: pese al nombre,
  es el campo "No documento", no el usuario).
- **Exportar a Excel**: mismo patrón que ya usa Recobros —
  `Contingencia.Web/Services/{IExcelExportService,ExcelExportService}.cs`
  (port exacto, mismo wrapper `ClosedXML.Excel.XLWorkbook`) +
  `wwwroot/js/descargas.js` (port exacto, base64 → Blob → click), registrado
  en `App.razor`. Réplica de `dt.Columns.RemoveAt(12)` de
  `imgexporta_Click`: en la rama de rol 601 esa posición es `ESTADO`, así
  que la tabla exportada excluye esa columna (se arma explícitamente sin
  ella, en vez de removerla por índice de un `DataTable` genérico).
- **"Ver" formato — endpoint protegido nuevo, no una URL pública**: el
  legado sirve el archivo directo desde `~/MediaUploader/Formatos/...` sin
  ningún control de acceso. Se agregó `GET /api/formatos/archivo?radicado=
  ...&extension=...` (`Contingencia.Web/Program.cs`) que exige sesión activa
  y **confirma que el radicado pertenezca al usuario autenticado**
  reutilizando `IFormatosService.ConsultarAsync` con el mismo filtro que ya
  aplica el SP para rol 601 — evita que un usuario adivine el radicado de
  otro y lo descargue. `Path.GetFileName` sobre `radicado`/`extension`
  descarta cualquier intento de path traversal.
- **`RangoFechasAct()` no se replicó**: pone por defecto tanto "Fecha Inicio"
  como "Fecha Fin" con la misma fecha en `Page_Load`, pero es un método de
  la BLL legada compilada (no hay procedimiento de base de datos equivalente
  — se buscó explícitamente y no existe). Como el formulario nuevo no tiene
  "Fecha Fin" (ver arriba) y como este es un filtro opcional, no un
  requisito, se dejó el campo "Fecha" vacío por defecto en vez de adivinar
  el comportamiento exacto de una BLL sin fuente disponible.
- **Duplicado de la inyección SQL de `JuntasFormatoCntC.aspx.vb`, ya
  corregido**: el `WebMethod Ciudad` de esta página es un duplicado
  byte-a-byte del ya encontrado/corregido en "Cargar formato" — se reutiliza
  directamente `FormatosService.BuscarPrestadoresAsync` (misma consulta
  parametrizada contra `DQS_MIP_COD_HAB`), no se duplicó el código.

**Verificado contra la base de pruebas (2026-09-02)**, sin navegador: se leyó
el texto real de `sp_MIP_ConsultaFormatosCNT` con `OBJECT_DEFINITION` (7
parámetros, rama por rol confirmada), se ejecutó directamente con los
parámetros exactos que arma `FormatosRepository.ConsultarAsync` filtrando
por un radicado real de `cwformatos` (`2025093000000000002639`) — columnas y
valores devueltos coinciden uno a uno con el mapeo de
`FormatosService.ConsultarAsync` a `FormatoConsultado`. También se confirmó
que sin filtro de radicado, `cwformatos` solo ve sus propias 7 filas del
30/09/2025.

**Filtros "Fecha Inicio/Fecha Fin/Estado" verificados en vivo con Claude in
Chrome (2026-09-02, tras la corrección de la decisión anterior)**: con
Fecha Inicio=Fecha Fin=30/09/2025 aparecen exactamente las 7 filas
conocidas de `cwformatos` para ese día; con el rango 01/10/2025–31/10/2025
aparecen ~190 filas, todas con fecha dentro de ese rango (confirma que
`cwformatos` es una cuenta compartida del equipo de captura — "FORMATOS
UNIVERSAL" — no de una sola persona, de ahí el volumen); filtrando además
por Estado="Devuelto" sobre ese mismo rango, las 27 filas resultantes
tienen **todas** Estado="Devuelto" exacto, confirmando que el filtro en
memoria funciona correctamente en combinación con el rango de fechas.
**Pendiente**: verificación visual del resto de la UI (formulario, grilla,
descarga de Excel, enlace "Ver") — delegada al usuario, no completada por el
agente en esta ronda.

### Página 6 (✅ migrada): Gestionar formato (`JuntasFormatoCntCO2.aspx` → modal en `/formatos/consultar`)

A diferencia de las páginas anteriores, esta NO estaba en el alcance original
de la migración — la "Página 5" la había documentado explícitamente como
**fuera de alcance** ("esta app solo sirve rol 601"). El usuario pidió
ampliar el alcance (2026-09-02): los roles **27, 29 y 30** también deben
poder ver el ícono de lápiz "Gestión" en Consultar formato y usar la
funcionalidad de `JuntasFormatoCntCO2.aspx` — migrada a un **modal en la
misma pantalla**, no a una página/ventana aparte como el legado.

- **No existe en Login3.aspx.vb ningún caso especial para 27/29/30** — ahí
  caen al `myremotepost.Url = "InicioMIPRES.aspx"` por defecto (otra app
  distinta, no migrada). Es una decisión **nueva**, no una réplica: se
  agregó una rama en `LoginService.DeterminarDestino` que manda esos 3
  roles directo a `/formatos/consultar` (no a `/inicio` con las 2 tarjetas,
  porque estos roles gestionan formatos ajenos, no cargan los propios).
- **Confusión real de dos columnas de rol, resuelta**: el claim
  `ClaimTypes.Role` que ya existía en esta app viene de la columna 1 del SP
  de login (`Session("rol")` en el legado) — un valor **distinto** de la
  columna 4 (`Session("idrol")`, `ResultadoUsuario.RolParaModulos`), que es
  la que Login3.aspx.vb realmente usa para decidir destino/branching por
  rol (`idrol="601"`, etc.). El control de acceso al modal de gestión
  necesitaba el idrol real, que nunca se guardaba en la tabla puente — se
  agregó una columna nueva **aditiva** `IdRol` a `dbo.ImperiumSesionPuente`
  (`Imperium.Infrastructure/Sql/005_AgregaIdRolPuenteSesion.sql`, NULLable,
  sin default — no rompe a Recobros, que nunca la escribe ni la necesita) y
  un claim nuevo `imperium:idrol` (`ClaimsPrincipalExtensions.IdRol()` /
  `.PuedeGestionarFormatos()`, esta última con la lista `{27,29,30}` —
  decisión de la capa nueva, el SP no distingue más que "601 vs. cualquier
  otro rol").
- **El SP de guardado no valida el rol**: `sp_MIP_ActualizaFormatoCnt`
  (`@CIA, @mipres, @obj, @estado, @usuario, @radicado`) actualiza
  `DQS_MIP_FORMATOS` por `CIA+RADICADO` únicamente — `@usuario` se envía
  por fidelidad con el legado pero el `UPDATE` real no lo usa. El control
  de "solo 27/29/30 pueden gestionar" vive 100% en `Contingencia.Web`
  (oculta la columna/botón en la UI para otros roles); no se tocó el SP.
- **Validación replicada exactamente** (`btnguardar_ServerClick` en
  `JuntasFormatoCntCO2.aspx.vb`): "No Prescripción" vacío → "Debe ingresar
  el mipres"; Estado en "Seleccionar" (o vacío) → "Debe ingresar el
  estado"; si Estado="Gestionado", valida la prescripción contra una
  **API real del gobierno** antes de aceptar (ver abajo) — si no es válida
  → "No prescripción no valida".
- **Dependencia externa real replicada tal cual** (decisión explícita del
  usuario: "replicarla tal cual"): antes de aceptar el estado "Gestionado",
  el legado llama `https://wsmipres.sispro.gov.co/WSMIPRESNOPBS/api/
  PrescripcionXNumero/{NIT}/{TOKEN}/{noPrescripcion}` — primero con el
  token de régimen Contributivo, y si la respuesta no contiene el número,
  reintenta con el de Subsidiado. Los tokens salen de `DQS_MIP_TOKEN` vía
  `sp_DQS_ConsultaFiltros` con `@INTVALIDA=136` (mismo SP "multiplexor" que
  ya usa Recobros — se portó `IFiltrosRepository`/`FiltrosRepository` sin
  cambios). Implementado en `Contingencia.Infrastructure/Mipres/
  MipresPrescripcionValidator.cs`. **Diferencia deliberada frente al
  legado**: el legado no envuelve esta llamada en Try/Catch (un fallo de
  red tumbaba la página WebForms entera); acá si falla, se atrapa y se
  muestra un mensaje amigable en vez de tumbar el circuito de Blazor Server
  del usuario — mismo comportamiento visible para el caso feliz, más
  resiliente en el caso de fallo de red.
- **EMAIL no viene en las columnas del SP de consulta** (solo se usa
  internamente para armar el HTML del enlace "Gestion" en la rama de otros
  roles) — se agregó `FormatosRepository.ConsultarEmailAsync` (SQL directo
  parametrizado contra `DQS_MIP_FORMATOS`, mismo criterio que
  `BuscarPrestadorAsync`) en vez de parsear el HTML del legado.
- **Correo de confirmación replicado** (`crearcorreo()` en
  `JuntasFormatoCntCO2.aspx.vb`): mismo mensaje real ("Su radicado: X ha
  sido {estado}. No Prescripción: Y..."), plantilla modernizada/simplificada
  (`PlantillaConfirmacionGestion.cs`, mismo criterio ya usado por
  `PlantillaRecuperacionClave` — no se copiaron los ~400 líneas de
  boilerplate MSO/Outlook del legado). Se envía en `Try/Catch` vacío, igual
  que el legado: si el SMTP falla, no revierte el `UPDATE` ya aplicado.
- **Modal en vez de ventana/página aparte**: el legado abre
  `JuntasFormatoCntCO2.aspx` en una ventana nueva vía `ventana2(radicado,
  mipres, obj, estado, email)` (JS armado dentro del HTML que devuelve el
  SP). Acá es un modal propio de `ConsultarFormato.razor` (mismos 3 campos:
  No Prescripción, Observaciones, Estado — dropdown idéntico
  Seleccionar/Devuelto/Gestionado), pre-poblado con los datos que ya trae
  la fila (no hace falta una llamada aparte), y que al guardar refresca la
  tabla con la misma búsqueda ya aplicada.

**Verificado en vivo con Claude in Chrome (2026-09-02)**, usuario real
`hans` (rol 29, CIA 890330031, `Imperium`/`data2023*` es un usuario de
prueba equivalente también rol 29): login → redirigió directo a
`/formatos/consultar` (no a `/inicio`) confirmando el nuevo destino; la
columna "Gestión" con el lápiz apareció correctamente; el modal precargó
los datos reales de un radicado de `cwformatos` (confirma que rol 29 ve
formatos de OTROS usuarios, tal como hace la rama "else" del SP); Guardar
con Estado="Gestionado" y el número de prescripción real de ese registro
disparó la llamada real a la API del gobierno y la aceptó ("Confirmación
correcta" — la prescripción sí existe y ya estaba gestionada en el sistema
real de MIPRES); una segunda prueba cambiando Observaciones a un valor de
prueba y Estado a "Devuelto" confirmó, leyendo directo la base de datos,
que el `UPDATE` real se aplicó — valor de prueba restaurado al original
después. **No verificado**: el envío del correo de confirmación (SMTP sin
configurar en este entorno de prueba, igual que el resto del proyecto).

### Página 7 (✅ migrada): Buzón PQR (`Sugerencias.aspx` → `/sugerencias`)

Último enlace del menú lateral que todavía apuntaba al legado (`AppLayout.
razor`, "PQR", vía `LegacyUrlBuilder.Url("Sugerencias.aspx")`) — ahora
apunta a `/sugerencias`. **No usa ninguna tabla ni procedimiento
almacenado**: es un formulario simple que arma y envía un correo, mismo
mecanismo de `IEmailSender`/`EmailSender` ya usado por Login (recuperar
clave) y por el modal de Gestionar formato.

- **Ojo con confundir dos archivos legados distintos**: el enlace "PQR" de
  esta app (familia Mipres, master `SiteMIPRESN.Master`) apunta al archivo
  `Sugerencias.aspx` — que **no es el mismo archivo** que ya migró Recobros
  a su propio `/sugerencias` (`Imperium.Web`), el cual viene de
  `SugerenciasREC.aspx.vb` (familia Recobros, `Site.Master`). Son dos
  páginas legadas con el mismo propósito y una estructura casi idéntica,
  pero con **destinatarios y asunto reales distintos** — se leyó
  `Sugerencias.aspx.vb` (el archivo correcto para Contingencia) en vez de
  asumir que compartía el código de Recobros. Sí se usó Recobros como guía
  estructural para la página Razor/CSS (`Sugerencias.razor`/`.razor.css`,
  prácticamente idénticos a los de Recobros salvo el nombre del namespace),
  igual que en el resto de esta migración.
- **Destinatarios reales confirmados**: `soporte2@dqualitysolutions.com.co`,
  `pipito1001@gmail.com`, `dataquality.comercial@gmail.com`,
  `dataquality.pqr@gmail.com` (4, no 3 como en Recobros — ese archivo tiene
  un destinatario adicional que `SugerenciasREC.aspx.vb` no tiene).
- **Asunto real confirmado**: `"Notificación PQR Imperium-Mipres"` (distinto
  del `"Notificación Buzón Imperium-Mipres"` que usa Recobros).
- **Validación replicada exactamente** (`btnconsultar_ServerClick`, único
  chequeo real del legado): email vacío → "Ingrese el email"; envío
  exitoso → "Mensaje enviado" (limpia los campos); fallo de SMTP → "No es
  posible enviar el mensaje" (capturado en `Try/Catch`, igual que el
  legado).
- **Vulnerabilidad del legado NO replicada** (mismo criterio ya aplicado en
  Recobros para su propia versión): `crearcorreo()` fijaba
  `ServicePointManager.ServerCertificateValidationCallback` para aceptar
  cualquier certificado — una configuración **global del proceso** (no solo
  de esa conexión SMTP) que deshabilitaba la validación TLS de *todas* las
  conexiones salientes de la app mientras esa página se ejecutara. La
  migración usa la validación de certificado normal de .NET.
- **Escapado agregado, no presente en el legado**: los campos libres (tipo,
  mensaje, email, usuario) se insertan en la plantilla con
  `WebUtility.HtmlEncode` antes de armar el correo — el legado los
  concatenaba sin escapar. El destino son bandejas de soporte interno
  (riesgo bajo), pero es una mejora trivial que no cambia el comportamiento
  visible.
- **Pie del correo corregido, no replicado literalmente**: el legado firma
  "© 2017 DQS RECOBROS..." pese a ser la página de Mipres/Contingencia —
  casi con certeza el mismo error de copiar/pegar ya documentado en otras
  plantillas de este cliente. Se reemplazó por "© {{año actual}}
  Imperium-Mipres..." (mismo criterio ya usado en `PlantillaRecuperacionClave`
  de no fijar un año viejo) en vez de perpetuar una marca ajena a esta app.

**Verificado en vivo con Claude in Chrome (2026-09-02)**: con `cwformatos`
logueado, el formulario carga correctamente bajo el mismo `AppLayout`;
enviar sin email muestra "Ingrese el email"; completar email y mensaje y
enviar muestra "No es posible enviar el mensaje" — confirma que el intento
real de envío (vía `IEmailSender.EnviarAsync`) se ejecutó y falló de forma
controlada porque el SMTP no está configurado en este entorno de prueba
(mismo estado que el resto de la app, no un defecto de esta página).

### Página 8 (✅ migrada): Junta Profesional (`JuntaProfesional.aspx` → `/juntas`)

Segundo flujo de rol servido por esta misma app/solución (rol **60**, CIA
890330031, usuario de prueba real `cwjuntas` — confirmado con el usuario:
"solo sale una opción en el menú"). A diferencia de las páginas de
Formatos, esta consulta una **API externa del gobierno en vivo**
(`wsmipres.sispro.gov.co`) como fuente principal de datos — la base de
datos aquí solo resuelve credenciales del WS y el listado de integrantes de
junta que se estampan en el acta, no los datos de fondo.

- **Confirmado con `sp_DQS_ConsultaModxRol @STRCIA='890330031',
  @STRROL='60'`**: devuelve exactamente una fila, `Junta Profesionales`
  (nombre con "s" al final, distinto de "Junta Profesional" sin ella que
  usa el resto de la UI) — coincide con lo que reportó el usuario. Sidebar
  gateado con `context.User.TieneModuloExacto("Junta Profesionales")`,
  mismo patrón que ya usaba "Contingencia" para el otro flujo.
- **Filtros del formulario, réplica fiel de `Page_Load`/
  `btnconsultar_ServerClick`**: Fecha Inicio y Fecha Fin (ambas con hoy por
  defecto), Estado (opcional, `%`=todos), Régimen (Contributivo por
  defecto; **Subsidiado si `Session("cia")="ESSC18"`**, una EPS distinta a
  Sanitas — replicado tal cual aunque nunca dispara para el único CIA real
  de esta app), N° Prescripción (uno o varios separados por coma/espacio/
  salto de línea), Tipo y N° de documento del paciente.
- **Lógica de consulta, réplica EXACTA de `btnconsultar_ServerClick`/
  `api()` — incluyendo dos comportamientos que a primera vista parecen
  errores pero son el comportamiento real del legado**:
  - Si Fecha Inicio = Fecha Fin (un solo día): si hay N° de Prescripción,
    consulta la API **una vez por cada número** (`JuntaProfesional/{nit}/
    {token}/{numeroPrescripcion}`); si no, consulta una vez por fecha
    (`JuntaProfesionalXFecha` o, si además hay documento de paciente,
    `JuntaProfesionalXPaciente`).
  - Si Fecha Inicio ≠ Fecha Fin (rango de varios días): recorre **cada día
    del rango por separado** (una llamada HTTP por día, secuencial — igual
    de lento que el legado, no se paralelizó) y **el filtro de N°
    Prescripción se ignora por completo**, aunque el usuario lo haya
    llenado. Esto no es una simplificación de la migración: es literalmente
    lo que hace `btnconsultar_ServerClick` (la rama del `If diferencia = 0`
    es la única que mira `txtpres.Value`).
  - Cada llamada HTTP fallida (red, JSON, lo que sea) se descarta en
    silencio sin abortar el resto — mismo `Catch` vacío de `api()`; un día
    problemático dentro de un rango largo no tumba la consulta completa.
  - NIT/TOKEN se resuelven en cada llamada vía `sp_DQS_ConsultaFiltros
    @INTVALIDA=136` (mismo SP/parámetro que ya usa
    `MipresPrescripcionValidator` del otro flujo) con `@STROPCION` según
    `Session("token")="IPS"` (`APIPRESCRIPCION2` vs. `APIPRESCRIPCION`,
    columna 18 del SP de login — ver "Columna `TokenMipres`" abajo).
- **Botón "Ver acta en PDF" (`gvconsulta_OnRowCommand`, `CommandName=
  "PDF"`)**: genera el acta al vuelo (no se guarda en disco, igual que el
  legado) contra `GET /api/juntas/acta?prescripcion=...` (protegido con
  `[Authorize]`, la CIA sale de la sesión, nunca de un parámetro). Réplica
  fiel:
  - Dos llamadas más a la API, **siempre con régimen "Contributivo"**
    (hardcodeado en el legado, sin importar el régimen usado en la
    búsqueda): `JuntaProfesional/{nit}/{token}/{noPrescripcion}` (trae
    número de acta, fecha, modalidad, justificación y decisión) y
    `PrescripcionXNumero/{nit}/{token}/{noPrescripcion}` (trae los datos
    clínicos completos: prestador, paciente, y las 5 listas de tecnologías
    — medicamentos, procedimientos, dispositivos, productos nutricionales,
    servicios complementarios).
  - Decenas de códigos (municipio, IPS, diagnóstico, ámbito de atención,
    vía de administración, forma farmacéutica, CUPS, dispositivo, servicio
    complementario) se traducen contra **`DQS_MIP_CRUZAR`**
    (`sp_DQS_ConsultaFiltros @INTVALIDA=138`, "consulta maestro Mipres" en
    el legado — trae TODA la tabla, sin filtro por CIA, y se cachea en
    memoria por cada generación de acta). **Ojo con mayúsculas/minúsculas**:
    el nombre real de la columna `CAMPO` en la base no siempre coincide en
    mayúsculas con el que usa el código legado (`NroIdIps` en la tabla vs.
    `NroIDIPS` en el código, `CODCUPS` vs. `CodCUPS`) — el cruce se hace
    sin distinguir mayúsculas (igual que hace SQL Server con la collation
    de esta base; si no fuera así, el legado nunca habría encontrado nada).
  - Los integrantes de la junta ("INTEGRANTES DE LA JUNTA PROFESIONAL DE LA
    SALUD") salen de **`sp_DQS_ConsultaProfesionalAñadido(@CIA, @ACTA)`**
    — join de `DQS_MIP_PROFESIONALESACTA` (qué profesionales firmaron cada
    acta) con `DQS_MIP_PROFESIONALES` (nombre/profesión de cada uno) por
    CIA+NoIDENTIFICACION.
  - **Bug real del legado corregido, no replicado**: en la sección
    "PROCEDIMIENTOS" del acta, la columna "Indicaciones" lee
    `dispositivos(i).IndRec` (¡de otra lista!, mismo índice `i`) en vez de
    `procedimientos(i).IndRec` — un copy/paste que además puede tirar
    `IndexOutOfRange` en el legado cuando una prescripción tiene más
    procedimientos que dispositivos. Se corrigió para que repita su **propio**
    `IndRec`, exactamente el mismo patrón que ya usan, sin ese bug, las
    secciones de Dispositivos y Servicios Complementarios (ambas repiten su
    propio `IndRec` en esa misma columna). Es la única corrección de
    contenido de esta migración — todo lo demás es maquetación 1:1.
  - **Exclusión deliberada, no un olvido**: el legado tiene un bloque
    completo de firmas fijas hardcodeadas ("ANGELA MARIA CABAL PEREZ",
    etc., ~50 líneas) que solo se activa cuando `Session("CIA")=
    "900971006"` — una EPS totalmente distinta a Sanitas (890330031, la
    única que usa esta app). Nunca se ejecutaría para ningún usuario real
    de Contingencia, así que no se portó (ver `ActaPdfBuilder.cs`).
  - **"Departamento" y "Municipio" muestran literalmente el mismo valor**
    en el acta — así estaba en el legado (nunca hubo un dato de
    departamento separado, ambas celdas leen la misma variable
    `municipio`), replicado tal cual sin "corregirlo".
  - Una celda del legado (`"DATOS DEL PACIENTE"`) declara `Colspan=3` en
    una tabla de solo 2 columnas (copy/paste de la tabla de 3 columnas de
    arriba) — iTextSharp lo toleraba silenciosamente, pero **iText7 (la
    librería usada acá, ver abajo) sí valida los límites de columna** y
    hubiera reventado la generación del PDF; se corrigió el colspan a 2.
- **Botones "Cargas" y "Firma" de cada fila**: migrados como **modales
  dentro de la misma página** (2026-09-03, ver "Página 9" más abajo) — ya
  no enlazan a `JuntasCargueDoc.aspx`/`JuntasFirma.aspx` en una pestaña
  nueva del legado.
- **Exportar a Excel**: mismo patrón ya usado en Consultar formato
  (`IExcelExportService` + `wwwroot/js/descargas.js`), exportando las 15
  columnas de `JuntaProfesionalDetalle` (equivalente 1:1 a la clase
  `JuntaProfesional` del legado, sin remover ninguna columna a diferencia
  de Formatos).
- **PDF generado con iText7, no iTextSharp**: iTextSharp (la librería que
  usa el legado) solo corre en .NET Framework — se usó **iText7**
  (`itext7` + `itext7.bouncy-castle-adapter`, paquetes NuGet nuevos en
  `Contingencia.Infrastructure`), la continuación oficial mantenida para
  .NET moderno, mismo criterio de "reemplazar la librería, no la lógica"
  ya aplicado en otras partes de esta migración.
  - **Bug real encontrado y corregido en la primera prueba en vivo**:
    `itext7` por sí solo tira `NotSupportedException` ("Either
    com.itextpdf:bouncy-castle-adapter or ...-fips-adapter dependency must
    be added...") al intentar escribir CUALQUIER PDF, incluso sin cifrarlo
    — necesita un proveedor de criptografía para el serializador interno
    de `PdfWriter`. Se agregó `itext7.bouncy-castle-adapter`. Esa
    dependencia trae a su vez `BouncyCastle.Cryptography 2.2.1`, con 3 CVEs
    moderados conocidos (`GHSA-8xfc-gm6g-vgpv`, `GHSA-m44j-cfrm-g8qc`,
    `GHSA-v435-xc8x-wvr9`) — se fijó explícitamente la versión `2.6.2`
    (parcheada) con un `PackageReference` directo.
  - Réplica del pie de página del legado (`itsEvents` en
    `ConsultaMIPRES.aspx.vb`): número de página y "Copia generada desde
    IMPERIUM.NET.CO || MIPRES NO PBSUPC - fecha de impresión: ..." en cada
    página, vía un `IEventHandler` sobre `PdfDocumentEvent.START_PAGE`
    (equivalente a `PdfPageEventHelper.OnStartPage` en iTextSharp).
- **Columna nueva `TokenMipres` en `dbo.ImperiumSesionPuente`** (aditiva,
  igual criterio que `IdRol` en la "Página 6"): guarda la columna 18 del SP
  de login (`Session("token")` en el legado, ya leída como
  `ResultadoUsuario.Token` pero nunca persistida hasta ahora) — necesaria
  para decidir, en cada llamada a la API de SISPRO, si pedir
  `APIPRESCRIPCION` o `APIPRESCRIPCION2`. Se llama `TokenMipres` y no
  `Token` a secas porque esa tabla ya tiene una columna PK con ese nombre
  (el GUID de un solo uso de la sesión puente) —
  `Imperium.Infrastructure/Sql/006_AgregaTokenMipresPuenteSesion.sql`,
  NULLable, sin default, aplicada contra la base de pruebas.

**Verificado en vivo con Claude in Chrome (2026-09-02)**, login real con
`cwjuntas`/captcha real (sin bypass): redirigió a `/juntas` correctamente;
sidebar mostró **únicamente** "Junta Profesional" y "PQR" (confirma el
único módulo real del rol 60); logo de EPS Sanitas visible. Consulta real
contra la API de SISPRO con Fecha Inicio=Fecha Fin=hoy: token/NIT reales
resueltos (`4C6B9830-...`/`800251440`, coinciden con `DQS_MIP_TOKEN` para
CIA 890330031/Contributivo), llamada HTTP real con `200 OK`, "Sin
resultados" (esperado, no hay actas programadas ese día). Rango de 13 días
(20/08–02/09/2026): 13 llamadas HTTP secuenciales, todas `200 OK` (300ms a
30s de latencia real, variable, propia de la API del gobierno — no un
problema de esta app), sin resultados en ese rango tampoco (dato real,
no hallado un caso de junta activo para esta CIA en esa ventana).
Generación de acta probada directamente contra `/api/juntas/acta` (número
de prescripción inexistente, a propósito, para forzar el camino "sin
datos" del PDF sin depender de encontrar un caso real): tras el fix de
BouncyCastle, el PDF se generó y renderizó correctamente — logo real
descargado desde `imperium.net.co`, las 4 tablas de encabezado/prestador/
paciente/clínica con su `rowspan`/`colspan` correctos, sección
"INTEGRANTES..." vacía sin errores, pie de página con número de página y
fecha de impresión reales. Exportar a Excel probado sobre una consulta con
0 resultados: sin errores de consola, genera el archivo igual (0 filas).
**No verificado por no encontrarse un caso real en la ventana de fechas
probada**: el contenido de las filas de medicamentos/procedimientos/
dispositivos/productos nutricionales/servicios complementarios dentro del
acta (los bloques condicionales solo se ejercitan si la prescripción
consultada realmente tiene alguno) — el resto de la tabla que los contiene
(encabezados de sección, `rowspan`/`colspan`, helpers de traducción de
código) sí quedó probado end-to-end con datos reales.

#### Dos bugs reales encontrados y corregidos tras el reporte del usuario ("la consulta no trae datos")

El usuario reportó, con una captura real de su propio entorno, que la
consulta de hoy (`02/09/2026`) no traía resultados — el mismo síntoma "sin
resultados" que ya se había dado por bueno en la verificación anterior
(dato real, se pensó). Investigar el reporte llevó a golpear la API real
directamente (fuera de la app, con `curl`/Python) con el NIT/TOKEN reales
de la CIA — **sí hay datos reales para esa fecha**, así que el síntoma era
un bug de la migración, no una ventana sin actividad real:

1. **La API de SISPRO no es consistente con los tipos de dato**: campos
   como `Consecutivo`, `EstJM` y `Modalidad` a veces llegan como **número
   JSON** (`"EstJM":2`) y no como texto (`"EstJM":"2"`) — el legado, con VB
   de tipado débil, nunca notó la diferencia (todo se concatena/compara
   como texto sin problema). `System.Text.Json` es estricto por defecto y
   tira `JsonException` al intentar convertir un número JSON directo a una
   propiedad `string` — excepción que el mismo `Catch` vacío de
   `ConsultarUnaAsync` (réplica fiel del `Catch` vacío de `api()` en el
   legado, ver arriba) se comía en silencio, aparentando "sin resultados"
   en vez de propagar el error real. Corregido con un
   `JsonConverter<string?>` propio (`FlexibleStringConverter`/
   `FlexibleStringConverterFactory`) que acepta string, número o `null`
   para **cualquier** propiedad `string` de los DTOs de esta API — no se
   anotó campo por campo porque no hay forma de saber de antemano cuáles
   de los ~80 campos entre las 6 clases (`JuntaProfesionalDetalle`,
   `MipresDatosPrescripcion`, `Medicamento`, `Procedimiento`, etc.) va a
   mandar la API como número en un caso real distinto.
2. **Varios campos de la API vienen en `null`** cuando el acta aún no
   existe (p.ej. `EstJM="2"`, pendiente de evaluación, deja `NoActa`,
   `FechaActa`, `Modalidad`, `JustificacionTecnica`, `CodEntProc` en
   `null`) — una vez corregido el bug anterior, esto reventó el circuito
   de Blazor completo (`ArgumentNullException` en
   `Uri.EscapeDataString(null)`, dentro de `UrlFirma`/`UrlCargas`/`UrlActa`
   al armar los enlaces de cada fila de la grilla) y, de haberse llegado a
   generar un acta con esos campos nulos, también habría reventado
   `ActaPdfBuilder` (`new Paragraph(null)` no es válido en iText7).
   Corregido en dos puntos: `Juntas.razor` sanea cada valor con `?? ""`
   antes de `Uri.EscapeDataString` (helper `Escapar`), y
   `ActaPdfBuilder.Texto` (el único punto por el que pasa todo el texto de
   las celdas del PDF) ahora acepta `string?` y sanea con `?? ""`.

**Re-verificado en vivo con Claude in Chrome tras ambos fixes**, mismos
criterios que el usuario reportó (`cwjuntas`, Fecha Inicio=Fecha Fin=hoy,
Contributivo): la consulta ahora sí trae filas reales (confirmado contra
la misma API directamente con `curl`/Python, mismos números de
prescripción); la grilla muestra Estado traducido a texto ("Aprobada",
"Pendiente evaluación") sin errores en consola del navegador ni en el
servidor. **Generación del acta probada con un caso real por primera vez**
(antes solo se había probado con una prescripción inexistente, sin datos):
el PDF de 2 páginas renderizó correctamente con datos clínicos reales —
prestador, paciente, diagnóstico, y la fila de "MEDICAMENTOS" con
traducciones de código correctas (`FrecuenciaAdministración`,
`Indicaciones especiales`, `Duración tratamiento` en texto legible, no
códigos crudos) — confirmando que las 5 secciones condicionales de
tecnologías, sin probar en la ronda anterior, también funcionan.
Exportar a Excel con resultados reales (no 0 filas como antes): sin
errores de servidor ni de consola.

#### Tres pedidos adicionales del usuario tras la corrección anterior

Con la consulta ya trayendo datos reales, el usuario reportó tres cosas
más en la misma ronda: "el exportar a excel no funciona", faltaban columnas
en la grilla (`CodEntProc`, `JustificacionTecnica`, `Modalidad`, `FProceso`,
`TipoIDPaciente`, `NroIDPaciente`, `CodEntJM` — el resto de las 15
propiedades de `JuntaProfesionalDetalle`, antes solo se mostraban 8), y
pidió que la tabla fuera paginada y que la consulta fuera "lo mas rapida
posible".

- **Bug real de Exportar a Excel — dos causas distintas, ambas corregidas**:
  1. **`DataRow` no acepta `null` crudo.** .NET exige `DBNull.Value`
     explícito para representar un valor nulo en una fila de `DataTable` —
     `tabla.Rows.Add(f.CodEntProc, ...)` tiraba `ArgumentNullException` en
     cuanto CUALQUIERA de los 15 campos venía en `null` (algo que, como se
     confirmó arriba, pasa todo el tiempo con esta API). Corregido con un
     helper `N(string? valor) => (object?)valor ?? DBNull.Value` aplicado a
     los 15 campos antes de `Rows.Add`.
  2. **Bug de sintaxis de Razor, más grave — el botón estaba SIEMPRE
     deshabilitado sin importar el estado real**: `disabled="@exportando ||
     resultados is null"` — en Razor, `@` solo enlaza la expresión C#
     inmediatamente siguiente (`exportando`); todo lo que sigue
     (`|| resultados is null`) se renderiza como **texto literal** pegado al
     valor del atributo (`disabled="False || resultados is null"`), y en
     HTML cualquier valor no vacío de `disabled` — incluida esa cadena de
     texto — activa el estado deshabilitado. El botón nunca se habilitaba,
     sin importar cuántos resultados hubiera. Corregido envolviendo TODA la
     expresión entre paréntesis: `disabled="@(exportando || resultados is
     null)"` (patrón que las dos flechas de paginación agregadas en esta
     misma ronda ya usaban bien, por eso no tenían el bug). **Esta era la
     causa real y única de "el exportar a excel no funciona"** — encontrada
     inspeccionando el DOM en vivo (`outerHTML` del botón mostraba
     literalmente el texto de la condición sin evaluar), no adivinada.
- **Columnas completas**: la grilla de `/juntas` ahora muestra las 15
  columnas de `JuntaProfesionalDetalle` (antes solo 8), igual que
  `AutoGenerateColumns="true"` en el legado (que no oculta ninguna
  propiedad).
- **Paginación en memoria**: 25 filas por página (`fmt-paginacion`,
  controles Anterior/Siguiente + "Página X de Y (N resultados)") —
  necesaria porque un rango de varios días real puede traer miles de filas
  (ver dato real de prueba abajo).
- **Consulta más rápida — dos optimizaciones, ninguna cambia el resultado
  final, solo cómo se obtiene**:
  1. **NIT/TOKEN se resuelven UNA sola vez por búsqueda**, no una vez por
     cada día del rango. El legado (y la primera versión de esta
     migración) llama `sp_DQS_ConsultaFiltros` en cada iteración aunque el
     resultado es siempre el mismo dentro de una misma búsqueda (mismo
     CIA+Régimen+tipo de token) — ahora `IJuntaProfesionalApiClient.
     ResolverCredencialesAsync` se llama una vez y el NIT/TOKEN resuelto se
     pasa a cada llamada. Aplica también a `GenerarActaPdfAsync` (antes
     resolvía credenciales dos veces, una por cada llamada a la API).
  2. **Las llamadas HTTP a SISPRO se disparan en paralelo** (tope de 8
     simultáneas vía `SemaphoreSlim`, para no saturar la API real del
     gobierno) en vez de una tras otra — el legado, y la primera versión
     de esta migración, esperaban cada respuesta antes de pedir la
     siguiente. También en `GenerarActaPdfAsync`: las dos llamadas HTTP
     (acta + detalle de prescripción) y la carga del cruce de códigos
     (`DQS_MIP_CRUZAR`) ahora corren en paralelo entre sí (son
     independientes), no en secuencia.
     
     Esto es una **decisión explícita de priorizar velocidad sobre
     replicar el comportamiento secuencial del legado** — a diferencia del
     resto de esta migración (que replica bugs/quirks del legado por
     defecto), acá el usuario pidió explícitamente "la consulta debe ser
     lo mas rapida posible", y la secuencialidad del legado nunca fue un
     comportamiento de negocio a preservar, solo una limitación de cómo
     estaba escrito el código VB original.

**Verificado en vivo con datos reales**: un rango de 13 días
(20/08–02/09/2026) para CIA 890330031 trajo **3.558 resultados reales**
(273/día en promedio — un volumen real considerable, confirma por qué la
paginación y la velocidad importan) en **~24 segundos** con las llamadas en
paralelo, contra la ejecución secuencial original de la misma búsqueda que
había tomado más de 80 segundos en una prueba anterior con un rango más
corto (13 días con menos llamadas simultáneas) — mejora sustancial, aunque
el techo sigue siendo la latencia de la llamada más lenta del lote (la API
real del gobierno varía entre ~300ms y ~30s por llamada, fuera del control
de esta app. Exportar a Excel probado con las 3.558 filas reales (muchas
con campos `null`): completa sin errores de servidor ni de consola,
confirmando el fix de `DBNull.Value`. Paginación probada con el mismo
resultado: 143 páginas de 25 filas, navegación Anterior/Siguiente
funcional.

Archivos tocados en esta ronda:
`Contingencia.Infrastructure/Mipres/{IJuntaProfesionalApiClient,
JuntaProfesionalApiClient}.cs` (credenciales resueltas aparte),
`Contingencia.Application/Juntas/JuntasService.cs` (paralelización con
`SemaphoreSlim`), `Contingencia.Web/Components/Pages/Juntas.razor`
(columnas, paginación, fix de `DBNull` y del `disabled` mal parentizado).

#### Ampliación (✅, 2026-10-01): "Enviar acta por correo" — portada desde el legado tras detectarse una adición reciente

A pedido del usuario ("revisar si hay cambios en el original de juntas para
traerlo al [Blazor] migrado... primero valida antes de ejecutar"), se
comparó el legado contra lo ya migrado. Hallazgo: `JuntaProfesional.aspx` /
`.aspx.vb` en el repo legado (`Imperium 2026 core/Imperium`, **sin
historial de git** — carpeta `Imperium/`/`BLL/`/`DAL/`/`ENT/` enteras
untracked, confirmado con `git status`) tenían fecha de modificación
`2026-10-01`, posterior a la última migración conocida (`2026-09-25`). Se
leyeron las 2104 líneas del `.vb` completas y se compararon contra lo
documentado acá: **una sola adición real**, la región `#Region "CORREO"`
(líneas 1881-2081) — un botón nuevo "enviar acta por correo" junto al de
Firma, con modal (destinatario(s)/asunto/observación) y envío por SMTP con
el PDF adjunto. Nada más del archivo cambió (se re-verificó línea por línea
contra "Página 8": `Page_Load`, `GenerarActaPdf` con sus bugs ya conocidos,
el bloque hardcodeado de firmas para CIA `900971006`, todo idéntico). Cero
procedimientos almacenados nuevos — la función de correo es 100% .NET
(`SmtpClient`) + archivo en disco, reutilizando `GenerarActaPdf` que ya
existía.

**Decisión de negocio confirmada con el usuario antes de construir**: el
PDF del acta enviada por correo se guarda en la **misma carpeta física que
usa "Cargas"** (sobrescribiendo siempre — el acta que se manda es la
vigente, no una copia por envío), replicando el comportamiento del legado
tal cual. Efecto secundario esperado y aceptado: el acta enviada por correo
también aparece en el listado de "Cargas" de esa fila.

**Credenciales SMTP reales ahora en `appsettings.Development.json`** (no en
`appsettings.json`, mismo patrón ya usado para la cadena de conexión de
`DQUALITYREC` y el secreto de Turnstile) — Contingencia no tiene repositorio
git (confirmado, carpeta sin inicializar), así que no hay riesgo de que
queden expuestas en un remoto, pero quedan en texto plano en disco.

**Gap que hubo que resolver primero**: `IEmailSender`/`EmailSender` (creado
para notificaciones de Formatos/Sugerencias/recuperar clave) solo admitía
un destinatario y sin adjuntos. Se agregó un segundo método
`EnviarAsync(IReadOnlyList<string> destinatarios, asunto, cuerpoHtml, byte[]
adjunto, nombreAdjunto)` — el método original de un solo destinatario queda
igual (delega al nuevo internamente), así que `JuntasFirmaService.
NotificarAsync`, `FormatosService`, `LoginService`, `SugerenciasService`
(los 4 llamadores existentes) no se tocaron.

**Código nuevo**:
- `Contingencia.Infrastructure/Correo/PlantillaActaCorreo.cs` — versión
  simplificada de `CuerpoDelCorreo` (mismo criterio que
  `PlantillaActaFirmada`: mismo mensaje real, sin el boilerplate de
  compatibilidad con Outlook que no hace falta). Observación del usuario
  codificada con `WebUtility.HtmlEncode` (réplica de `Server.HtmlEncode`
  del legado — el correo es HTML y la observación es texto libre).
- `IJuntasFirmaService`/`JuntasFirmaService` — `ParsearDestinatariosCorreo`
  (réplica de `DestinatariosValidos`: admite `;`/`,`, descarta lo que no
  sea una dirección válida según `MailAddress` sin tumbar el resto) y
  `EnviarActaCorreoAsync` (réplica de la mitad "envío" de
  `btnEnviarCorreo_Click` — recibe el PDF ya generado y guardado por quien
  llama, arma el cuerpo y envía; atrapa el fallo de envío y devuelve "El
  acta quedó guardada, pero el correo no salió", igual que el legado).
- `Juntas.razor` — columna nueva "Correo" (separada, no compartiendo celda
  con "Firma" como en el `.aspx`: esta migración ya usa una columna por
  ícono de acción, igual que Pdf/Cargas/Firma) + modal "Enviar acta por
  correo". El orden de validación replica `btnEnviarCorreo_Click` exacto:
  destinatarios primero (evita pagar el costo de 2 llamadas a SISPRO si de
  entrada no hay a quién mandarle), luego asunto/observación, luego recién
  `JuntasService.GenerarActaPdfAsync` + `JuntasArchivoService.GuardarAsync`
  (mismo nombre de archivo que el legado: `Acta_<prescripción
  saneada>.pdf`, solo letras/dígitos/`-`/`_`) y por último
  `JuntasFirmaService.EnviarActaCorreoAsync`.

**Verificado en vivo (2026-10-01)**, con Chrome, usuario real `cwjuntas`
(rol 60, CIA 890330031): (1) enviar sin destinatario bloqueó con el mensaje
esperado; (2) destinatario mezclado (uno inválido + uno real) pasó la
validación correctamente, descartando solo el inválido; (3) con las
credenciales SMTP todavía vacías, ambos envíos de prueba generaron el PDF
real (llamadas reales a SISPRO), lo guardaron en
`App_Data/mediauploader/{cia}/JuntasP/{prescripcion}/Acta_{prescripcion}.pdf`
(headers `%PDF-1.7` confirmados, ~44KB) y aparecieron correctamente en el
listado del modal "Cargas" de esa misma fila, y el envío SMTP falló de forma
controlada con "El acta quedó guardada, pero el correo no salió" — exactamente
lo esperado sin credenciales configuradas.

**Envío real confirmado (2026-10-01)**, con las credenciales SMTP reales que
dio el usuario (las mismas del legado: `CorreoAlertas`/`Host`/`pwdcorreo` de
su `Web.config`, cargadas en `Smtp:User`/`Smtp:Host`/`Smtp:Password` de
`appsettings.Development.json`, Gmail con contraseña de aplicación): se
envió el acta real **#13** (prescripción `20260925164002562114`, estado
"Aprobada", con firmas reales de profesionales) a
`desarrollo@imperium.net.co` — mensaje de éxito "El acta se envió a
desarrollo@imperium.net.co." y confirmado que es el mismo PDF guardado en
disco (44.850 bytes). Archivos de prueba (ambas rondas) borrados al final —
0 restantes en `App_Data/mediauploader`.

**Nota de herramienta de pruebas (no es un bug de la app)**: en esta sesión,
el clic simulado del navegador automatizado no lograba registrar el evento
`onclick` de Blazor en `/juntas` específicamente (confirmado: ni siquiera un
`<a>` de navegación del menú lateral respondía), mientras que la misma
página sí respondía correctamente a `elemento.click()` disparado por
JavaScript — la conexión SignalR estaba viva (WebSocket conectado,
confirmado en la consola) en todo momento. Se resolvió usando `.click()` vía
`javascript_tool` para todo el flujo de prueba de esta ampliación.

### Página 9 (✅ migrada): Cargas y Firma de junta (`JuntasCargueDoc.aspx` / `JuntasFirma.aspx` → modales en `/juntas`)

A pedido explícito del usuario (2026-09-03: "trata de que queden en
modales en la misma ventana y no en modales nuevas"), los botones "Cargas"
y "Firma" de cada fila —hasta ahora enlaces al legado en pestaña nueva—
pasaron a ser **modales dentro de la misma página `/juntas`**, mismo
patrón visual ya usado por el modal "Gestionar formato" de Consultar
formato (`fmt-modal-fondo`/`fmt-modal`).

**Antes de migrar cada página se leyó el `.aspx` completo, no solo el
`.aspx.vb`** — hallazgo clave: ambas páginas tienen código muerto real
mezclado con la funcionalidad que sí se ejecuta.

#### Modal "Cargas" (`JuntasCargueDoc.aspx`)

Gestor de archivos simple: subir uno o varios documentos a una carpeta por
prescripción, listarlos, verlos, eliminarlos.

- **`sp_DQS_ActualizaJunta(@Cia, @MIPRES, @TIPOT, @CONT)`**: `DELETE` +
  `INSERT` en `DQS_MIP_JUNTASC` (columnas confirmadas por
  `sys.columns`: `CIA, MIPRES, TIPOT, CONT, FECHA`) — registra que esa
  prescripción/tipo de tecnología/consecutivo tiene documentos cargados.
  Se llama **una vez por cada archivo** subido con éxito
  (`FileUploadUsingJQuerySelectionMethod` en el legado), envuelta en un
  `Try/Catch` vacío — un fallo acá no revierte el archivo ya guardado en
  disco; se replica ese mismo criterio (`JuntasService.RegistrarCargueAsync`
  dentro de un `try { } catch { }` vacío en el `@code` de `Juntas.razor`).
- **Archivos bajo `App_Data`, no bajo `wwwroot`**: el legado guarda en
  `~/MediaUploader/{cia}/JuntasP/{prescripción}/` (carpeta pública, sin
  control de acceso — cualquiera con la URL exacta podía ver el archivo).
  Se replica la misma estructura de carpetas pero bajo
  `App_Data/mediauploader/{cia}/JuntasP/{prescripción}/` — mismo criterio
  ya usado por `FormatosArchivoService`. El endpoint nuevo
  `GET /api/juntas/cargue?prescripcion=...&archivo=...` exige sesión activa
  y **la CIA siempre sale de la sesión autenticada, nunca de un parámetro**
  — evita que un usuario vea archivos de otra compañía cambiando la URL.
- **Bucket compartido de CIA, replicado tal cual**: el legado remapea 13
  CIA distintas (`EPSS2468, ESS024, EPSS2405, EPSS2408, EPSS2413,
  EPSS2415, EPSS2420, EPSS2423, EPSS2447, EPSS2454, EPSS2470, EPSS2476,
  EPSS2494`) a una sola carpeta física `EPSS2468` — repetido 3 veces
  literalmente igual dentro de `JuntasCargueDoc.aspx.vb`, y una cuarta vez
  en `JuntaProfesional.aspx.vb` (para decidir el ícono de "ya tiene
  archivos"). Ninguna de esas 13 CIA es `890330031` (EPS Sanitas, la única
  que usa esta app), así que la condición nunca dispara hoy — se replicó
  de todas formas en `JuntasArchivoService.NormalizarCia` (un único lugar,
  no 4 copias) por si este módulo llega a servir alguna de esas compañías
  más adelante.
- **Ícono "ya tiene documentos" en la grilla principal — mejora real, no
  solo réplica**: el legado calculaba esto dentro de
  `gvconsulta_RowDataBound` de `JuntaProfesional.aspx.vb` (revisando si la
  carpeta tenía archivos) para decidir el ícono de la columna "Cargas" —
  ese cálculo estaba documentado como pendiente en la primera migración de
  esta página (cuando "Cargas" todavía era un enlace externo). Ahora que
  el almacenamiento de archivos es real, se implementó:
  `IJuntasArchivoService.TieneArchivos` cambia el ícono entre
  `fa-cloud-arrow-up` (sin archivos) y `fa-check-double` (con archivos) —
  verificado en vivo subiendo y luego eliminando un archivo de prueba, el
  ícono cambió en ambas direcciones correctamente.
- **Multiselección sin límite propio en el legado**
  (`AllowMultiple="true"`, sin tope): se fijó un límite de 20 archivos por
  carga en `InputFile` — no existía como tal en el WebForms original
  (donde no hace falta declarar un máximo), pero Blazor sí lo exige.

#### Modal "Firma" (`JuntasFirma.aspx`)

- **Código muerto real identificado, no migrado**: el `.aspx` completo
  trae un lienzo (`<canvas>`) de captura de firma a mano alzada con JS
  (`drawMouse`/`drawTouch`) y un `WebMethod UploadImage` que guarda un PNG
  en disco — **todo ese bloque está dentro de un comentario `<%-- --%>`**
  en el propio archivo (líneas 228-257 de `JuntasFirma.aspx`), inalcanzable
  desde la UI real. No se migró — no hay nada que perder, nunca se
  ejecutaba en producción.
- **Lo que sí se ejecuta**: un formulario de búsqueda de profesionales
  (`sp_DQS_ConsultaProfesional`) + dos grillas ("resultados de búsqueda"
  con acción "Asignar" y "Profesionales asignados" con acción "Eliminar",
  esta última reutilizando `sp_DQS_ConsultaProfesionalAñadido` — el mismo
  SP que ya usa la generación del acta en PDF) + un botón "Notificar"
  (visible solo si hay más de 2 asignados) que envía un correo y marca el
  acta como enviada a la EPS.
- **Bug real del legado en la búsqueda, confirmado leyendo
  `btnconsultrajuntas_ServerClick` y `OBJECT_DEFINITION` de
  `sp_DQS_ConsultaProfesional(@cia, @doc, @nom, @prof, @sede, @serv)`**: el
  formulario tiene 5 campos (Documento, Nombre, Profesión, **Sede**,
  Servicio) pero el código solo lee 4 (`txtdoc, txtnom, txtprof,
  txtserv`) — **el campo "Sede" (`txtsede`) nunca se envía al SP**, y lo
  que el usuario escribe en "Servicio" viaja como parámro `@sede`, mientras
  que `@serv` recibe otra vez el valor de "Nombre" (`txtnom` repetido). Se
  replicó el comportamiento real del SP tal cual
  (`JuntasRepository.BuscarProfesionalesAsync` manda `servicio` como
  `@sede` y `nombre` como `@serv`) y **se omitió el campo "Sede" en la UI
  migrada** — incluirlo habría sido engañoso (un filtro que visualmente
  existe pero nunca tuvo efecto real en el legado).
- **Validación de "Asignar" replicada exactamente**
  (`gvjuntas2_OnRowCommand`, `CommandName="ASG"`): acta vacía/`"&nbsp"` →
  "Aún no se ha generado acta"; columna `Firma="Si"` → "El profesional no
  ha cargado la firma" (se replica la comparación tal cual, aunque el
  nombre de la columna sugiera lo contrario — no se "corrigió" un
  significado que no está confirmado); `sp_DQS_AsignaProfesional` fallando
  (típicamente ya estaba asignado, sin un índice único documentado que lo
  confirme, pero así lo interpreta el propio legado) → "El profesional ya
  ha sido asignado a esta acta"; éxito → "Profesional añadido".
- **Destinatario de la notificación — dirección personal, replicada tal
  cual, no cambiada por esta migración**: `btnnot_ServerClick` envía
  siempre a `fmayorga1001@gmail.com` (una cuenta @gmail personal, no un
  alias de rol de EPS Sanitas). A diferencia de un pie de correo con marca
  ajena (que sí se corrigió en otras plantillas de este cliente, ver
  "Página 7"), **cambiar el destinatario real de una notificación
  operativa es una decisión de negocio que esta migración no debe tomar
  por su cuenta** — se replica literal en
  `JuntasFirmaService.DestinatarioNotificacion`, documentado aquí para que
  el usuario decida si debe actualizarse.
- **`sp_DQS_ActualizaProfesional(@CIA, @ACTA)`**: `UPDATE
  DQS_MIP_PROFESIONALESACTA SET ESTADO='ENVIADA A EPS'` — se llama
  únicamente si el correo se envió con éxito (mismo Try/Catch de
  `btnnot_ServerClick`: si el envío falla, tampoco se marca el estado).

**Verificado en vivo con Claude in Chrome (2026-09-03)**, con `cwjuntas`
sobre datos reales de SISPRO: se abrió el modal "Cargas" para una
prescripción real, se subió un archivo de prueba (confirmó "1 archivo(s)
cargado(s) correctamente", apareció en la lista, el ícono de la fila
principal cambió de "sin archivos" a "ya tiene archivos"), se verificó que
el enlace "Ver" sirve el contenido real a través de
`/api/juntas/cargue`, y se eliminó el archivo (la lista volvió a "Sin
archivos cargados" y el ícono de la fila volvió a su estado original). Se
abrió el modal "Firma" tanto para una fila sin acta generada (título
"Firma de junta — Acta" vacío, sin errores) como para una fila con acta
real (`M26-1214`, título correcto); la búsqueda de profesionales se
ejecutó sin errores de servidor ni de consola.

**Camino feliz completo de asignar/eliminar, verificado en vivo
(2026-09-03, segunda ronda)**: dado que `DQS_MIP_PROFESIONALES` estaba
completamente vacía en la base de pruebas, se insertó un profesional de
prueba real (`CIA=890330031, NoIDENTIFICACION=999999999, "PROFESIONAL DE
PRUEBA CLAUDE"`, a pedido explícito del usuario, con el compromiso de
borrarlo después) para poder ejercitar el flujo completo contra el acta
real `2980`:

- Buscar por documento `999999999` → lo encontró (confirma
  `sp_DQS_ConsultaProfesional` funcionando con datos reales).
- Asignar → "Profesional añadido" (mensaje exacto del legado), y el panel
  "Profesionales asignados" lo mostró de inmediato (confirma que
  `sp_DQS_ConsultaProfesionalAñadido` —el mismo SP que arma esa tabla en el
  PDF del acta— ve la fila recién insertada por `sp_DQS_AsignaProfesional`).
- Asignar de nuevo, mismo profesional → "El profesional ya ha sido
  asignado a esta acta" (mensaje exacto del legado, confirma la detección
  de duplicados).
- Eliminar → "Profesional eliminado", el panel volvió a "Sin resultados".
- Tras la prueba: se confirmó por SQL directo que no quedó ningún residuo
  en `DQS_MIP_PROFESIONALESACTA` (0 filas) y se borró el profesional de
  prueba de `DQS_MIP_PROFESIONALES` — la tabla quedó exactamente como
  antes de la prueba (0 filas para la CIA 890330031).

**Seguimos sin verificar por no justificarse más carga de datos
sintéticos**: el flujo de "Notificar" (solo visible con más de 2
profesionales asignados a la vez) — confirmado contra `OBJECT_DEFINITION`
de `sp_DQS_ActualizaProfesional`, no contra una ejecución real.

Archivos nuevos de esta ronda: `Contingencia.Infrastructure/Correo/
PlantillaActaFirmada.cs`, `Contingencia.Application/Juntas/
{IJuntasFirmaService,JuntasFirmaService}.cs`, `Contingencia.Web/Services/
{IJuntasArchivoService,JuntasArchivoService}.cs`; extendidos
`Contingencia.Infrastructure/Juntas/{IJuntasRepository,JuntasRepository}.cs`
(5 métodos nuevos), `Contingencia.Application/Juntas/
{IJuntasService,JuntasService}.cs` (`RegistrarCargueAsync`),
`Contingencia.Web/Program.cs` (`GET /api/juntas/cargue`),
`Contingencia.Web/Components/Pages/Juntas.razor` (los dos modales).

---

### Página 10 (✅ migrada, 2026-09-29): Firma en el acta + página nueva "Profesionales" (`MIP_PROFESIONALES.aspx` → `/profesionales`)

El legado siguió evolucionando después de migrar las páginas 8 y 9. Detectado por fecha de
modificación de archivo (`JuntaProfesional.aspx.vb`, `MIP_PROFESIONALES.aspx*`, `FirmaActa.vb`,
`BLL/LoginBLL.vb`, `DAL/LoginDAL.vb` — todos 2026-09-25 en el repo legado) y confirmado
comparando contra la base de producción real (**`172.25.2.13`/`DQUALITY`** — la del
`Web.config` que estaba activa, `45.169.253.168`, resultó ser una base vieja sin estos
cambios; ver nota de servidores más abajo). Son dos piezas nuevas, relacionadas pero
independientes, migradas juntas en esta sesión:

#### a) El acta de `/juntas` ahora imprime la firma de cada profesional

En el legado, `JuntaProfesional.aspx.vb` agrega una celda por profesional con su firma
(imagen recortada al trazo de tinta y escalada a 235×70pt) en la tabla "INTEGRANTES DE LA
JUNTA PROFESIONAL DE LA SALUD", vía una clase `FirmaActa.vb` (recorte con
`System.Drawing`/`LockBits`, render con iTextSharp). La ruta de la imagen sale de
`DQS_MIP_PROFESIONALES.Firma` —la misma columna que se había migrado como bandera "Sí/No" en
`JuntasFirmaService`— que ahora guarda una ruta relativa
(`.../Files-Gestion-Usuarios/Firmas/<CIA>/<usuario>/Firma.<ext>`).
`sp_DQS_ConsultaProfesionalAñadido` (el que alimenta `JuntasRepository.
ConsultarProfesionalesActaAsync`) ahora trae también `t2.Firma`.

**Migrado**: `MiembroJunta` (`Contingencia.Infrastructure/Juntas/DatosActaPdf.cs`) tiene un
4º campo `ImagenFirma` (`byte[]?`, ya descargado, sin recortar). `JuntasService.
GenerarActaPdfAsync` lee `Firma` de cada fila (con guarda `Columns.Contains("Firma")`, réplica
de `FirmaActa.RutaDeFila`) y descarga la imagen en paralelo por profesional vía
`IFirmaImagenService.DescargarAsync` (nuevo, `Contingencia.Infrastructure/Juntas/
FirmaImagenService.cs`). `ActaPdfBuilder.CeldaFirma`/`ImagenDeFirma`/`RecortadaAlTrazo`/
`LimitesDelTrazo` (mismo archivo `ActaPdfBuilder.cs`) portan el algoritmo de `FirmaActa.vb`
pixel por pixel (mismas constantes: ancho máximo 235pt, alto máximo 70pt, margen de recorte
6px, alfa mínimo 16, blanco mínimo 240) usando **SixLabors.ImageSharp en vez de
System.Drawing** (Windows-only, rompería la compatibilidad con Linux ya validada para esta
app) — `ProcessPixelRows` en vez de `LockBits`, pero mismo criterio de "hay tinta si no es
transparente ni casi blanco". Si no hay imagen, o si el recorte/decodificado falla (archivo
corrupto, un PDF subido como firma, etc.), la celda sale solo con el rótulo "Firma:" — igual
que el legado, una firma que falta no puede impedir que se imprima el acta. Verificado con un
harness que generó un acta de prueba con 3 profesionales (firma real capturada en vivo, sin
firma, y con bytes corruptos) — los 3 casos renderizaron sin excepciones, y la firma real salió
correctamente recortada y escalada en el PDF.

**De dónde sale la imagen — resolución en dos pasos, sin depender de dónde se creó**:
`IFirmaImagenService.DescargarAsync` primero busca la ruta relativa bajo
`App_Data/` de esta misma app (firmas nuevas, capturadas desde `/profesionales`); si no la
encuentra ahí, la pide por HTTP a `Legacy:BaseUrl` + la ruta (firmas viejas, capturadas desde
`MIP_PROFESIONALES.aspx`, que el legado sigue sirviendo como archivo estático sin
autenticación). Así el acta muestra la firma sin importar cuál de las dos apps la creó.

#### b) Página nueva "Profesionales" (`/profesionales`)

Hermana de "Juntas" en el menú (mismo módulo `"Junta Profesionales"` que habilita el rol 60 de
Sanitas — `SiteMipresN.Master.vb` línea ~331, `AppLayout.razor` mismo `@if
TieneModuloExacto("Junta Profesionales")`). CRUD completo de `DQS_MIP_PROFESIONALES`: listar
con filtros (búsqueda libre, profesión, sede, estado) y paginación cliente (10/25/50/100,
mismo patrón que Liquidación/Soportes en Recobros), crear/editar, **inactivar sin borrar**
(`sp_DQS_MIP_ProfesionalesCambiarEstado`, bit `ESTADO`), captura de firma dibujada a mano en
`<canvas>` o subida como archivo (jpg/jpeg/png/bmp/gif/pdf), y sincronización transaccional de
un usuario espejo en `DQS_REC_USUARIOS` (mismo documento = mismo usuario de aplicación; la
clave y el rol no se tocan en las actualizaciones — el procedimiento genera y muestra una
clave nueva solo si tuvo que crear el usuario).

Archivos nuevos: `Contingencia.Infrastructure/Profesionales/{ProfesionalMip,
IProfesionalesRepository,ProfesionalesRepository}.cs`, `Contingencia.Application/
Profesionales/{IProfesionalesService,ProfesionalesService}.cs`, `Contingencia.Web/Services/
{IProfesionalFirmaArchivoService,ProfesionalFirmaArchivoService}.cs`,
`Contingencia.Web/Components/Pages/Profesionales.razor(.css)`,
`Contingencia.Web/wwwroot/js/firma-canvas.js` (lienzo de firma real — a diferencia del lienzo
de `JuntasFirma.aspx` en el legado, código muerto nunca alcanzable desde la UI, ver "Página 9";
este SÍ está conectado). Registrados en `Program.cs` y cargado globalmente en `App.razor`
(mismo motivo que `dialogos.js`/`descargas.js`: un `<script>` dentro de la propia página no
se re-ejecuta con la enhanced navigation de Blazor).

**Simplificaciones deliberadas frente al legado** (no son bugs, decisiones de esta migración):
- El nombre de carpeta de la firma usa directamente el **número de documento**, no el
  "usuario" que resuelve `sp_DQS_MIP_ProfesionalesResolverUsuario` en el legado (vía AJAX antes
  de guardar). Evita necesitar soporte de parámetros de salida (`OUTPUT`) en
  `IStoredProcedureExecutor`, que hoy solo maneja `SELECT`/`ExecuteNonQuery`; el documento ya es
  único por CIA (mismo criterio que valida `sp_DQS_MIP_ProfesionalesInsertar`). Por eso
  `ResolverUsuario`/`SincronizarUsuario` no tienen método propio en
  `IProfesionalesRepository` — solo se llaman internamente desde los procedimientos de
  Insertar/Actualizar, igual que en el legado.
- Las firmas nuevas se guardan bajo `App_Data/mediauploader/Files-Gestion-Usuarios/Firmas/...`
  (minúsculas, según la convención ya usada por `FormatosArchivoService`/`JuntasArchivoService`
  — Linux distingue mayúsculas) en vez de la carpeta pública del legado
  (`~/MediaUploader/...`, servida sin autenticación). El nombre de la subcarpeta
  "Files-Gestion-Usuarios/Firmas" sí imita el del legado, solo por trazabilidad humana en la
  columna `Firma` — nunca hace falta que coincida exactamente, porque el recorrido de
  escritura/lectura de esta app nunca depende del que usa el legado.

#### Cambios de esquema — aplicados en dos servidores el 2026-09-29

Guardados en `Contingencia.Infrastructure/Sql/001_MipProfesionales_FirmaEnActa.sql`. Fuente de
verdad: `172.25.2.13/DQUALITY` (servidor real donde corre hoy el legado con esta
funcionalidad — confirmado con `OBJECT_DEFINITION` directo). **Ojo con los servidores**: el
`Web.config` legado tiene la cadena a `45.169.253.168` como la única activa (no comentada),
pero ese servidor **no** es el que corre el legado actualizado — quedó desactualizado. El
script se aplicó en:
- `DQUALITYREC` (`172.25.2.15,1435`) — base de pruebas de esta migración.
- `DQUALITY` (`45.169.253.168`) — a pedido explícito del usuario, para nivelarlo aunque no sea
  el servidor real del legado.

En ambos, `DQS_MIP_PROFESIONALES` tenía 0 filas antes de aplicar el script (confirmado antes de
tocar nada), así que los `ALTER COLUMN` a `nvarchar(1000)`/`nvarchar(2000)` y el `ADD
IDPROFESIONAL INT IDENTITY` fueron seguros, sin datos que migrar ni perder.

- `DQS_MIP_PROFESIONALES`: columnas viejas (`CIA`, `TIPODOC`, `NoIDENTIFICACION`,
  `APELLIDOS Y NOMBRES`, `PROFESION`, `SEDE`, `SERVICIO`, `CORREOELECTRONICO`, `CELULAR`,
  `FActivación`, `Firma`) pasaron de `varchar(50)`/`varchar(100)` a `nvarchar(1000)`. Columnas
  nuevas: `IDPROFESIONAL` (`int IDENTITY`, PK de facto), `ESTADO` (`bit`), `FECHACREACION`/
  `USUARIOCREACION`, `FECHAACTUALIZACION`/`USUARIOACTUALIZACION`,
  `FECHAINACTIVACION`/`USUARIOINACTIVACION` (auditoría), `APELLIDOS`/`NOMBRES`
  (`nvarchar(500)`, partidos de "APELLIDOS Y NOMBRES" para el formulario nuevo).
- 9 procedimientos nuevos, firma real confirmada contra producción:
  `sp_DQS_MIP_ProfesionalesConsultar/Obtener/Insertar/Actualizar/CambiarEstado/Resumen/Listas/
  ResolverUsuario/SincronizarUsuario` (los tres últimos son helpers internos — ni el legado ni
  esta migración los llaman directo desde la página; `Insertar`/`Actualizar` los invocan por su
  cuenta dentro de la misma transacción).
- `sp_DQS_ConsultaProfesionalAñadido` actualizado (agrega `t2.Firma` al `SELECT`).
- No hizo falta tocar `DQS_REC_USUARIOS` en ningún servidor: todas las columnas que usa
  `SincronizarUsuario` (`Cargo`, `PWDTEMP`, `FechaPwd`, `IMAGEN`, `TOKEN`, `CEDULA`...) ya
  existían.

**Verificado en vivo end-to-end (2026-09-29)**, contra `DQUALITYREC`, con el usuario real
`cwjuntas` (rol 60, CIA `890330031`) desde `/profesionales`: se creó un profesional de prueba
con firma dibujada a mano (confirmado el usuario espejo creado en la misma transacción, con el
mismo `ROL` que `cwjuntas`, y la clave generada mostrada una sola vez); se editó y se confirmó
que la vista previa de la firma cargaba correctamente (data URI); se inactivó y se confirmó que
el usuario espejo también quedó `Inactivo`, con `FECHAINACTIVACION`/`USUARIOINACTIVACION`
poblados. Se borraron la fila y el archivo físico al final, sin dejar residuos (la tabla quedó
en 0 filas en ambos servidores).

---

### Página 11 (✅ nueva, 2026-09-29): Wizard "Nueva Contingencia MIPRES" (`/formatos/contingencias/nueva` + `/formatos/contingencias/consultar`)

Sin equivalente legado: no existe ninguna pantalla WebForms que este wizard migre o
reemplace. Es una funcionalidad nueva, pedida por el usuario a partir de un prototipo
externo (`mipres-flow-buddy.lovable.app/nueva-contingencia`), para registrar una
contingencia con datos estructurados (causal, IPS, profesional, paciente, diagnóstico,
tecnología, posología, justificación, soportes y firma) en vez de solo cargar un PDF ya
diligenciado — eso es lo único que soporta "Cargar formato" (Página 4). Alcance acotado
por decisión explícita del usuario: solo el wizard de creación + una consulta de solo
lectura; quedan fuera de alcance las secciones de bandeja IPS/EPS, reportes y
configuración del prototipo.

**Wizard de 11 pasos** (`NuevaContingencia.razor`): Causal → IPS → Profesional tratante →
Paciente → Diagnóstico → Tipo de tecnología → Detalle de tecnología → Posología →
Justificación técnica → Soportes y firma → Revisión. Reutiliza tal cual: el autocompletado
de IPS (`FormatosService.BuscarPrestadoresAsync`/`DQS_MIP_COD_HAB`, Página 4), la búsqueda
de profesionales activos (`IProfesionalesService.ConsultarAsync(cia, busqueda, null, null,
"1")`, Página 10), y la captura de firma en canvas (`ImperiumFirmaCanvas.*`, mismo JS que
`Profesionales.razor`) — con un servicio de archivos nuevo (`IContingenciaArchivoService`,
no el de "Cargar formato": ese es un-archivo-por-radicado, acá se necesitan varios
soportes + una firma). El campo "profesional"/"IPS"/"diagnóstico" siguen el mismo patrón
tolerante a texto libre ya usado en "Cargar formato" (`<input list="...">` +
`<datalist>`, formato `"Código|Nombre"`, sin obligar a elegir una sugerencia).

**Firma = placeholder de FEA, no una integración real.** La firma se captura localmente
(dibujo en canvas o archivo subido) y se guarda en disco — no se envía a ningún proveedor
de firma electrónica avanzada. La columna `FIRMAPENDIENTEFEA` queda siempre en `1` y el
wizard (paso 10) y el detalle de la consulta muestran un aviso fijo: "Firma capturada
localmente — integración con proveedor de firma electrónica avanzada (FEA) pendiente."

**Catálogos**: forma farmacéutica, vía de administración y causal son **texto libre** por
decisión explícita del usuario — no existe hoy ningún catálogo consultable para esos tres
campos en ninguna de las dos soluciones (Contingencia ni Recobros). Frecuencia,
duración, indicación especial y tipo de prestación (paso 8, Posología) sí reutilizan las
listas cerradas ya verificadas en `ActaPdfBuilder.cs` (Página 8) — duplicadas (no
refactorizadas) en `CatalogosPosologia.cs`, guardando el texto en vez del código numérico.

**CIE-10 nuevo en Contingencia** (paso 5, Diagnóstico): no existía ningún catálogo de
diagnósticos en esta solución — se portó la consulta de Recobros
(`Imperium.Infrastructure/Liquidacion/LiquidacionAutocompleteRepository.BuscarCie10Async`)
a `Contingencia.Infrastructure/Cie10/Cie10Repository.cs`, mismo patrón de acceso directo
Dapper que `SesionPuenteRepository` (misma base física `DQUALITY` compartida con Recobros,
tabla `DQS_REC_CARGUECIE10`, columnas `Código`/`Descripción`/`Marca`, sin SP). Confirmado en
vivo: 12.634 filas en `DQUALITYREC` al momento de esta migración.

**Paciente = 100% manual** (paso 4): se confirmó que no existe ningún lookup de
paciente/afiliado por documento en ninguna de las dos soluciones (ni tabla local madura, ni
API externa) — `DQS_AFI_AFILIADOSAN` (Recobros) es la única tabla con nombre de "afiliado"
pero está marcada "no inspeccionada a fondo" y solo se lee internamente desde un SP de
liquidación, no es un lookup reutilizable.

#### Esquema nuevo — aplicado en dos servidores el 2026-09-29

Guardado en `Contingencia.Infrastructure/Sql/002_MipContingencias_NuevaContingencia.sql`
(siguiente número en la carpeta `Sql` propia de Contingencia, que antes solo tenía
`001_...` — numeración independiente de la de Recobros). Sin fuente de verdad legada (no
hay ningún objeto de producción que replicar): tabla y SPs 100% nuevos, diseñados para este
wizard. Aplicado en:
- `DQUALITYREC` (`172.25.2.15,1435`) — base de pruebas.
- `DQUALITY` (`45.169.253.168`) — a pedido explícito del usuario, para nivelarlo (mismo
  criterio ya usado en la Página 10).

**`dbo.DQS_MIP_CONTINGENCIAS`** (nueva, con PK real `ID INT IDENTITY` — a diferencia de
`DQS_MIP_FORMATOS`, esta tabla no tiene precedente legado que obligue a omitir una PK):
`CIA`, `RADICADO`, `ESTADO` (default `'Registrado'`, sin flujo de gestión todavía),
`USUARIO`, `FECHACREACION`, `CAUSAL`, `IPS_CODIGO`/`IPS_NOMBRE`,
`PROFESIONAL_DOCUMENTO`/`PROFESIONAL_NOMBRE`/`PROFESIONAL_PROFESION`,
`PACIENTE_TIPODOC`/`PACIENTE_NUMDOC`/`PACIENTE_NOMBRES`/`PACIENTE_APELLIDOS`/`PACIENTE_SEXO`/
`PACIENTE_FECHANACIMIENTO`/`PACIENTE_MUNICIPIO`/`PACIENTE_TELEFONO`/`PACIENTE_REGIMEN`,
`DIAGNOSTICO_CODIGO`/`DIAGNOSTICO_DESCRIPCION`, `TIPOTECNOLOGIA`,
`TEC_NOMBRE`/`TEC_CODIGO`/`TEC_FORMAFARMACEUTICA`/`TEC_VIAADMINISTRACION`/`TEC_DOSIS`,
`POS_TIPOPRESTACION`/`POS_CANTIDADFORMULADA`/`POS_CADAFRECUENCIA`/`POS_FRECUENCIA`/
`POS_DURACIONTRATAMIENTO`/`POS_DURACIONUNIDAD`/`POS_INDICACIONESPECIAL`/`POS_CANTIDADTOTAL`/
`POS_INDICADORRECURRENCIA`, `JUSTIFICACION_TECNICA`, `SOPORTES_RUTAS_JSON` (JSON de rutas
relativas), `FIRMA_RUTA` (ruta relativa), `FIRMAPENDIENTEFEA` (`bit`, default `1`). Índice
`IX_MIPCONT_CIA_RADICADO (CIA, RADICADO)`.

Radicado generado con el mismo algoritmo que `sp_MIP_InsertaFormatoCnt` (confirmado por
`OBJECT_DEFINITION`): `AAAAMMDD` + `COUNT(*)` de la tabla, relleno de ceros hasta 14
dígitos (radicado final de 22 caracteres) — espacio de radicados independiente del de
`DQS_MIP_FORMATOS`, mismo patrón de contador no atómico ya aceptado en el resto del
proyecto.

3 SPs nuevos: `sp_MIP_InsertaContingencia` (INSERT de todo excepto firma/soportes, devuelve
el radicado), `sp_MIP_ActualizaContingenciaArchivos` (UPDATE de `FIRMA_RUTA`/
`SOPORTES_RUTAS_JSON` una vez los archivos ya están en disco con el radicado conocido) y
`sp_MIP_ConsultaContingencias` (filtros opcionales estilo `NULLIF`/`IS NULL`, sin
precedente legado — no el estilo `"%"` de `sp_MIP_ConsultaFormatosCNT`, que es fiel a un
legado que no aplica acá).

**Verificado en vivo end-to-end (2026-09-29)**, contra `DQUALITYREC`, con el usuario real
`cwformatos` (CIA `890330031`) desde `/formatos/contingencias/nueva`: se completaron los 11
pasos (medicamento Acetaminofén, posología 1 cada 8 horas por 5 días, firma dibujada a
mano), se confirmó el radicado devuelto, se verificó la fila completa contra la base
(incluida `FIRMA_RUTA` apuntando al archivo físico real y `FIRMAPENDIENTEFEA=1`), se
verificó que apareciera correctamente en `/formatos/contingencias/consultar` (filtro por
documento de paciente) y que el detalle mostrara la firma servida vía
`/api/contingencias/archivo` (mismo patrón de seguridad que `/api/formatos/archivo`: exige
sesión activa y que el radicado pertenezca a la CIA del usuario, solo sirve rutas ya
registradas en `FIRMA_RUTA`/`SOPORTES_RUTAS_JSON` de esa contingencia). Se borraron la fila
y el archivo físico al final en ambos servidores, sin dejar residuos (0 filas en
`DQUALITYREC` y en `45.169.253.168/DQUALITY`).

Antes de esta migración, tampoco existía ningún CIE-10 verificado en Contingencia — se
confirmó en el mismo momento que `DQS_REC_CARGUECIE10` ya vive en la base compartida
`DQUALITYREC` con 12.634 filas, listo para consultarse sin migración adicional.

#### Ampliación (✅, 2026-09-29): catálogo paramétrico de Causal + dictado por voz

A partir de capturas del prototipo externo, se agregaron dos cosas más al wizard, en
`Contingencia.Infrastructure/Sql/003_MipContingenciasParametricas.sql` (aplicado en
`DQUALITYREC` y en `DQUALITY`/`45.169.253.168`, mismo criterio que los scripts anteriores):

**Tabla paramétrica nueva `dbo.DQS_MIP_CONT_PARAMETRICAS`** (`ID` PK, `GRUPO`, `CODIGO`,
`VALOR`, `ORDEN`, `ACTIVO`) — genérica y reutilizable para futuros listados cerrados del
wizard (un `GRUPO` nuevo por listado, sin tocar esquema otra vez), mismo espíritu que
`DQS_MIP_CRUZAR` pero como tabla propia de este wizard, no compartida con Junta
Profesional. Primer uso: `GRUPO='CAUSAL'`, sembrada con 6 valores confirmados por el
usuario contra el prototipo (`DIFICULTAD_TECNICA`, `SIN_SERVICIO_ELECTRICO`,
`SIN_CONECTIVIDAD`, `INCONSISTENCIA_AFILIACION`, `INCONSISTENCIA_IDENTIFICACION`,
`URGENCIA_MEDICA`). SP nuevo `sp_MIP_ConsultaParametricas(@Grupo = NULL)` — trae todo el
catálogo (o un grupo puntual) en una sola llamada; `ParametricasRepository`/
`ParametricasService` lo cargan completo y lo agrupan en memoria por `GRUPO`
(`ILookup<string, ParametricaValor>`), exactamente el mismo patrón que
`JuntasService.CargarCruzarAsync`/`BuscarCruzar` sobre `DQS_MIP_CRUZAR` — confirmado como
el idiomático de este proyecto para catálogos multi-grupo (no un query parametrizado por
grupo cada vez).

El paso 1 del wizard pasó de un único `<textarea>` de texto libre a un `<select>` (Causal,
alimentado por este catálogo) + un `<textarea>` nuevo con límite de 500 caracteres
(Descripción de la contingencia) — replicando la separación que trae el prototipo. Esto
implicó ensanchar `DQS_MIP_CONTINGENCIAS` de forma aditiva: `CAUSAL_NOMBRE NVARCHAR(500)` y
`CAUSAL_DESCRIPCION NVARCHAR(500)` (columnas nuevas); la columna `CAUSAL` ya existente
cambió de significado (antes texto libre, ahora el **código** del catálogo, ej.
`URGENCIA_MEDICA`) — seguro porque la tabla seguía en 0 filas reales. `sp_MIP_InsertaContingencia`
y `sp_MIP_ConsultaContingencias` se recrearon (`DROP`+`CREATE`) agregando estos campos.

**Trampa de codificación encontrada y corregida en el momento**: el primer intento de
aplicar `003_...sql` contra `DQUALITYREC` insertó los 6 valores de causal con las tildes
corrompidas (`sqlcmd` interpretó el archivo `.sql`, guardado en UTF-8 sin BOM, con la
codificación ANSI de la consola en vez de UTF-8 — mismo problema clásico de mojibake). Se
detectó comparando el valor guardado contra el literal Unicode exacto en SQL
(`WHERE VALOR = N'...'` fallaba pese a que el `SELECT` a simple vista se veía casi bien);
la corrección fue anteponerle un BOM UTF-8 al archivo `.sql` (`\xEF\xBB\xBF`) antes de
volver a aplicarlo — con eso `sqlcmd` detectó la codificación correcta y los 6 valores
quedaron verificados carácter por carácter contra el literal esperado, en ambos
servidores. **Lección para scripts SQL futuros de este proyecto que inserten literales con
tildes/ñ**: guardar el `.sql` con BOM UTF-8 desde el principio, o verificar con una
comparación exacta (`= N'...'`) después de aplicarlo — nunca confiar en que el `SELECT`
"se ve bien" en la consola, porque la consola puede tener su propio problema de
codificación *distinto* del de los datos realmente guardados.

**Dictado por voz** (pasos 1 a 9 del wizard; los pasos 10-11 no tienen campos de texto
propios y no lo muestran): `Contingencia.Web/wwwroot/js/dictado-voz.js` (nuevo, registrado
globalmente en `App.razor` junto a `firma-canvas.js`/`dialogos.js`, mismo motivo: la
navegación mejorada de Blazor no re-ejecuta `<script>` embebidos en páginas). Es un
envoltorio delgado sobre la Web Speech API del navegador (`SpeechRecognition`/
`webkitSpeechRecognition`) — **100% del lado del cliente**, sin ninguna implicación de
compatibilidad Linux del lado servidor (el servidor solo sirve el archivo JS estático,
igual en Windows que en Linux). Dos limitaciones del navegador, no del código, verificadas
en esta sesión:
- **Requiere contexto seguro (HTTPS)** — `localhost` cuenta como seguro sin HTTPS
  (confirmado en vivo: `window.isSecureContext === true` en `http://localhost:5266`), pero
  cualquier otro origen sin HTTPS hará que el botón falle con error `not-allowed` para
  todos los usuarios ahí. Si el ambiente real de despliegue (EPS Sanitas) no sirve por
  HTTPS, este es un prerequisito de infraestructura a resolver aparte, no un bug de código.
- **Solo Chrome/Edge** — Firefox no lo soporta en producción. `ImperiumDictado.soportado()`
  se verifica al cargar la página (`OnAfterRenderAsync`) y, si es `false`, el botón queda
  deshabilitado con un aviso — confirmado en vivo (`soportado()` devolvió `true` en Chrome).

Toda la lógica de negocio (qué campo llenar con qué texto) vive en C#
(`NuevaContingencia.razor`), no en JS: `DotNetObjectReference<NuevaContingencia>` +
métodos `[JSInvokable]` (`OnDictadoResultado`/`OnDictadoError`/`OnDictadoFin`) reciben la
transcripción; el botón "Cargar campos" corre un parser "mejor esfuerzo" (método
`CargarCampos`/`AplicarDictado`) que busca, para el paso actual, el sinónimo de campo más
largo mencionado en el texto (evita que "documento" se coma la mención de "número de
documento") y toma como valor lo que sigue hasta la siguiente mención reconocida; hace
fuzzy-match contra catálogos cerrados cuando el campo es un `<select>` (incluida la Causal,
contra `DQS_MIP_CONT_PARAMETRICAS`) y contra mapas fijos para Tipo de documento/Sexo. Nunca
bloquea el flujo: lo que no se reconoce queda para llenar a mano. El texto libre que sí se
asigna queda en minúsculas y sin tildes (se normaliza para comparar sin acentuación, ya que
el reconocimiento de voz no siempre transcribe tildes de forma consistente) — limitación
aceptada, el usuario puede corregirlo a mano en el mismo campo antes de enviar.

**Verificado en vivo (2026-09-29)**, con Chrome contra `http://localhost:5266`: el
desplegable de Causal mostró las 6 opciones con tildes correctas; se dictó (simulando la
transcripción, ya que no hay micrófono real disponible en el entorno de pruebas
automatizado) "causal urgencia medica descripcion no fue posible..." en el paso 1 →
"Cargar campos" seleccionó correctamente "Urgencia médica" en el desplegable y llenó la
descripción; en el paso 4 (Paciente, 8 campos) se dictó una frase con los 8 campos
mencionados (incluido "tipo de documento cedula de ciudadania" y "sexo femenino") y los 8
se reconocieron y aplicaron correctamente, incluido el mapeo a los códigos `CC`/`F`. Se
completó un segundo registro end-to-end por la UI (sin voz real, con texto tecleado
directo en los campos) confirmando `CAUSAL='URGENCIA_MEDICA'`,
`CAUSAL_NOMBRE='Urgencia médica'` (verificado con comparación exacta `= N'...'`) y
`CAUSAL_DESCRIPCION` poblados correctamente en la fila final; se borró después en ambos
servidores, sin dejar residuos. **No verificado en esta sesión** (requiere hardware real):
reconocimiento de voz con audio real de un micrófono — solo se confirmó que la API está
correctamente cableada (`soportado()`, inicio/detención sin errores, `DotNetObjectReference`
recibiendo callbacks) y que el parser de campo+valor funciona correctamente dado un texto
ya transcrito.

#### Ampliación (✅, 2026-09-30): botón "Crear" para agregar causales nuevas desde el wizard

Pedido del usuario: si el catálogo `DQS_MIP_CONT_PARAMETRICAS` (`GRUPO='CAUSAL'`) no trae la
causal que se necesita, el usuario debe poder crearla ahí mismo, sin pedirle a nadie que
corra un script SQL. Guardado en
`Contingencia.Infrastructure/Sql/004_MipParametricaInsertar.sql` (aplicado en `DQUALITYREC`
y en `DQUALITY`/`45.169.253.168`).

**SP nuevo `sp_MIP_InsertaParametrica(@Grupo, @Codigo, @Valor)`**: rechaza duplicados
(`GRUPO`+`CODIGO` ya existente → `RESULTADO=0`), calcula `ORDEN` como
`MAX(ORDEN) de ese GRUPO + 1` (así los valores nuevos quedan al final de la lista, sin
reordenar los existentes), inserta con `ACTIVO=1`. Verificado en vivo: inserción normal,
rechazo de duplicado exacto, y `ORDEN` correlativo (el 6to valor de la semilla original
quedó en `ORDEN=6`, el nuevo en `ORDEN=7`) — fila de prueba borrada después en ambos
servidores.

**Capa de código**: `IParametricasRepository.InsertarAsync(grupo, codigo, valor)` (mapea
`RESULTADO`/`MENSAJE` del `DataTable`) → `IParametricasService.CrearAsync(grupo, valor)`,
que genera el `CODIGO` en C# a partir del texto que escribe el usuario
(`ParametricasService.GenerarCodigo`: quita tildes, pasa a mayúsculas, reemplaza cualquier
secuencia no alfanumérica por `_`, recorta a 50 caracteres) — el usuario nunca escribe el
código a mano, solo el nombre. `NuevaContingencia.razor` agrega un botón "+ Crear" al lado
del `<select>` de Causal (paso 1) que abre un modal (mismo patrón `fmt-modal-fondo`/
`fmt-modal` ya usado en "Gestionar formato"/"Profesionales") con un único campo de texto;
al guardar, recarga el caché completo de `parametricas`
(`ParametricasService.CargarAsync()`) y deja la causal recién creada
auto-seleccionada en el desplegable.

**Verificado en vivo (2026-09-30)**, con Chrome contra `http://localhost:5266`: se creó
"Falla eléctrica del generador (prueba QA)" desde el modal → código generado
`FALLA_ELECTRICA_DEL_GENERADOR_PRUEBA_QA`, quedó `ORDEN=7`, apareció de inmediato
seleccionada en el desplegable sin recargar la página; se confirmó el valor exacto guardado
contra el literal Unicode (`= N'...'`, con tilde correcta — a diferencia del incidente de
codificación de `003_...sql`, este flujo pasa por ADO.NET/parámetros tipados, no por
`sqlcmd` leyendo un archivo, así que no aplica ese mismo riesgo); se repitió la creación con
el mismo nombre y el modal mostró correctamente "Ya existe una causal con ese nombre." sin
duplicar la fila. Se borró la fila de prueba al final en ambos servidores (quedaron en 6
filas del grupo `CAUSAL` en los dos).

#### Ampliación (✅, 2026-09-30): indicador "PASO N/11" y panel informativo de la IPS

Dos ajustes de UI pedidos por el usuario tras comparar contra el prototipo externo, sin
cambios de esquema:

**Indicador "PASO N/11"**: cada paso del wizard ahora muestra un encabezado propio
(`N. TítuloDelPaso` + una insignia "PASO N/11" alineada a la derecha), agregado una sola vez
en el marcado (`<div class="nc-paso-encabezado">`, justo antes del bloque de dictado),
reutilizando `pasoActual`/`TitulosPaso` que ya existían — no hizo falta estado nuevo.

**Panel informativo de la IPS (paso 2)**: al elegir una IPS del `<datalist>` (no mientras se
sigue escribiendo), aparece una tarjeta con sus datos reales. **Investigación previa
obligatoria** antes de construirlo: se confirmó contra las 199.423 filas de
`DQS_MIP_COD_HAB` en `DQUALITYREC` que esa tabla **no tiene dirección, teléfono ni correo**
en ningún registro (columnas candidatas `Extra_IX`/`Extra_X`/`Descripcion` vacías en el
100% de las filas) — el prototipo externo sí muestra esos tres campos, pero se decidió NO
inventarlos; el panel solo muestra lo que existe de verdad: NIT (`Extra_II_NroIDPrestador`),
Código de Habilitación (`Codigo`), Departamento (`Extra_VI_NombreDptoSede`), Municipio
(`Extra_V_NombreMpioSede`), Clase de prestador (`Extra_VIII`, ej. "Instituciones
Prestadoras de Servicios de Salud - IPS" vs. "Profesional Independiente" vs. "Transporte
Especial de Pacientes") y Estado (`Habilitado`, `'SI'`/`'NO'` como texto, no `bit`).

**Código nuevo**, todo en el módulo `Formatos` ya existente (no se tocó nada usado por
Cargar/Consultar formato, solo se agregaron miembros nuevos a las interfaces): record
`DetallePrestador` + `IFormatosService.ObtenerDetallePrestadorAsync(codigo)` +
`IFormatosRepository.ObtenerDetalleAsync(codigo)` (SQL directo parametrizado contra
`DQS_MIP_COD_HAB`, mismo patrón que `BuscarPrestadorAsync`, sin SP). En
`NuevaContingencia.razor`, `OnIpsInputAsync` detecta que el texto actual tiene la forma
`"Codigo|Nombre"` (así arma el valor el `<datalist>` al elegir una opción — texto libre
tecleado a mano no la tiene) para decidir si buscar el detalle en vez de seguir buscando
sugerencias.

**Verificado en vivo (2026-09-30)**, con Chrome: se buscó y seleccionó "INSTITUTO DEL
CORAZON" (código `050010001201`) desde el datalist real de `DQUALITYREC`; el panel mostró
NIT `800107179`, Cód. Habilitación `050010001201`, Departamento `ANTIOQUIA`, Municipio
`MEDELLÍN`, Clase "Instituciones Prestadoras de Servicios de Salud - IPS" y Estado
"Habilitado" — los seis campos coinciden exactamente con la fila real de la base de datos.
No se generó ninguna fila de prueba (solo navegación por el wizard, sin enviar el
formulario), así que no hizo falta limpieza.

#### Ampliación (✅, 2026-09-30): mismo panel informativo para Profesional y Diagnóstico

El usuario pidió replicar el panel de la IPS en los pasos 3 (Profesional) y 5
(Diagnóstico). Mismo criterio: **investigar primero, nunca inventar un campo que no
exista**.

**Profesional (paso 3)** — a diferencia de la IPS, acá NO hizo falta ninguna consulta
nueva: `IProfesionalesService.ConsultarAsync` (ya usado por el `<datalist>`) ya devuelve el
objeto `ProfesionalMip` completo, con todos los campos reales de `DQS_MIP_PROFESIONALES`
(confirmados leyendo `INFORMATION_SCHEMA.COLUMNS` en vivo). El prototipo externo mostraba
"Registro profesional" (tipo tarjeta/matrícula) y "Estado RETHUS" — **ninguno de los dos
existe**: `DQS_MIP_PROFESIONALES` es el catálogo propio de esta aplicación (el mismo que
usan la página "Profesionales" y Junta Profesional para asignar firmantes), no el registro
nacional RETHUS del gobierno, y no tiene ninguna columna de número de tarjeta profesional.
El panel muestra en su lugar los campos reales: Documento (`TIPODOC`+`NoIDENTIFICACION`),
Profesión (`PROFESION`), Sede (`SEDE`), Servicio (`SERVICIO`), Correo
(`CORREOELECTRONICO`), Celular (`CELULAR`) y Estado (`ESTADODESC`, Activo/Inactivo local,
no una verificación en vivo contra RETHUS). `NuevaContingencia.razor.OnProfesionalInputAsync`
detecta la selección igual que IPS (el datalist arma `"Documento|Nombre|Profesion"`) y
vuelve a llamar `ConsultarAsync` filtrando por el documento para traer el objeto completo
(el datalist en sí solo trae esos 3 campos).

**Diagnóstico (paso 5)** — tampoco hizo falta consulta nueva: `Cie10Service.BuscarAsync`
(usado por el `<datalist>`) ya devuelve `Cie10Fila(Codigo, Descripcion, Marca)` completo.
Se investigó el significado real de la columna `Marca` de `DQS_REC_CARGUECIE10` (nunca
documentado antes): solo tiene 3 valores distintos en toda la tabla (`A`/`F`/`M`),
verificados empíricamente cruzando códigos conocidos — diagnósticos de próstata siempre
`M`, diagnósticos de embarazo siempre `F` — confirmando que es un marcador de sexo al que
aplica el diagnóstico (`A`=Ambos, `F`=Solo mujeres, `M`=Solo hombres). El panel muestra
Código CIE-10, "Aplica a" (`Marca` traducida vía `InterpretarMarcaCie10`) y Descripción.

**Verificado en vivo (2026-09-30)**, con Chrome: para Profesional se insertó un profesional
de prueba real (`sp_DQS_MIP_ProfesionalesInsertar`, documento `999888777`, CIA `890330031`)
para poder seleccionarlo del datalist — el panel mostró correctamente Documento, Profesión,
Sede, Servicio, Correo, Celular y Estado "Activo"; se borró la fila y su usuario espejo en
`DQS_REC_USUARIOS` al final (0 filas restantes). Para Diagnóstico se seleccionó el código
real `D291` ("TUMOR BENIGNO DE LA PROSTATA") y el panel mostró correctamente "Aplica a:
Solo hombres" — confirma la interpretación de `Marca='M'`. No se generó ninguna fila en
`DQS_MIP_CONTINGENCIAS` (solo navegación, sin enviar el formulario).

#### Ampliación (✅, 2026-09-30): paso 4 "Paciente" rediseñado — consulta real a un web service externo de afiliados

El prototipo mostró el paso 4 con un diseño distinto al ya migrado (que era 100% manual):
un buscador por tipo+número de documento con botón "Buscar" que trae los datos del afiliado
desde un web service real, más tres campos de diligenciamiento nuevos que **no** son parte
de los datos del afiliado (Fecha de atención, Hora, Ámbito de atención MIPRES). Antes de
construir esto se relanzó un Explore agent para re-confirmar (por segunda vez en la sesión)
que ni Contingencia ni Recobros tenían ya algún cliente para este servicio — confirmado que
es información genuinamente nueva aportada por el usuario, no algo que se hubiera pasado
por alto antes.

**Servicio externo** (sin precedente en ninguno de los dos codebases): `POST
https://imperiumgitsanitas.net:51044/tutelas/tutelasmipres`, body
`{"TIPO_IDENTIFICACION":"CC","NUMERO_IDENTIFICACION":"..."}`, header `Authorization: Bearer
<JWT>`, respuesta `{"respuesta": {...datos demográficos..., "Beneficiario": [...]}, "success":
true}`. Cliente nuevo `Contingencia.Infrastructure/Afiliados/AfiliadoApiClient.cs`
(`HttpClient` tipado vía `AddHttpClient<IAfiliadoApiClient, AfiliadoApiClient>`), nunca lanza
excepciones hacia arriba (`ResultadoConsultaAfiliadoApi` no-throwing), URL base como
constante privada.

**El bearer token se guarda en base de datos, no en `appsettings.json`** — tabla nueva
`dbo.DQS_MIP_AFILIADOS_TOKEN` (ID, TOKEN NVARCHAR(MAX), FECHACREACION, USUARIO,
append-only), leída siempre en vivo antes de cada llamada (nunca cacheada en el arranque de
la app). Decisión explícita: el JWT tiene expiración (`exp`) y necesita poder rotarse sin
redeploy — mismo criterio ya usado en `DQS_MIP_TOKEN` para las credenciales de SISPRO (ver
"Página 6").

**Campos nuevos en `DQS_MIP_CONTINGENCIAS`** (aditivos, ver script de esquema abajo):
`PACIENTE_FECHAATENCION` (DATE), `PACIENTE_HORAATENCION` (NVARCHAR(10)),
`PACIENTE_AMBITOATENCION_CODIGO`/`PACIENTE_AMBITOATENCION_NOMBRE` — el catálogo de "Ámbito
de atención" no es nuevo: es `DQS_MIP_CRUZAR` con `CAMPO='CodAmbAte'` (mismo mecanismo ya
usado por el acta de Junta Profesional), confirmado con los 5 valores reales (11/12/21/22/30
= Ambulatorio Priorizado/No Priorizado, Hospitalario Domiciliario/Internación, Urgencias).

**Validación del paso 4 reescrita**: de "debe ingresar nombres/apellidos a mano" a "debe
buscar y confirmar el paciente" (`afiliadoEncontrado`), más fecha/hora/ámbito obligatorios.
El dictado por voz del paso 4 se redujo a solo Tipo/Número de documento (los demás campos ya
no se digitan directamente).

**Bug real encontrado y corregido en la misma prueba (2026-09-30)**: el paso 11 (Revisión)
mostraba el valor crudo del `<datalist>` para IPS/Profesional/Diagnóstico (ej. `A000|COLERA
DEBIDO A VIBRIO CHOLERAE...`, con el separador `|` interno visible al usuario) en vez de un
texto legible — solo un problema de visualización en el resumen, el envío real
(`ConfirmarAsync`) ya separaba código/descripción correctamente con `PartirEnDos` antes de
guardar en base de datos. Corregido agregando `TextoLegible`/`ProfesionalLegible` en
`NuevaContingencia.razor` para el paso 11 únicamente.

**Verificado en vivo (2026-09-30)**, con Chrome, documento real de ejemplo `83219399`
(régimen Subsidiado, EPS Sanitas): la búsqueda trajo correctamente "ALBEIRO MUÑOZ LOSADA",
Sexo M, Nacimiento 1981-02-04, Municipio "PITALITO, HUILA", Teléfono 3203997508. Wizard
completo de punta a punta (los 11 pasos) con IPS/Profesional/Diagnóstico elegidos por
datalist real (no texto libre) para confirmar el fix de la Revisión — el resumen mostró
"200130059101 — HOSPITAL AGUSTIN CODAZZI E.S.E.", "1022360340 — Pinzon Mayorga Andres
Felipe (Ingeniero)" y "A000 — COLERA DEBIDO A VIBRIO CHOLERAE 01, BIOTIPO CHOLERAE"
correctamente. Fila insertada en `DQUALITYREC` con las 4 columnas nuevas pobladas
(`PACIENTE_FECHAATENCION='2026-09-30'`, `PACIENTE_HORAATENCION='10:30:00'`,
`PACIENTE_AMBITOATENCION_CODIGO='30'`, `PACIENTE_AMBITOATENCION_NOMBRE=' Urgencias'`),
verificada con comparación exacta `= N'...'` (sin corrupción de acentos/ñ — los datos se
insertaron vía ADO.NET parametrizado, no vía `sqlcmd -i`, así que el bug de encoding de
scripts `.sql` no aplica aquí), y luego eliminada junto con su carpeta de firma en
`App_Data/mediauploader/contingencias/` (0 filas de prueba restantes en `DQUALITYREC`).
Checklist Linux (grep `System.Drawing`/`Microsoft.Win32`/`Registry.`/`WindowsIdentity`/
`ProtectedData`/`Server.MapPath`) limpio en `Contingencia.Infrastructure/Afiliados/`.

**Actualización (2026-09-30)**: `ConsultarNuevaContingencia.razor` (detalle de consulta) ya
muestra los 4 campos de Fecha/Hora/Ámbito de atención (bloque "Atención") y, desde la
ampliación siguiente, también los dos diagnósticos relacionados cuando existen.

#### Ampliación (✅, 2026-09-30): tarjetas de "Tipo de tecnología" + dos diagnósticos relacionados opcionales

El usuario comparó dos pasos más del wizard contra el prototipo y pidió dos ajustes
puntuales, sin relación entre sí más que caer el mismo día:

**a) Paso 5 "Diagnóstico" — faltaban los relacionados.** El prototipo tiene, además del
diagnóstico principal (obligatorio), dos campos opcionales "Diagnóstico relacionado 1" y
"Diagnóstico relacionado 2" — mismo catálogo CIE-10 (`DQS_REC_CARGUECIE10`), mismo mecanismo
de `<datalist>` con formato `"Codigo|Descripcion"` ya usado para el diagnóstico principal,
pero **sin panel informativo** (el prototipo tampoco lo muestra para estos dos, a diferencia
del principal que sí trae Código/Aplica a/Descripción). Nuevas columnas aditivas en
`DQS_MIP_CONTINGENCIAS`: `DIAGNOSTICO_REL1_CODIGO`/`DIAGNOSTICO_REL1_DESCRIPCION`,
`DIAGNOSTICO_REL2_CODIGO`/`DIAGNOSTICO_REL2_DESCRIPCION` (las 4 `NVARCHAR` nulas). Ambos
opcionales — no se agregó validación en `ValidarPasoActual` para el paso 5, que solo sigue
exigiendo el diagnóstico principal.

**b) Paso 6 "Tipo de tecnología" — reemplazado el `<select>` por tarjetas clicables**, igual
al diseño del prototipo: una cuadrícula de 5 tarjetas (Medicamento, Procedimiento, Dispositivo
médico, Producto nutricional, Servicio complementario), cada una con un título y una
descripción de una línea de qué datos pedirá el siguiente paso (ej. "DCI, concentración, vía,
dosis, frecuencia y duración" para Medicamento), y la tarjeta activa resaltada con borde azul
(mismo azul `#2b6ff2` ya usado en el resto del wizard). Cambio 100% de presentación: el valor
subyacente (`form.TipoTecnologia`) y sus 5 códigos (`Medicamento`/`Procedimiento`/
`Dispositivo`/`ProductoNutricional`/`ServicioComplementario`) no cambiaron, así que no hizo
falta tocar el paso 7 (Detalle tecnología, que decide sus campos según ese mismo valor), la
validación, el dictado por voz, ni ninguna columna de base de datos.

**Esquema — script `006_MipDiagnosticosRelacionados.sql`**, aplicado el 2026-09-30 en
`DQUALITYREC` y en `DQUALITY`/`45.169.253.168` (con las credenciales correctas de ese
servidor, `sa`/`DQS2023**` — distintas de las de `DQUALITYREC` —, confirmadas por el usuario
tras un primer intento fallido con la contraseña de pruebas). Solo agrega las 4 columnas y
recrea `sp_MIP_InsertaContingencia` (4 parámetros nuevos justo después de
`@DiagnosticoDescripcion`) y `sp_MIP_ConsultaContingencias` (4 columnas nuevas al `SELECT`);
sin cambios de tabla nueva ni de índice.

**Aprovechando esta migración, se hizo una auditoría completa de paridad de esquema entre
los dos servidores** (no solo lo de esta ampliación): confirmado que `DQS_MIP_CONT_PARAMETRICAS`,
`DQS_MIP_AFILIADOS_TOKEN`, los 4 SPs de parametricas/token y las 4 columnas de Fecha/Hora/
Ámbito de atención (ampliación anterior) también existen en `45.169.253.168`. Única diferencia
encontrada (no de esquema, de dato): el catálogo `CAUSAL` tiene 7 filas en `DQUALITYREC`
(las 6 sembradas más `POR_RESOLUCION`, creada con el botón "+ Crear" durante pruebas) contra
6 en `45.169.253.168` — no se replicó a propósito, ya que "+ Crear" es captura en vivo, no
una migración.

**Verificado en vivo (2026-09-30)**, con Chrome, wizard completo de punta a punta: diagnóstico
principal `A010` (Fiebre tifoidea/paratifoidea A) + relacionado 1 `A012` (paratifoidea B) +
relacionado 2 `A013` (paratifoidea C), tipo de tecnología "Medicamento" elegido con la tarjeta
nueva. La Revisión (paso 11) mostró los 3 diagnósticos correctamente formateados
("Código — Descripción"), y la fila insertada en `DQUALITYREC` tuvo las 4 columnas nuevas
pobladas exactamente con esos valores. Fila y archivo de firma borrados al final (0 filas de
prueba restantes).

#### Ampliación (✅, 2026-09-30): los campos de búsqueda por `<datalist>` ya no aceptan texto libre

El usuario detectó que los 5 campos de autocompletado del wizard (IPS, Profesional,
Diagnóstico principal, Diagnóstico relacionado 1 y 2) son `<input list="...">` nativos de
HTML — el navegador no impide que el usuario deje escrito cualquier texto sin elegir ninguna
sugerencia, y hasta ahora el wizard lo aceptaba igual, guardando ese texto libre como si fuera
un código real (`PartirEnDos` trata cualquier texto sin `|` como "código sin descripción").
**Sin cambios de esquema ni de tabla** — es una corrección puramente de validación en
`NuevaContingencia.razor`.

Cada uno de los 5 campos ya volvía a consultar contra la base de datos cuando detectaba el
formato `"Codigo|Descripcion"` del datalist (`OnIpsInputAsync`, `OnProfesionalInputAsync`,
`OnDiagnosticoInputAsync`, y ahora también `OnDiagnosticoRel1InputAsync`/`Rel2InputAsync`,
que antes no lo hacían por no tener panel informativo que mostrar) — guardando el resultado en
`detalleIps`/`detalleProfesional`/`detalleDiagnostico`/`detalleDiagnosticoRel1`/`Rel2`, que
queda `null` tanto si el texto no tiene el formato del datalist como si el código no existe
realmente en la tabla. `ValidarPasoActual` ahora exige que ese `detalle` correspondiente no
sea `null` para dejar avanzar el paso 2 (IPS), el paso 3 (Profesional) y el paso 5
(Diagnóstico principal, obligatorio; los dos relacionados solo si el usuario escribió algo en
ellos, ya que son opcionales) — mensaje uniforme por campo: "Debe elegir [el campo] de la
lista de sugerencias (no se admite texto libre)".

**Verificado en vivo (2026-09-30)**: escribir texto arbitrario en IPS, Profesional o
Diagnóstico relacionado 1 y presionar "Siguiente" bloquea el avance con el mensaje esperado;
elegir una opción real de la lista (o dejar vacío un relacionado) permite avanzar con
normalidad.

#### Ampliación (✅, 2026-09-30): búsqueda inteligente real en el paso 7 (Detalle tecnología) para Procedimiento/Dispositivo/Producto Nutricional/Servicio complementario

El usuario reportó que el paso 7 dejaba avanzar sin exigir ninguna selección real para los
tipos de tecnología Procedimiento, Dispositivo médico, Producto Nutricional y Servicio
complementario — eran dos `<input>` de texto libre (Código + Descripción) sin ningún
respaldo de catálogo. Pidió traer una búsqueda inteligente contra la tabla real
`DQS_MIP_CODIGOS`, filtrando por `TipoServicio` según el tipo de tecnología elegido en el
paso 6. **Medicamento queda fuera de este cambio** (el usuario indicó que lo explicaría
aparte) — sigue con sus 4 campos de texto libre (Principio activo, Forma farmacéutica, Vía
de administración, Dosis) sin tocar.

**Tabla real confirmada** (`DQS_MIP_CODIGOS`, 206.557 filas en `DQUALITYREC`, sin SP propio
— se buscó en todos los `sys.sql_modules` que la referencian y son informes pesados de
auditoría, no un lookup): columnas `CIA`, `Tipo` (`Medicamento`/`Procedimiento`/
`Dispositivos medicos`/`Producto Nutricional`/`Servicios`), `TipoServicio` (código corto
real: `M`/`P`/`D`/`N`/`S`), `Codigo`, `Descripción`, `CodigoLargo`, `Grupo`, `Fomra` (sic,
error de tipeo del legado, no corregido — se preserva tal cual está en la base),
`Presentacion`, `Unidades`. Hallazgo importante compartido con el usuario antes de construir:
**las 206.557 filas pertenecen todas a `CIA='NEPS'`, ninguna a `890330031`** (EPS Sanitas, la
CIA de pruebas de esta sesión). A pedido explícito del usuario ("filtra por NEPS x defecto"),
la búsqueda filtra siempre por `CIA='NEPS'` fija (constante en `CodigosRepository`, no la CIA
de la sesión) — decisión pragmática mientras no se cargue el catálogo propio de cada
compañía; si en el futuro se carga contenido para otras CIAs, este filtro fijo tendrá que
revisarse.

**Código nuevo** (mismo patrón que `Cie10Repository`/`Cie10Service`, SQL directo sin SP):
`Contingencia.Infrastructure/Codigos/ICodigosRepository.cs` (+ `CodigoFila(Codigo,
Descripcion)`) y `CodigosRepository.cs` (`BuscarAsync(tipoServicio, prefijo, cantidad)`,
`WHERE CIA='NEPS' AND TipoServicio=@TipoServicio AND (Codigo LIKE @Prefijo+'%' OR
[Descripción] LIKE @Prefijo+'%')`); `Contingencia.Application/Codigos/ICodigosService.cs`/
`CodigosService.cs` (guarda vacío si el prefijo o el tipo de servicio vienen vacíos, trae
hasta 100 filas). Registrado en `Program.cs`.

**`NuevaContingencia.razor` — paso 7 reestructurado**: el branch de Medicamento quedó solo
para `"Medicamento"` (antes también incluía `"ProductoNutricional"`, que compartía Vía de
administración/Dosis con Medicamento — ya no, pasa al branch de búsqueda). Los 4 tipos
restantes ahora comparten un único campo `<input list="dl-codigos-tec">` (formato
`"Codigo|Descripcion"`, igual que IPS/Profesional/Diagnóstico) + panel informativo
(Código/Descripción) cuando hay selección verificada. `TipoServicioParaBusqueda()` mapea
`form.TipoTecnologia` → el código corto real (`P`/`D`/`N`/`S`). Mismo criterio de
anti-texto-libre de la ampliación anterior: `detalleCodigoTecnologia` queda `null` si el
texto no viene de una sugerencia real, y `ValidarPasoActual` exige que no sea `null` para
avanzar el paso 7 (los 4 tipos de búsqueda) — Medicamento sigue sin validación en este paso
(fuera de alcance de este cambio).

**Bug real encontrado y corregido en la misma sesión**: si el usuario volvía al paso 6 y
cambiaba de tipo de tecnología (ej. de Procedimiento a Producto Nutricional), el código ya
elegido para el tipo anterior quedaba pegado y seguía pasando la validación del paso 7 aunque
perteneciera al `TipoServicio` equivocado — nunca se limpiaba `TecBusquedaTexto`/
`detalleCodigoTecnologia` al cambiar de tarjeta. Corregido con
`SeleccionarTipoTecnologia(valor)`, que resetea todos los campos `Tec*` del paso 7 (incluidos
los de Medicamento) cada vez que el tipo de tecnología realmente cambia.

**Verificado en vivo (2026-09-30)**, con Chrome: texto libre en el paso 7 con Procedimiento
seleccionado bloqueó el avance con el mensaje esperado; búsqueda real "puncion" trajo
`33100|PUNCION LUMBAR SOD` y `701203|PUNCION O INSICION EN VAGINA...` (2 resultados reales de
`DQS_MIP_CODIGOS` filtrados por `TipoServicio='P'`), con panel informativo correcto tras
elegir uno. Se repitió con Producto Nutricional ("ensure" → 22 resultados reales,
`TipoServicio='N'`) confirmando también el fix del reseteo: tras elegir un código en
Procedimiento y volver a cambiar a Producto Nutricional, el campo de búsqueda del paso 7
apareció vacío, sin el código del tipo anterior. No se generó ninguna fila en
`DQS_MIP_CONTINGENCIAS` (solo navegación, sin confirmar el registro).

#### Ampliación (✅, 2026-10-01): paso 8 "Posología" simplificado para todo lo que no sea Medicamento

A partir de capturas del prototipo externo, el usuario pidió rediseñar el paso 8 para
Procedimiento/Dispositivo médico/Producto Nutricional/Servicio complementario (Medicamento
queda explícitamente fuera — su paso 7 sigue pendiente de definición). El formulario de 9
campos original (pensado solo para medicamentos: tipo de prestación, cantidad formulada,
cada/frecuencia, duración+unidad, indicación especial, cantidad total, recurrencia) se
reemplaza, para estos 4 tipos, por uno de 5 campos: **Cantidad total** (obligatorio),
**Duración (número)**, **Unidad de tiempo (MIPRES)** (`CatalogosPosologia.DuracionUnidad`,
sin cambios), **Indicación especial (MIPRES)** (`CatalogosPosologia.IndicacionEspecial`, sin
cambios — los 10 valores ya coincidían exactamente con el catálogo del prototipo, no se
sembró nada nuevo) e **Indicaciones adicionales** (texto libre, nuevo).

**Columna nueva**: `POS_INDICACIONESADICIONALES NVARCHAR(500) NULL` en
`DQS_MIP_CONTINGENCIAS` (`Sql/007_MipPosologiaIndicacionesAdicionales.sql`, aplicado con BOM
en `DQUALITYREC` y en `DQUALITY`/45.169.253.168, columna verificada en ambos). Recrea
(`DROP`+`CREATE`) `sp_MIP_InsertaContingencia` (parámetro nuevo justo después de
`@PosIndicadorRecurrencia`) y `sp_MIP_ConsultaContingencias` (columna nueva al `SELECT` en la
misma posición) — seguro, sigue sin haber filas reales de producción.

**`NuevaContingencia.razor`**: el paso 8 ahora bifurca por `form.TipoTecnologia` igual que ya
lo hacía el paso 7 — `"Medicamento"` conserva el formulario original sin tocar; los otros 4
tipos renderizan el formulario simplificado. `ValidarPasoActual` exige `PosCantidadTotal` no
vacío solo para los tipos no-Medicamento. `SeleccionarTipoTecnologia` (el mismo método que ya
resetea los campos del paso 7 al cambiar de tipo) se extendió para resetear también todos los
campos `Pos*` — aplicado proactivamente, antes de observar ningún bug concreto en este paso,
por la lección ya aprendida con el paso 7. La Revisión (paso 11) y el detalle
(`ConsultarNuevaContingencia.razor`) muestran la misma bifurcación: para Medicamento, la
línea de posología original; para los demás, "Cantidad total X, duración Y Z — indicación" +
una línea aparte de "Indicaciones adicionales" solo si no viene vacía.

**Verificado en vivo (2026-10-01)**, flujo completo de los 11 pasos con Chrome: Causal
(desplegable + descripción), IPS real (`195480928201` — NATURAL BIOLOGY IPS SAS, de
`DQS_MIP_COD_HAB`), Profesional real (`1022360340` — Pinzon Mayorga Andres Felipe, activo),
Paciente real (documento `83219399` — ALBEIRO MUÑOZ LOSADA, resuelto contra el web service de
afiliados), Diagnóstico principal + 2 relacionados (CIE-10 reales `A066`/`B001`/`C098`), Tipo
de tecnología "Servicio complementario", Detalle con búsqueda real contra `DQS_MIP_CODIGOS`
(`159|ACCESORIOS CON FILTRO SOLAR`, `TipoServicio='S'`, `CIA='NEPS'`). En el paso 8: confirmado
que "Siguiente" bloquea con "Debe ingresar la cantidad total." si se deja vacío; con
Cantidad total=60, Duración=30, Día(s), Indicación especial="Infusión continua" e
Indicaciones adicionales con texto, avanzó correctamente. La Revisión mostró la línea
simplificada esperada más la de "Indicaciones adicionales". Tras confirmar, se generó el
radicado `2026100100000000000000` con fila real en `DQS_MIP_CONTINGENCIAS` (ID 6 en
`DQUALITYREC`) con `POS_CANTIDADTOTAL=60`, `POS_DURACIONTRATAMIENTO=30`,
`POS_DURACIONUNIDAD='Día(s)'`, `POS_INDICACIONESPECIAL='Infusión continua'` y
`POS_INDICACIONESADICIONALES` con el texto capturado — todo correcto. Fila y archivo de firma
(`App_Data/mediauploader/contingencias/890330031/2026100100000000000000/firma.png`)
eliminados tras la verificación; no se insertó nada en `DQUALITY`/168 (la cadena de conexión
en tiempo de ejecución solo apunta a `DQUALITYREC`, igual que en sesiones anteriores).

**Pendiente**: Medicamento (pasos 7 y 8) sigue sin definir — el usuario indicó que
especificaría su comportamiento aparte.

#### Ampliación (✅, 2026-10-01): paso 7 "Detalle tecnología" rediseñado para Medicamento (paso 8 sigue pendiente)

El usuario definió Medicamento con una captura del prototipo: Código*, Descripción*,
Principio activo / DCI*, Concentración*, Forma farmacéutica (MIPRES)* y Vía de administración
(MIPRES)* — reemplaza el formulario anterior de 4 campos libres (Principio activo/nombre,
Forma farmacéutica, Vía de administración, Dosis, sin validación alguna).

**Forma farmacéutica y Vía de administración**: el usuario indicó que están en
`DQS_MIP_CRUZAR` — confirmado (`CAMPO='CodFF'`, 39 valores reales, ej. `C42887|AEROSOL`;
`CAMPO='CodVA'`, 123 valores reales, ej. `001|AURICULAR (OTICA)`). La página YA consultaba
esa misma tabla (`FiltrosRepository.ConsultarAsync(cia, 138)`, `sp_DQS_ConsultaFiltros`) para
cargar `ambitosAtencion` (`CAMPO='CodAmbAte'`) en `OnInitializedAsync` — se extendió ese mismo
método para también filtrar `CodFF`/`CodVA` del mismo `DataTable` ya traído (sin ninguna
consulta adicional a la base) y pasaron de ser `<input>` de texto libre a `<select>` cerrados.
Se guarda la **descripción** elegida en las columnas existentes `TEC_FORMAFARMACEUTICA`/
`TEC_VIAADMINISTRACION` (sin cambio de esquema — esas columnas nunca tuvieron código propio).

**Principio activo / DCI**: el usuario indicó explícitamente que estas denominaciones NO
llevan código (son DCI — Denominación Común Internacional) y que se cargan desde la tabla
`DQS_MIP_DCI` (6.607 filas confirmadas en `DQUALITYREC`: 5.467 con `[Habilitado MIPRES]='1'`),
columna real `[Descripcion DCI Concentracion]` (máximo 965 caracteres reales; nombre entre
corchetes por el espacio). La mayoría de los valores reales empieza con `"["` (ej.
`"[ACETAMINOFEN] 100mg/1ml"`), así que a diferencia de los demás catálogos del wizard (que
buscan por prefijo) esta búsqueda es por **contención** (`LIKE '%texto%'`) — un prefijo
literal habría dejado casi todo fuera de la búsqueda. Sin código, el criterio anti-texto-libre
no puede verificarse por `'|'` como los demás campos: se verifica con una consulta de
existencia exacta (`DciRepository.ExisteAsync`) contra el texto tal cual quedó en el campo.
Columna nueva **`TEC_PRINCIPIOACTIVO NVARCHAR(1000) NULL`** en `DQS_MIP_CONTINGENCIAS`
(`Sql/008_MipMedicamentoDetalle.sql`, aplicado con BOM en `DQUALITYREC` y en
`DQUALITY`/45.169.253.168, columna verificada en ambos — `DQS_MIP_CRUZAR` y `DQS_MIP_DCI`
también confirmadas presentes y con el mismo conteo de filas en ambos servidores). Recrea
(`DROP`+`CREATE`) `sp_MIP_InsertaContingencia` (parámetro nuevo justo después de `@TecCodigo`)
y `sp_MIP_ConsultaContingencias` (columna nueva al `SELECT` en la misma posición).

**Código nuevo** (mismo patrón que `Cie10Repository`/`CodigosRepository`, SQL directo sin SP):
`Contingencia.Infrastructure/Dci/IDciRepository.cs`/`DciRepository.cs`
(`BuscarAsync(texto, cantidad)` por contención, `ExisteAsync(denominacion)`);
`Contingencia.Application/Dci/IDciService.cs`/`DciService.cs` (guarda vacío si el texto viene
vacío). Registrados en `Program.cs`. No se creó un servicio nuevo para Forma
farmacéutica/Vía de administración — se extendió el código ya existente en
`NuevaContingencia.razor.OnInitializedAsync` (ver arriba), evitando una segunda consulta al
mismo `sp_DQS_ConsultaFiltros`.

**`NuevaContingencia.razor` — paso 7 Medicamento reestructurado**: 6 campos (`TecCodigo`,
`TecNombre` ahora como "Descripción", `TecPrincipioActivo` nuevo, `TecDosis` ahora como
"Concentración", `TecFormaFarmaceutica`/`TecViaAdministracion` ahora `<select>`).
`OnPrincipioActivoInputAsync` actualiza `opcionesDci` (datalist) y `principioActivoValido` en
cada tecleo. `ValidarPasoActual` (paso 7) ahora exige, para Medicamento: los 5 campos simples
no vacíos, y además `principioActivoValido` (no basta con que el campo no esté vacío — debe
existir tal cual en `DQS_MIP_DCI`). `SeleccionarTipoTecnologia` se extendió una vez más para
resetear `TecPrincipioActivo`/`opcionesDci`/`principioActivoValido` al cambiar de tipo, mismo
criterio que las ampliaciones anteriores. La Revisión (paso 11) y el detalle
(`ConsultarNuevaContingencia.razor`) muestran el DCI entre paréntesis después de la
descripción, solo si no viene vacío.

**Verificado en vivo (2026-10-01)**, flujo completo de los 11 pasos con Chrome, tipo de
tecnología "Medicamento": el formulario del paso 7 renderizó los 6 campos nuevos, con los 39
valores reales de Forma farmacéutica y los 123 de Vía de administración en los desplegables.
Se confirmó que texto inventado en Principio activo ("texto inventado que no existe") bloqueó
el avance con "Debe elegir el principio activo (DCI) de la lista de sugerencias (no se admite
texto libre)."; con "acetaminofe" la búsqueda por contención trajo resultados reales de
`DQS_MIP_DCI` (`[ACETAMINOFEN] 100mg/1ml` entre otros) a pesar de no empezar por ese texto.
Con los 6 campos completos (`ACETA500` / `Acetaminofén tableta 500mg` /
`[ACETAMINOFEN] 100mg/1ml` / `500 mg` / `TABLETAS DE LIBERACION NO MODIFICADA` / `ORAL`) avanzó
correctamente y la Revisión mostró la línea esperada. Tras confirmar, se generó el radicado
`2026100100000000000000` con fila real en `DQS_MIP_CONTINGENCIAS` (ID 7 en `DQUALITYREC`) con
las 6 columnas `TEC_*` pobladas correctamente, incluida `TEC_PRINCIPIOACTIVO`. Fila y archivo
de firma de prueba eliminados tras la verificación.

**Pendiente**: el paso 8 "Posología" para Medicamento sigue con su formulario original de 9
campos (tipo de prestación, cantidad formulada, cada/frecuencia, duración+unidad, indicación
especial, cantidad total, recurrencia), sin ningún cambio ni validación — el usuario no ha
definido ese paso todavía.

#### Ampliación (✅, 2026-10-01): paso 8 "Posología" rediseñado para Medicamento

El usuario definió Medicamento con una captura del prototipo: **Dosis\*** (Cantidad + Unidad
MIPRES), **Frecuencia\* (MIPRES)** (Cada + Unidad de tiempo MIPRES), **Cantidad total\***,
**Duración (número)**, **Unidad de tiempo (MIPRES)**, **Indicación especial (MIPRES)** e
**Indicaciones adicionales** — reemplaza el formulario de 9 campos anterior (que incluía
"Tipo de prestación" y "¿Es recurrente?", ya no presentes en el prototipo; esas dos columnas
de la tabla se dejan sin tocar, simplemente dejan de recibir datos nuevos, ya eran
NULL-friendly).

**Unidad (de Dosis) y Unidad de tiempo/frecuencia** vienen de `DQS_MIP_CRUZAR`, a pedido
explícito del usuario:
- `CAMPO='DosisUM'` — 268 filas reales confirmadas en ambos servidores (ej. `0168|mg`,
  `0176|ml`, `0072|UI`, `anti-Xa IU/ml`, `EID50`).
- `CAMPO='CODFREADMON'` — originalmente solo 7 filas reales (1 Minuto(s)..7 Según respuesta
  al tratamiento); le faltaba el valor 8 "Única" que SÍ está en el catálogo MIPRES ya
  verificado en `Contingencia.Infrastructure/Juntas/ActaPdfBuilder.cs` (legado;
  `CatalogosPosologia.Frecuencia` ya traía ese mismo valor hardcodeado) y en el prototipo —
  el usuario aprobó explícitamente insertar la fila faltante (`CODIGO='8'`,
  `DESCRIPCION='Única'`) en `DQS_MIP_CRUZAR` en ambos servidores.

**Datos sucios preexistentes corregidos en `DQS_MIP_CRUZAR` (CAMPO='CODFREADMON'), a pedido
explícito del usuario, en ambos servidores**: `CODIGO='2'` guardaba `'Hora(s'` (sin el
paréntesis de cierre) → corregido a `'Hora(s)'`; `CODIGO='7'` guardaba
`'Según respuesta al\r\ntratamiento'` (salto de línea en medio del texto) → colapsado a
`'Según respuesta al tratamiento'`; `CODIGO` 1/3/4/5 tenían un `\r` sobrante al final →
recortado con `RTRIM`/`REPLACE`. Estos defectos no tienen relación con esta ampliación — ya
estaban en la tabla compartida desde antes, simplemente nadie los había leído hasta ahora.

**Columna nueva `POS_CANTIDADFORMULADAUNIDAD NVARCHAR(50) NULL`** en
`DQS_MIP_CONTINGENCIAS` (`Sql/009_MipMedicamentoPosologia.sql`, aplicado con BOM en
`DQUALITYREC` y en `DQUALITY`/45.169.253.168; columna, conteo de `DosisUM` y de
`CODFREADMON` verificados idénticos en ambos). Recrea (`DROP`+`CREATE`)
`sp_MIP_InsertaContingencia` (parámetro nuevo justo después de `@PosCantidadFormulada`) y
`sp_MIP_ConsultaContingencias` (columna nueva al `SELECT` en la misma posición).

**`NuevaContingencia.razor`**: `OnInitializedAsync` se extendió una vez más para filtrar,
del mismo `DataTable` de `DQS_MIP_CRUZAR` ya traído (`CAMPO='CodAmbAte'`/`'CodFF'`/`'CodVA'`),
también `'DosisUM'` (→ `unidadesDosis`) y `'CODFREADMON'` (→ `unidadesFrecuencia`, ordenado
numéricamente por código, no alfabéticamente) — sin ninguna consulta adicional a la base. El
paso 8 Medicamento ahora usa `<select>` cerrados para Unidad de dosis, Unidad de frecuencia y
Unidad de tiempo (las tres leen `unidadesFrecuencia`/`unidadesDosis`, guardando la
**descripción** elegida, igual que Forma farmacéutica/Vía de administración del paso 7 — sin
código propio en las columnas existentes). `ValidarPasoActual` (paso 8) ahora exige, para
Medicamento: Dosis, Frecuencia (cada) y Cantidad total no vacíos (Duración/Unidad/Indicación
siguen opcionales, mismo criterio que el formulario simplificado de los otros 4 tipos).
`SeleccionarTipoTecnologia` se extendió para resetear también
`PosCantidadFormuladaUnidad` al cambiar de tipo. La Revisión (paso 11) y el detalle
(`ConsultarNuevaContingencia.razor`) muestran la nueva línea de posología con Dosis+Unidad y
Cantidad total, más "Indicaciones adicionales" aparte si no viene vacía (antes esa línea no
se mostraba para Medicamento, solo para los otros 4 tipos).

**Verificado en vivo (2026-10-01)**, flujo completo de los 11 pasos con Chrome, tipo de
tecnología "Medicamento": el paso 8 renderizó los 268 valores reales de `DosisUM` en el
desplegable de Unidad y los 8 valores limpios de `CODFREADMON` (ya sin el dato sucio) en
ambos desplegables de Unidad de tiempo/frecuencia. Con Dosis=500/mg, Frecuencia=8/Hora(s),
Cantidad total=60, Duración=20/Día(s), Indicación especial="Infusión continua" e
Indicaciones adicionales con texto, avanzó correctamente; la Revisión mostró
"Posología: Dosis 500 mg, cada 8 Hora(s), cantidad total 60, por 20 Día(s) — Infusión
continua" y la línea de Indicaciones adicionales aparte. Tras confirmar, se generó el
radicado `2026100100000000000000` con fila real en `DQS_MIP_CONTINGENCIAS` (ID 8 en
`DQUALITYREC`) con las 10 columnas `POS_*` pobladas correctamente, incluida
`POS_CANTIDADFORMULADAUNIDAD='mg'`. Fila y archivo de firma de prueba eliminados tras la
verificación.

Con esto, Medicamento queda completamente definido en los pasos 7 y 8 — no quedan pendientes
de este módulo.

#### Ajuste (2026-10-02): Medicamento ya no pide Código ni Descripción en el paso 7

Tras ver el formulario en ejecución, el usuario indicó que para Medicamento no hacen falta
"Código" ni "Descripción" — solo el DCI. El paso 7 de Medicamento queda con 4 campos:
**Principio activo / DCI\*** (búsqueda real contra `DQS_MIP_DCI`), **Concentración\***,
**Forma farmacéutica (MIPRES)\*** y **Vía de administración (MIPRES)\***. Sin cambios de
esquema: `TEC_CODIGO`/`TEC_NOMBRE` simplemente quedan `NULL` para Medicamento (siguen siendo
usadas por los otros 4 tipos de tecnología). `ValidarPasoActual` (paso 7) dejó de exigir
código/descripción; el dictado por voz ya no ofrece esos dos campos; la Revisión y
`ConsultarNuevaContingencia.razor` muestran para Medicamento `DCI: … — concentración,
forma, vía` (y `código — descripción` para los demás tipos, sin los sufijos de
forma/vía/dosis que solo aplicaban a Medicamento). No se tocó ningún script SQL ni ningún SP
en este ajuste, por lo que no hay nada nuevo que nivelar entre `DQUALITYREC` y el 168.

#### Ampliación (✅ código y base, 2026-10-02): varias tecnologías por contingencia

El usuario pidió poder registrar más de una tecnología en la misma contingencia (ej. un
medicamento y un servicio). Aclaró que el paso 9 (justificación técnica) va **por tecnología**,
igual que la posología: los pasos 6 (tipo), 7 (detalle), 8 (posología) y 9 (justificación) se
repiten por cada tecnología; causal, IPS, profesional, paciente, diagnósticos, soportes y firma
se capturan una sola vez.

**Base de datos** (`Sql/010_MipContingenciasMultiplesTecnologias.sql`, aplicado con BOM en
`DQUALITYREC` y en `DQUALITY`/45.169.253.168, verificado en ambos):
- Tabla hija nueva **`DQS_MIP_CONTINGENCIAS_TEC`** (`ID` identity PK, `CIA`, `RADICADO`,
  `ORDEN` 1..n, `TIPOTECNOLOGIA`, las columnas `TEC_*`, `POS_*` y `JUSTIFICACION_TECNICA` NOT
  NULL; índice `IX_MIPCONTTEC_CIA_RADICADO (CIA, RADICADO, ORDEN)`).
- Las columnas de tecnología/posología/justificación **salieron** de `DQS_MIP_CONTINGENCIAS`
  (ya no hay dos fuentes de verdad). El script primero copia lo existente a la tabla hija
  (ORDEN=1) y SOLO borra las columnas si no quedó ninguna contingencia sin copiar. En
  `DQUALITYREC` había 1 fila real de prueba del usuario (radicado `2026100200000000000000`,
  Medicamento) — quedó migrada (padre 1 / hija 1); en el 168 había 0 filas.
- `sp_MIP_InsertaContingencia` recreado: ya no recibe `@TipoTecnologia`/`@Tec*`/`@Pos*`/
  `@JustificacionTecnica`, recibe **`@TecnologiasJson`** (arreglo JSON, una entrada por
  tecnología) e inserta cabecera + todas las tecnologías en **una sola transacción**
  (`OPENJSON`, `XACT_ABORT ON`; `ORDEN` = posición en el arreglo + 1). Compatibilidad de
  ambas bases: nivel 140 (OPENJSON requiere ≥130).
- `sp_MIP_ConsultaContingencias` recreado solo con la cabecera; el filtro `@TipoTecnologia`
  ahora busca con `EXISTS` en la tabla hija.
- `sp_MIP_ConsultaContingenciaTecnologias` (nuevo): `@CIA`, `@RadicadosJson` → tecnologías de
  esos radicados ordenadas por radicado y `ORDEN`.

**Código**: `TecnologiaContingencia` (record, un elemento por tecnología) y
`ResumenTecnologia` (textos de Detalle/Posología compartidos por la Revisión y la Consulta);
`SolicitudNuevaContingencia`/`ContingenciaConsultada` ahora llevan `Tecnologias`
(`ContingenciaConsultada.TiposTecnologia` concatena los tipos para la grilla/Excel).
`ContingenciasService.RegistrarAsync` exige al menos una tecnología y que cada una tenga tipo y
justificación, y serializa la lista a JSON; `ConsultarAsync` trae la cabecera y luego las
tecnologías de los radicados devueltos y las agrupa. En `NuevaContingencia.razor`: la
tecnología en captura vive en `form` y las ya cerradas en `tecnologiasAgregadas`; botón
**"+ Agregar otra tecnología"** en el paso 9 y en la Revisión (valida los pasos 6 a 9 de la
actual, la guarda y vuelve al paso 6 en blanco); el paso 6 lista las ya agregadas con
"Quitar"; la Revisión muestra una sección por tecnología. `ConsultarNuevaContingencia.razor`
muestra todas las tecnologías en el detalle (con su justificación).

**Verificado (2026-10-02)** con un arnés de consola contra `DQUALITYREC` (mismas clases reales
del servicio y los repositorios, sin pasar por el login): registrar una contingencia con
Medicamento + Servicio complementario → 2 filas en `DQS_MIP_CONTINGENCIAS_TEC` en el orden
correcto; `ConsultarAsync` devuelve las 2 tecnologías agrupadas y `TiposTecnologia` =
"Medicamento, ServicioComplementario"; el filtro por tipo encuentra la contingencia con
Medicamento y con Servicio complementario y no con Procedimiento; registrar sin tecnologías
responde "Debe registrar al menos una tecnología." La fila de prueba del arnés se borró. El
usuario además ya registró desde el wizard la contingencia `2026100200000000000001` con 2
tecnologías (queda en la base, es suya).

#### Ampliación (✅ código, 2026-10-02): formulario oficial MIPRES NoPBS en PDF al final del wizard

El usuario pidió generar al final el formulario oficial "MIPRES NoPBS - FORMULARIO PARA
CONTINGENCIA" (PDF de 3 páginas, adjunto `formulario-contingencia-mipres.pdf`) y que lo que el
wizard no captura se deje **vacío**. Sin cambios de base de datos: se genera al vuelo (no se
guarda en disco) a partir de lo ya registrado.

**Cómo se genera**: el PDF original no tiene campos AcroForm (0 widgets), así que se usa como
plantilla y se superponen los datos por coordenadas con iText (el mismo paquete del acta de
Junta Profesional). Las coordenadas (casillas, celdas de dígitos, cajas de texto) se midieron
sobre la plantilla y se verificaron renderizando el resultado y comparándolo con el original.
Se genera **una copia de las páginas 1 y 2 por cada tecnología** (secciones I a IV iguales,
más la sección de su tipo: VI Medicamento, VII+VIII Procedimiento, X Dispositivo, XII Producto
nutricional, XIV Servicio complementario) y **una sola vez al final la página 3**
(instrucciones). Código: `Infrastructure/Contingencias/FormularioPdf/` (`DatosFormularioContingencia`,
`FormularioContingenciaPdfBuilder`, plantilla incrustada como `EmbeddedResource` con
`LogicalName="formulario-contingencia-mipres.pdf"`) y
`Application/Contingencias/ContingenciaFormularioService` (arma los datos: busca el NIT de la
IPS en `DQS_MIP_COD_HAB`, y tipo de documento + nombres/apellidos del profesional en su ficha;
parte nombres/apellidos del paciente por la primera palabra; separa la concentración "500 mg"
en número y unidad). Endpoint `GET /api/contingencias/formulario?radicado=` (exige sesión; la
CIA sale siempre de la sesión) y dos accesos: el diálogo final del wizard ofrece "Ver
formulario PDF" (se abre en pestaña nueva y luego lleva a la consulta, `TranFormulario` en
`dialogos.js`), y el detalle de `ConsultarNuevaContingencia.razor` tiene el enlace "Ver
formulario MIPRES NoPBS (PDF)".

**Qué se llena**: (1) casilla del motivo — solo 3 causales tienen casilla (Dificultad técnica,
Sin conectividad, Sin servicio eléctrico; las demás no marcan nada y la causal va en la
aclaración) y aclaración; (2) fecha/hora de registro; (3–5) razón social, NIT y código de
habilitación de la IPS; (6–10) tipo y número de documento y nombres del profesional; (11–14)
tipo/número de documento, nombres y ámbito del paciente (solo los tipos con casilla en el
formulario: CC, RC, TI, CE, PA, NV, CD, SC); (17) diagnóstico principal y relacionados;
secciones de cada tecnología (DCI, concentración, forma, vía, dosis, frecuencia, duración,
cantidad total, indicaciones, justificación, CUPS/códigos) y la **firma** capturada en las
casillas de firma del profesional.

**Queda vacío a propósito** (no se captura en el wizard): especialidad y registro profesional
(9, 10); paciente con enfermedad huérfana y cuál (15, 16); requiere soporte nutricional (18);
tipo de medicamento (19); todas las certificaciones (20, 34, 44, 52, 65: medicamento/procedimiento
utilizado o descartado y razones, evidencia, etc.); tipo de prestación (21, 35, 45, 54, 66);
unidad farmacéutica de la cantidad total (31, 62); tipo de producto nutricional, forma y vía del
producto nutricional, tipo de servicio complementario; número de prescripción asociada (53);
y toda la sección XV (transcriptor de la EPS). Con la justificación técnica pasa lo siguiente:
va en la casilla "Justificación No PBS" (25, 38, 47) y en "Justificación" del servicio
complementario (65); el producto nutricional no tiene casilla de justificación en el formulario.

**Verificado con datos reales (2026-10-02)**: con la contingencia de prueba del arnés el
formulario salió con el NIT leído de `DQS_MIP_COD_HAB` (901285019), el tipo de documento y los
cuatro nombres/apellidos del profesional tomados de su ficha, y las dos tecnologías en sus
secciones (VI y XIV) con la firma. El nombre de la IPS en `DQS_MIP_COD_HAB` trae comillas y
espacios sobrantes (`\" NATURAL BIOLOGY IPS SAS\"`), por eso el servicio los recorta antes de
pintarlo. Acceso adicional: el detalle de la consulta tiene el botón "Formulario PDF" en el
encabezado del modal y cada fila de la grilla tiene un ícono de PDF junto al de "Ver detalle".

**Compatibilidad con Linux (validada 2026-10-02)**: solo se usa iText (código administrado) con
la fuente estándar PDF Helvetica (sin fuentes del sistema ni `libgdiplus`); la plantilla va
incrustada en el ensamblado (no depende de rutas ni de mayúsculas del sistema de archivos);
grep sobre toda la solución sin `System.Drawing`, `Microsoft.Win32`, `Registry`,
`WindowsIdentity`, `ProtectedData`, `DllImport` ni rutas con `\` (las rutas de archivo usan
`Path.Combine` y normalizan `/`); los `src`/`href` de `App.razor` coinciden letra por letra con
los archivos de `wwwroot` (incluido `js/dialogos.js`); el formato de fecha del formulario usa
cultura invariante. Los scripts SQL 008, 009 y 010 son independientes del sistema operativo.

#### Prueba de punta a punta de la app de formatos (✅, 2026-10-10)

Se probó en Chrome (usuario `cwformatos`, rol Formatos) contra `DQUALITYREC`: **Cargar formato**
(validación de campos vacíos; carga real de un PDF y de un PNG con tipo de documento CC/CE; fila
en `DQS_MIP_FORMATOS`, archivo en `App_Data/mediauploader/formatos/{cia}/{radicado}.{ext}`),
**Consultar formato** (validación, filtro por documento + prestador, y exporte a Excel
`InformeFormatos.xlsx` con la fila cargada), descarga de los archivos por
`/api/formatos/archivo` (PDF y PNG), **Nueva contingencia** de punta a punta con 2 tecnologías
distintas (Procedimiento + Producto nutricional), 2 soportes (PDF + PNG) y firma dibujada
(radicado → diálogo "Ver formulario PDF" → pestaña nueva con el formulario de 5 páginas),
**Consultar nueva contingencia** (grilla, detalle con las 2 tecnologías/soportes/firma, botón
"Formulario PDF", exporte `InformeContingencias.xlsx` con "Procedimiento, ProductoNutricional"),
descarga de soporte y firma por `/api/contingencias/archivo`, caso negativo de ruta no
permitida (`../../appsettings.json` → 404) y de radicado inexistente en el formulario (404), y
la validación del **PQR**. No se envió ningún PQR real (manda correo a destinatarios reales).
Los datos de prueba (2 formatos y 1 contingencia con sus archivos) se eliminaron al terminar;
quedan solo las contingencias del propio usuario.

**Hallazgo (no es un defecto, se mantiene tal cual)**: al cargar un formato, la columna
`DQS_MIP_FORMATOS.NIT` guarda el texto completo del datalist del prestador, `Codigo|Nombre`
(ej. `854400415801|A + mas ambulancias y servicios s.a.s`). Se vio primero como un error (el
comentario de `CargarFormato.razor` dice que "solo se guarda el código") y se llegó a corregir,
pero al revisar los datos reales: 4.846 de 5.577 formatos —incluidos los más recientes, hasta el
2026-09-29— guardan `Codigo|Nombre`, y el filtro de Consultar formato compara contra ese mismo
texto (`LIKE`). Solo 731 filas, anteriores, traen únicamente el código. Guardar solo el código
habría hecho que el filtro dejara de encontrar casi todos los formatos existentes, así que
**se revirtió el cambio** y el comportamiento queda como estaba (la selección del datalist
funciona de punta a punta: cargar con `Codigo|Nombre` y consultar con el mismo texto devuelve
la fila). El comentario del código quedó desactualizado respecto de los datos reales.

#### Prueba de punta a punta de Junta Profesional y Profesionales (✅, 2026-10-11)

Probado en Chrome con el usuario de Juntas (rol 60) contra `DQUALITYREC` y el API real de
MIPRES/SISPRO: **Junta Profesional** — consulta por rango (2026-10-01 a 2026-10-10: 25
prescripciones; el API responde por día y tarda ~5–40 s por llamada, así que el rango de 10 días
tomó cerca de 1 min), exporte `JuntaProfesional.xlsx` (2.236 filas, 15 columnas con encabezados
correctos; exporta el rango completo, no solo la grilla), **acta PDF** de una prescripción (sale
con los datos del prestador, paciente, medicamento, justificación y decisión), modal **Cargas**
(cargar 2 archivos, "Ver" por `/api/juntas/cargue`, eliminar uno a uno, ruta no permitida →
404), modal **Firma** (carga la búsqueda de profesionales) y modal **Correo** (validación de
destinatario vacío y envío real del acta a `desarrollo@imperium.net.co`; el PDF queda en Cargas,
como en el legado). **Profesionales** — resumen, alta (con validación de obligatorios, usuario
de aplicación y contraseña generados), edición, **Inactivar** (`ESTADO=0` + fecha/usuario de
inactivación en `DQS_MIP_PROFESIONALES` y usuario espejo `Inactivo` en `DQS_REC_USUARIOS`) y
**Reactivar**. Los datos de prueba (profesional QA 9999000111 y su usuario espejo, archivos de
Cargas) se eliminaron al terminar.

**Hallazgos**:
- **Búsqueda por nombre del modal Firma no devuelve nada**: `JuntasRepository.BuscarProfesionalesAsync`
  replica un defecto del legado y manda `nombre` también como `@serv` (y `servicio` como
  `@sede`) a `sp_DQS_ConsultaProfesional`; al buscar "Pinzon" se filtra además por
  `SERVICIO LIKE 'Pinzon'` y no hay resultados, mientras que la búsqueda sin filtros sí trae al
  profesional. Corregirlo sería pasar `("", servicio)` en vez de `(servicio, nombre)` — **no se
  cambió** porque es una réplica documentada del legado; queda pendiente de decisión.
- **No se probó asignar un profesional a un acta** (modal Firma → ASIGNAR) porque
  `JuntasFirmaService` notifica por correo a una dirección fija; tampoco el PQR ni la firma
  real del acta con profesionales asignados.
- Inactivar/Reactivar usan un `confirm()` nativo del navegador: bloquea las herramientas de
  automatización (se probó anulando `window.confirm` en la página).

---

## Tablas

| Tabla | Usada por |
|---|---|
| `DQS_REC_USUARIOS` | Login — autenticación, marca de sesión; también el SP de consulta de formatos la usa internamente para resolver el rol del usuario (`SELECT ROL FROM DQS_REC_USUARIOS WHERE USUARIO=@USUARIO`) |
| `DQS_OPT_LOGUEO` | Login — registro de sesión (`sp_opt_ingresalogueo`); verificado en vivo, fila nueva insertada correctamente |
| `dbo.ImperiumSesionPuente` | Login — tabla puente compartida con Recobros (mismo objeto físico), token de un solo uso hacia esta app. Columna `IdRol` agregada de forma aditiva (2026-09-02, ver "Página 6") para el flujo de gestión — NULLable, Recobros no la usa |
| `DQS_MIP_FORMATOS` | Cargar formato — destino de `sp_MIP_InsertaFormatoCnt` (columnas: CIA, TIPOID, NUMID, NIT, OBJ, RADICADO, RUTA, FECHA, EMAIL, FECHAFORMATO, USUARIO, ESTADO, más NOPRESCRIPCION/OBSERVACIONES que el SP no llena); origen de Consultar formato vía `sp_MIP_ConsultaFormatosCNT`; destino de Gestionar formato vía `sp_MIP_ActualizaFormatoCnt` (actualiza NOPRESCRIPCION/ESTADO/OBSERVACIONES); EMAIL también se consulta aparte por SQL directo (`ConsultarEmailAsync`) para el correo de confirmación; verificado en vivo en las tres direcciones |
| `DQS_MIP_COD_HAB` | Cargar formato — catálogo de prestadores habilitados (columnas usadas: Codigo, Nombre), autocompletado del campo "Código de Habilitación - Prestador"; verificado en vivo con consulta parametrizada. Reutilizada por el wizard "Nueva Contingencia" (paso 2, `FormatosRepository.ObtenerDetalleAsync`) para el panel informativo tras elegir una IPS — columnas adicionales `Extra_II_NroIDPrestador` (NIT), `Extra_V_NombreMpioSede`/`Extra_VI_NombreDptoSede` (Municipio/Departamento), `Extra_VIII` (Clase de prestador), `Habilitado`; confirmado contra las 199.423 filas de `DQUALITYREC` que esa tabla NO tiene dirección/teléfono/correo (`Extra_IX`/`Extra_X`/`Descripcion` siempre vacías) — ver "Página 11" |
| `DQS_MIP_TOKEN` | Gestionar formato — credenciales (NIT, TOKEN) del WS externo de prescripciones MIPRES, por CIA+Regimen+Detalle, vía `sp_DQS_ConsultaFiltros` `@INTVALIDA=136`; verificado en vivo con credenciales reales de la CIA 890330031 |
| `DQS_REC_ROLMODULO`, `DQS_REC_ROLES`, `DQS_REC_MODULOS` | Login/Inicio — módulos visibles por rol, usadas internamente por `sp_DQS_ConsultaModxRol` (`JOIN` de las 3 por CIA+IDROL/ID_MODULO, confirmado leyendo `OBJECT_DEFINITION`) para armar el claim `imperium:modulo` que gatea el sidebar (`TieneModuloExacto("Contingencia")` / `TieneModuloExacto("Junta Profesionales")`) |
| `DQS_MIP_CRUZAR` | Junta Profesional — tabla de cruce genérica (columnas `CODIGO`, `DESCRIPCION`, `CAMPO`) usada para traducir decenas de códigos del acta en PDF (municipio, IPS, diagnóstico, ámbito de atención, vía de administración, forma farmacéutica, CUPS, dispositivo, servicio complementario), vía `sp_DQS_ConsultaFiltros @INTVALIDA=138`; verificado en vivo (`Campo` no siempre coincide en mayúsculas con el código legado, cruce sin distinguir mayúsculas). También usada por `NuevaContingencia.razor` desde 2026-10-01: paso 4 `CodAmbAte`; paso 7 Medicamento `CodFF`/`CodVA`; paso 8 Medicamento `DosisUM` (268 filas) y `CODFREADMON` (7 filas originalmente, completado a 8 insertando `CODIGO='8'/DESCRIPCION='Única'` a pedido del usuario; además se corrigieron 3 descripciones sucias preexistentes de `CODFREADMON` — `'Hora(s'` sin cierre, un salto de línea en medio de "Según respuesta al tratamiento", y `\r` sueltos al final de 4 filas) |
| `DQS_MIP_DCI` | Catálogo de denominaciones comunes internacionales (DCI) + concentración, sin código propio — columna real `[Descripcion DCI Concentracion]` (6.607 filas en `DQUALITYREC`, 5.467 con `[Habilitado MIPRES]='1'`); usada desde 2026-10-01 por el paso 7 "Detalle tecnología" del wizard para Medicamento (`DciRepository`, búsqueda por contención, no por prefijo — la mayoría de los valores empieza con `"["`); verificado en vivo |
| `DQS_MIP_PROFESIONALESACTA` | Junta Profesional — qué profesionales (por `NoIDENTIFICACION`) firmaron cada acta (`ACTA`) de cada CIA. Leída por `sp_DQS_ConsultaProfesionalAñadido` (panel "Profesionales asignados"/acta en PDF); escrita por `sp_DQS_AsignaProfesional` (INSERT, ESTADO fijo 'APROBADA Y FIRMADA') y `sp_DQS_EliminaProfesional` (DELETE); actualizada por `sp_DQS_ActualizaProfesional` (ESTADO='ENVIADA A EPS' tras notificar) — ver "Página 9" |
| `DQS_MIP_PROFESIONALES` | Junta Profesional y Profesionales — catálogo de profesionales. Columnas originales (Firma, TIPODOC, NoIDENTIFICACION, "APELLIDOS Y NOMBRES", PROFESION, SEDE, SERVICIO, CORREOELECTRONICO, CELULAR, FActivación) ensanchadas a `nvarchar(1000)` el 2026-09-25 en el legado; agregadas `IDPROFESIONAL` (PK de facto, `IDENTITY`), `ESTADO`, `FECHACREACION`/`USUARIOCREACION`, `FECHAACTUALIZACION`/`USUARIOACTUALIZACION`, `FECHAINACTIVACION`/`USUARIOINACTIVACION`, `APELLIDOS`/`NOMBRES` — ver "Página 10". Consultada por `sp_DQS_ConsultaProfesional` (búsqueda del modal "Firma", ver "Página 9"), por los 7 `sp_DQS_MIP_Profesionales*` (CRUD de "Página 10") y, vía join con `DQS_MIP_PROFESIONALESACTA`, por `sp_DQS_ConsultaProfesionalAñadido` (que desde el 2026-09-25 también trae `Firma`) — confirmada con 0 filas (cualquier CIA) en `DQUALITYREC` y en `45.169.253.168/DQUALITY` antes y después de la migración de esquema del 2026-09-29. Reutilizada por el wizard "Nueva Contingencia" (paso 3, vía `IProfesionalesService.ConsultarAsync` — sin SP ni consulta nueva) para el panel informativo tras elegir un profesional; confirmado que NO tiene columna de "registro profesional" (tarjeta/matrícula) ni verificación contra RETHUS — solo el `ESTADO` local de esta tabla — ver "Página 11" |
| `DQS_MIP_JUNTASC` | Junta Profesional — registro de qué prescripción/tipo de tecnología/consecutivo tiene documentos cargados (columnas confirmadas: CIA, MIPRES, TIPOT, CONT, FECHA), vía `sp_DQS_ActualizaJunta` (DELETE+INSERT, una vez por archivo subido en el modal "Cargas") — ver "Página 9" |
| `dbo.DQS_MIP_CONTINGENCIAS_TEC` | Wizard "Nueva Contingencia" — una fila por tecnología de cada contingencia (tipo, `TEC_*`, `POS_*`, justificación; `ORDEN` 1..n); creada 2026-10-02 (`010_MipContingenciasMultiplesTecnologias.sql`) y poblada por `sp_MIP_InsertaContingencia` vía JSON; las columnas equivalentes salieron de `DQS_MIP_CONTINGENCIAS` |
| `dbo.DQS_MIP_CONTINGENCIAS` | Wizard "Nueva Contingencia" — tabla nueva sin precedente legado (columnas listadas en "Página 11"), destino de `sp_MIP_InsertaContingencia`/`sp_MIP_ActualizaContingenciaArchivos`, origen de `sp_MIP_ConsultaContingencias`; ampliada 2026-09-30 con `PACIENTE_FECHAATENCION`/`PACIENTE_HORAATENCION`/`PACIENTE_AMBITOATENCION_CODIGO`/`PACIENTE_AMBITOATENCION_NOMBRE` y, el mismo día, con `DIAGNOSTICO_REL1_CODIGO`/`DIAGNOSTICO_REL1_DESCRIPCION`/`DIAGNOSTICO_REL2_CODIGO`/`DIAGNOSTICO_REL2_DESCRIPCION` (dos diagnósticos relacionados opcionales); verificado en vivo, 0 filas al cierre en ambos servidores |
| `dbo.DQS_MIP_AFILIADOS_TOKEN` | Paso 4 "Paciente" del wizard — bearer token del web service externo de afiliados (`imperiumgitsanitas.net`), tabla append-only (ID, TOKEN, FECHACREACION, USUARIO), leída en vivo antes de cada llamada (nunca cacheada); mismo criterio de "token en BD por si expira" ya usado en `DQS_MIP_TOKEN` — ver "Página 11" |
| `DQS_REC_CARGUECIE10` | Diagnóstico (wizard "Nueva Contingencia") — mismo catálogo compartido que usa Recobros (`Código`/`Descripción`/`Marca`, SQL directo sin SP, ver `Cie10Repository`); 12.634 filas confirmadas en `DQUALITYREC`. Columna `Marca` (nunca documentada antes, ni en este documento ni en el de Recobros): solo 3 valores reales en toda la tabla (`A`/`F`/`M`), verificados empíricamente como marcador de sexo al que aplica el diagnóstico (`A`=Ambos, `F`=Solo mujeres, `M`=Solo hombres — próstata siempre `M`, embarazo siempre `F`), usada por el panel informativo del paso 5 — ver "Página 11" |
| `dbo.DQS_MIP_CONT_PARAMETRICAS` | Wizard "Nueva Contingencia" — catálogo paramétrico genérico nuevo (`GRUPO`/`CODIGO`/`VALOR`/`ORDEN`/`ACTIVO`), reutilizable para futuros listados cerrados del wizard; primer grupo sembrado: `CAUSAL` (6 valores), vía `sp_MIP_ConsultaParametricas` — ver "Página 11" |
| `dbo.DQS_MIP_CIA_PARAMETROS` | Multi-CIA (2026-10-14) — parámetros por CIA (`CIA`/`CLAVE`/`VALOR`); primer uso `AFILIADOS_ORIGEN` (`WS`/`MANUAL`) y `AFILIADOS_ENTIDAD` del paso 4 del wizard, vía `sp_MIP_ConsultaCiaParametros` — ver "Paso 2 — respuestas del usuario" |
| `DQS_MIP_CODIGOS` | Paso 7 "Detalle tecnología" (wizard "Nueva Contingencia") — catálogo real de códigos MIPRES para Procedimiento/Dispositivo médico/Producto Nutricional/Servicio complementario/Medicamento (columna `Tipo`), filtro real por `TipoServicio` (`P`/`D`/`N`/`S`/`M`); 206.557 filas confirmadas en `DQUALITYREC`, sin SP propio (SQL directo, ver `CodigosRepository`). Columna `CIA` presente pero con **todas** las filas en `CIA='NEPS'` — ninguna en `890330031` (EPS Sanitas) — por eso la búsqueda filtra `CIA='NEPS'` fija a pedido explícito del usuario, no la CIA de la sesión; columna `Fomra` (sic) es un error de tipeo real del legado, preservado tal cual — ver "Página 11" |

## Procedimientos almacenados

| Procedimiento | Parámetros | Usado desde |
|---|---|---|
| `sp_DQS_ConsultaUsuario` | `@STRUSU, @STRPWD` | `LoginRepository.ConsultarUsuarioAsync` — mismo SP que Recobros, ver su documentación para los efectos secundarios (desbloqueo automático, etc.) |
| `sp_DQS_ConsultaModxRol` | `@STRCIA, @STRROL` | `LoginRepository.ConsultarModulosPorRolAsync` |
| `sp_DQS_ActualizaMarcaUsuario` | `@STRCIA, @STRUSUARIO, @INTMARCA` | `LoginRepository.ActualizarMarcaUsuarioAsync` — verificado en vivo (MARCA 0→1 en login exitoso) |
| `sp_opt_ingresalogueo` | `@CIA, @USUAPP, @IP` | `LoginRepository.RegistrarLogueoAsync` — verificado en vivo, envuelto en Try/Catch (réplica fiel de Login3.aspx.vb) |
| `sp_DQS_ActualizaPwd` | `@STREMAIL, @STRPWD` | `LoginRepository.ActualizarPasswordAsync` — flujo de recuperar clave agregado por decisión del usuario (Login3.aspx.vb original nunca llegaba a usarlo, ver sub-sección "Recuperar clave") |
| `sp_MIP_InsertaFormatoCnt` | `@CIA, @TIPOID, @NUMID, @NIT, @OBJ, @ext, @email, @fechaformato, @usuario` (9 parámetros, confirmados) | `FormatosRepository.InsertarAsync` — verificado en vivo (radicado generado y fila insertada correctamente, luego eliminada) |
| `sp_MIP_ConsultaFormatosCNT` | `@CIA, @TIPO, @ID, @NIT, @RADICADO, @FECHA, @USUARIO` (7 parámetros, confirmados leyendo `OBJECT_DEFINITION` — tiene una rama de comportamiento distinta por rol interna al SP, ver "Página 5") | `FormatosRepository.ConsultarAsync` — verificado en vivo contra datos reales de `cwformatos` y de un usuario rol 29 |
| `sp_MIP_ActualizaFormatoCnt` | `@CIA, @mipres, @obj, @estado, @usuario, @radicado` (6 parámetros, confirmados leyendo `OBJECT_DEFINITION` — el `UPDATE` real solo usa CIA+RADICADO, `@usuario` no se usa dentro del SP) | `FormatosRepository.ActualizarGestionAsync` — verificado en vivo (cambio real confirmado leyendo la base, valor de prueba restaurado después) |
| `sp_DQS_ConsultaFiltros` | 13 parámetros (`@STRCIA, @INTVALIDA, ...` — mismo SP "multiplexor" ya documentado en Recobros; internamente delega a `sp_DQS_ConsultaFiltros_Core`) | `FiltrosRepository.ConsultarAsync` con `@INTVALIDA=136` (token/NIT del WS MIPRES) y `@INTVALIDA=138` (`DQS_MIP_CRUZAR`, tabla maestra de cruce para el acta de Junta Profesional) — verificado en vivo, devuelve NIT/TOKEN reales de `DQS_MIP_TOKEN` para la CIA 890330031 |
| `sp_DQS_ConsultaProfesionalAñadido` | `@CIA, @ACTA` | `JuntasRepository.ConsultarProfesionalesActaAsync` — join `DQS_MIP_PROFESIONALESACTA`+`DQS_MIP_PROFESIONALES` por CIA+NoIDENTIFICACION, filtrado por CIA+ACTA; confirmado leyendo `OBJECT_DEFINITION` |
| `sp_DQS_ConsultaProfesional` | `@cia, @doc, @nom, @prof, @sede, @serv` (LIKE parcial contra `DQS_MIP_PROFESIONALES`, `""` = todos) | `JuntasRepository.BuscarProfesionalesAsync` — bug real del legado replicado: `@sede` recibe el texto de "Servicio" y `@serv` repite el de "Nombre" (ver "Página 9") |
| `sp_DQS_AsignaProfesional` | `@CIA, @ACTA, @DOC` | `JuntasRepository.AsignarProfesionalAsync` — INSERT en `DQS_MIP_PROFESIONALESACTA` con ESTADO fijo `'APROBADA Y FIRMADA'`; confirmado leyendo `OBJECT_DEFINITION` |
| `sp_DQS_EliminaProfesional` | `@CIA, @ACTA, @DOC` | `JuntasRepository.EliminarProfesionalAsync` — DELETE en `DQS_MIP_PROFESIONALESACTA`; confirmado leyendo `OBJECT_DEFINITION` |
| `sp_DQS_ActualizaProfesional` | `@CIA, @ACTA` | `JuntasRepository.ActualizarEstadoActaAsync` — UPDATE `DQS_MIP_PROFESIONALESACTA SET ESTADO='ENVIADA A EPS'`; confirmado leyendo `OBJECT_DEFINITION` |
| `sp_DQS_ActualizaJunta` | `@Cia, @MIPRES, @TIPOT, @CONT` | `JuntasRepository.RegistrarCargueAsync` — DELETE+INSERT en `DQS_MIP_JUNTASC`; confirmado leyendo `OBJECT_DEFINITION` |
| `sp_DQS_MIP_ProfesionalesConsultar` | `@CIA, @Busqueda, @Profesion, @Sede, @Estado` (todos menos `@CIA` opcionales) | `ProfesionalesRepository.ConsultarAsync` — listado con filtros de "Página 10"; trae también `USUARIOAPP` (subselect contra `DQS_REC_USUARIOS` por CEDULA+CIA) |
| `sp_DQS_MIP_ProfesionalesObtener` | `@CIA, @IdProfesional` | `ProfesionalesRepository.ObtenerAsync` — una fila, para el formulario de edición (no trae `FECHAINACTIVACION`/`USUARIOINACTIVACION`, a diferencia de Consultar) |
| `sp_DQS_MIP_ProfesionalesInsertar` | `@CIA, @TipoDoc, @NoIdentificacion, @Apellidos, @NombresProf, @Profesion, @Sede, @Servicio, @Correo, @Celular, @FActivacion, @Firma, @Usuario, @IdEmpresa, @Clave` (15) | `ProfesionalesRepository.InsertarAsync` — INSERT + `EXEC sp_DQS_MIP_ProfesionalesSincronizarUsuario` en la misma transacción; devuelve RESULTADO/IDPROFESIONAL/MENSAJE/USUARIOAPP/CLAVEAPP/USUARIOCREADO |
| `sp_DQS_MIP_ProfesionalesActualizar` | `@CIA, @IdProfesional, @TipoDoc, @NoIdentificacion, @Apellidos, @NombresProf, @Profesion, @Sede, @Servicio, @Correo, @Celular, @FActivacion, @Firma, @Usuario, @IdEmpresa, @Clave` (16) | `ProfesionalesRepository.ActualizarAsync` — UPDATE + `EXEC sp_DQS_MIP_ProfesionalesSincronizarUsuario` (conserva el `ESTADO` que ya tenía el profesional) en la misma transacción |
| `sp_DQS_MIP_ProfesionalesCambiarEstado` | `@CIA, @IdProfesional, @Estado, @Usuario` | `ProfesionalesRepository.CambiarEstadoAsync` — "Inactivar" no borra la fila (`ESTADO=0` + `FECHAINACTIVACION`/`USUARIOINACTIVACION`); el usuario espejo en `DQS_REC_USUARIOS` sigue la misma suerte (`ESTADO='Activo'/'Inactivo'`) |
| `sp_DQS_MIP_ProfesionalesResumen` | `@CIA` | `ProfesionalesRepository.ResumenAsync` — TOTAL/ACTIVOS/INACTIVOS/SEDES para las tarjetas de resumen |
| `sp_DQS_MIP_ProfesionalesListas` | `@CIA` | `ProfesionalesRepository.ListasAsync` — valores distintos de PROFESION y SEDE, para los `<datalist>`/combos de filtro |
| `sp_DQS_MIP_ProfesionalesResolverUsuario` *(helper interno)* | `@CIA, @Cedula, @Usuario OUTPUT, @YaExiste OUTPUT` | Solo invocado por `sp_DQS_MIP_ProfesionalesSincronizarUsuario` — no tiene método propio en `IProfesionalesRepository` (`IStoredProcedureExecutor` no soporta parámetros `OUTPUT`; ver "Página 10") |
| `sp_DQS_MIP_ProfesionalesSincronizarUsuario` *(helper interno)* | `@CIA, @Cedula, @Apellidos, @Nombres, @Email, @Estado, @Imagen, @UsuarioSesion, @IdEmpresa, @Clave, @Usuario OUTPUT, @Creado OUTPUT, @Mensaje OUTPUT` | Solo invocado por `Insertar`/`Actualizar` — crea o actualiza el usuario espejo en `DQS_REC_USUARIOS`, copiando ROL/TOKEN/Cargo/PWDTEMP/FechaPwd del usuario en sesión |
| `sp_MIP_InsertaContingencia` | ~47 parámetros (uno por campo del wizard, incluidos `@CausalNombre`/`@CausalDescripcion` agregados 2026-09-29, `@PacienteFechaAtencion`/`@PacienteHoraAtencion`/`@PacienteAmbitoAtencionCodigo`/`@PacienteAmbitoAtencionNombre` y `@DiagnosticoRel1Codigo`/`@DiagnosticoRel1Descripcion`/`@DiagnosticoRel2Codigo`/`@DiagnosticoRel2Descripcion` agregados ambos el 2026-09-30, `@PosIndicacionesAdicionales` agregado 2026-10-01 justo después de `@PosIndicadorRecurrencia`, `@TecPrincipioActivo` agregado el mismo día justo después de `@TecCodigo`, y `@PosCantidadFormuladaUnidad` agregado el mismo día justo después de `@PosCantidadFormulada`, ver "Página 11") | `ContingenciasRepository.InsertarAsync` — INSERT en `DQS_MIP_CONTINGENCIAS` (sin `FIRMA_RUTA`/`SOPORTES_RUTAS_JSON`, se completan después), devuelve el radicado generado; verificado en vivo |
| `sp_MIP_ConsultaTokenAfiliados` | *(sin parámetros)* | `AfiliadoTokenRepository.ObtenerVigenteAsync` — trae el token vigente más reciente de `DQS_MIP_AFILIADOS_TOKEN`, leído en vivo antes de cada llamada al web service de afiliados (sin caché); verificado en vivo — ver "Página 11" |
| `sp_MIP_InsertaTokenAfiliados` | `@Token, @Usuario` | Sin método propio en `IAfiliadoTokenRepository` (no hay pantalla de administración) — pensado para rotar el JWT manualmente por SQL directo cuando expire, insertando una fila nueva (append-only, no reemplaza la anterior); usado así para sembrar el token real de esta sesión — ver "Página 11" |
| `sp_MIP_ActualizaContingenciaArchivos` | `@CIA, @Radicado, @FirmaRuta, @SoportesRutasJson` | `ContingenciasRepository.ActualizarArchivosAsync` — UPDATE de las dos columnas de archivos una vez guardados en disco; verificado en vivo |
| `sp_MIP_ConsultaContingencias` | `@CIA, @Usuario, @Radicado, @Causal, @IpsCodigo, @ProfesionalDocumento, @PacienteNumDoc, @TipoTecnologia, @FechaInicio, @FechaFin` (todos menos `@CIA` opcionales) | `ContingenciasRepository.ConsultarAsync` — sin precedente legado, filtros `NULLIF`/`IS NULL`; recreado 2026-09-29 agregando `CAUSAL_NOMBRE`/`CAUSAL_DESCRIPCION` al `SELECT`, y tres veces más el 2026-10-01 agregando `POS_INDICACIONESADICIONALES`, `TEC_PRINCIPIOACTIVO` y `POS_CANTIDADFORMULADAUNIDAD`; verificado en vivo |
| `sp_MIP_ConsultaContingenciaTecnologias` | `@CIA, @RadicadosJson` | `ContingenciasRepository.ConsultarTecnologiasAsync` — tecnologías de una lista de radicados (arreglo JSON), ordenadas por radicado y `ORDEN`; nuevo 2026-10-02 |
| `sp_MIP_ConsultaParametricas` | `@Grupo = NULL` | `ParametricasRepository.ConsultarAsync` — trae todo el catálogo `DQS_MIP_CONT_PARAMETRICAS` (o un grupo puntual); `ParametricasService.CargarAsync` lo agrupa en memoria por `GRUPO`, mismo patrón que `CargarCruzarAsync`/`DQS_MIP_CRUZAR`; verificado en vivo — ver "Página 11" |
| `sp_MIP_ConsultaCiaParametros` | `@Cia` | `CiaParametrosRepository.ConsultarAsync` — `CLAVE`/`VALOR` activos de la CIA de la sesión; `CiaParametrosService` los convierte en `ParametrosCia` (origen de afiliados WS/manual, entidad); nuevo 2026-10-14 |
| `sp_MIP_InsertaParametrica` | `@Grupo, @Codigo, @Valor` | `ParametricasRepository.InsertarAsync` — botón "+ Crear" del wizard (2026-09-30); rechaza duplicados `GRUPO`+`CODIGO`, `ORDEN` autocalculado (`MAX+1` de ese grupo); verificado en vivo — ver "Página 11" |

No usados por Login3 (a diferencia de Recobros): `sp_opt_consultalogueo`,
`sp_DQS_InsertarIngresoMod`.

**SQL directo (no procedimiento)**: `FormatosRepository.BuscarPrestadorAsync`
consulta `DQS_MIP_COD_HAB` parametrizada — reemplaza al `WebMethod Ciudad`
del legado, que concatenaba el texto tecleado directo en el SQL (inyección
real, ver sección de la página "Cargar formato"). `FormatosRepository.
ConsultarEmailAsync` consulta `DQS_MIP_FORMATOS.EMAIL` parametrizada (ver
"Página 6"). `Cie10Repository.BuscarAsync` (Dapper directo, mismo patrón que
`SesionPuenteRepository`) consulta `DQS_REC_CARGUECIE10` parametrizada, portada
de `LiquidacionAutocompleteRepository.BuscarCie10Async` de Recobros — ver
"Página 11".

**Dependencia externa que no es base de datos propia**: la API real del
gobierno colombiano SISPRO/MIPRES (`wsmipres.sispro.gov.co/WSMIPRESNOPBS/
api/...`), usada en dos puntos:

- `MipresPrescripcionValidator` llama a
  `.../PrescripcionXNumero/{NIT}/{TOKEN}/{noPrescripcion}` al marcar
  "Gestionado" en el modal de Gestionar formato (ver "Página 6").
- `JuntaProfesionalApiClient` (módulo Junta Profesional, "Página 8") llama
  a `.../JuntaProfesionalXFecha/{NIT}/{TOKEN}/{fecha}`,
  `.../JuntaProfesionalXPaciente/{NIT}/{fecha}/{TOKEN}/{tipoDoc}/{numDoc}`,
  `.../JuntaProfesional/{NIT}/{TOKEN}/{noPrescripcion}` (búsqueda por
  prescripción y, con régimen fijo "Contributivo", datos del acta) y
  `.../PrescripcionXNumero/{NIT}/{TOKEN}/{noPrescripcion}` (datos clínicos
  completos para el PDF del acta).

Replicadas tal cual por decisión explícita del usuario; si alguna vez dejan
de estar disponibles o cambian de contrato, esto rompe el flujo de
"Gestionado" o el módulo Junta Profesional respectivamente (no el resto de
la app).

**Segunda dependencia externa (2026-09-30, wizard "Nueva Contingencia")**:
`AfiliadoApiClient` llama a `POST
https://imperiumgitsanitas.net:51044/tutelas/tutelasmipres` (bearer token en
`DQS_MIP_AFILIADOS_TOKEN`, ver "Página 11") para traer los datos del afiliado
en el paso 4 "Paciente". Si este servicio deja de estar disponible o el JWT
expira sin rotarse, el paso 4 no puede completarse (no hay fallback manual —
decisión explícita: el prototipo pedía consumir un web service real, no un
formulario editable).

---

## Estructura de la solución

```
Contingencia 2026 core/
├── Contingencia.Core.slnx
├── Contingencia.Domain/          (biblioteca de clases, net10.0)
├── Contingencia.Infrastructure/  (biblioteca de clases, net10.0 — Dapper, Microsoft.Data.SqlClient)
├── Contingencia.Application/     (biblioteca de clases, net10.0 — referencia Domain + Infrastructure)
└── Contingencia.Web/             (Blazor Web App, Interactive Server, net10.0 — referencia Application + Domain)
```

Mismo grafo de dependencias que `Imperium.Core.slnx` (Recobros): `Web` →
`Application` → `Infrastructure` → `Domain`, y `Web` también referencia
`Domain` directo. Paquetes NuGet igualados uno a uno con los de Recobros
(`Dapper 2.1.79`, `Microsoft.Data.SqlClient 7.0.2`,
`Microsoft.Extensions.Configuration.Abstractions/Binder 10.0.11`,
`Microsoft.Extensions.Hosting.Abstractions 10.0.11`, `Newtonsoft.Json
13.0.4` en Infrastructure; `Microsoft.Extensions.Caching.Memory 10.0.11` en
Application), más 3 paquetes nuevos en `Contingencia.Infrastructure` sin
equivalente en Recobros, agregados para el módulo Junta Profesional
(generación del acta en PDF, ver "Página 8"): `itext7 8.0.5`,
`itext7.bouncy-castle-adapter 8.0.5` (requerido por `itext7` para poder
escribir cualquier PDF) y `BouncyCastle.Cryptography 2.6.2` (versión fijada
explícitamente por encima de la `2.2.1` que trae el adapter, que tiene CVEs
moderados conocidos).

**Ya migrado**: estructura base, página `Opciones` (`/`), página `Login`
(`/login`, con autenticación por cookie propia, captcha Turnstile,
recuperar clave, y tabla puente de sesión compartida con Recobros — ahora
también con el `IdRol` real del usuario), página `Inicio` (`/inicio`,
destino real del rol 601) con su propio `AppLayout`/`AppLayout.razor.css`/
`js/app-layout.js`, página `Cargar formato` (`/formatos/cargar`, inserta en
`DQS_MIP_FORMATOS`, sube archivo, autocompletado de prestador
parametrizado), página `Consultar formato` (`/formatos/consultar`,
búsqueda + grilla + exportación a Excel + descarga protegida del archivo
vía `/api/formatos/archivo`, **más el modal "Gestionar formato"** —
`JuntasFormatoCntCO2.aspx` migrado, visible solo para roles 27/29/30, que
además ahora son enrutados directo a esta página al loguearse), página
`Buzón PQR` (`/sugerencias`, formulario de correo, último enlace del menú
que apuntaba al legado), y página `Junta Profesional` (`/juntas`, segundo
flujo de rol de esta app — consulta en vivo contra la API de SISPRO y
genera el acta de junta en PDF con iText7, ver "Página 8") — ver secciones
arriba. `Contingencia.Web` partió del esqueleto de `dotnet new
blazor --interactivity Server` sin páginas de ejemplo
(`Counter`/`Weather`/`Home`) ni el `NavMenu` de plantilla — `App.razor`,
`MainLayout.razor` (simplificado, sin sidebar, igual que en Recobros),
`ReconnectModal.razor`, `Error.razor`, `NotFound.razor`.

**Todavía sin construir / fuera de alcance**: `JuntasFormatoCntCO2.aspx`
como página aparte (ya migrada como modal, ver arriba) — lo que queda fuera
es solo la variante de la "Gestion" HTML del SP para roles que NO son
601/27/29/30 (no existen en este cliente/alcance). El sidebar de
`AppLayout` sigue gateado por el claim de módulo "Contingencia"
(`TieneModuloExacto`), que los roles 27 y 30 NO tienen asignado para esta
CIA (solo el 29 sí) — decisión deliberada de no tocar esa lógica: como el
login ya los manda directo a `/formatos/consultar`, no dependen del
sidebar para llegar ahí, solo no verían el enlace si navegan a otra parte
y quieren volver (caso de uso no pedido en esta ronda). `JuntasCargueDoc.
aspx` y `JuntasFirma.aspx` (las páginas que antes abrían los botones
"Cargas" y "Firma" de cada fila en Junta Profesional) ya están migradas
como modales dentro de `/juntas` (2026-09-03, ver "Página 9") — lo único
que queda fuera de esas dos páginas es el lienzo de captura de firma a
mano alzada de `JuntasFirma.aspx`, que es código muerto en el propio
legado (bloque comentado, nunca alcanzable desde la UI real).

### Correcciones de `AppLayout.razor` (2026-09-02, reportadas por el usuario con captura real)

Tres ajustes al layout compartido, ninguno propio de una sola página:

- **Logo del cliente (EPS Sanitas) quedaba pegado al fondo absoluto del
  sidebar**: `.app-sidebar-cliente` tenía `margin-top: auto`, que en un
  contenedor flex de `height: 100vh` lo empuja hasta el final físico de la
  pantalla — con un menú tan corto (solo "Formato contingencia" y "PQR", a
  diferencia del menú más largo de Recobros donde este mismo patrón sí
  tiene sentido) eso dejaba un hueco enorme y el logo casi fuera de vista
  en pantallas chicas. Cambiado a `margin-top: 1.5rem` — ahora sigue el
  flujo normal, justo debajo del menú.
- **El resaltado "activo" del menú lateral estaba fijo en el markup, no
  reflejaba la ruta real**: tanto el grupo "Mipres"/"Formato contingencia"
  como (la falta de) el de "PQR" tenían la clase `active` hardcodeada o
  ausente sin importar en qué página se estuviera — visitar `/sugerencias`
  nunca resaltaba "PQR", y "Formato contingencia" quedaba resaltado
  siempre, incluso estando en el buzón PQR. Se agregó
  `AppLayout.EsSeccionActiva(prefijo)` (compara `Navigation.Uri` contra el
  prefijo de ruta) y se aplicó a ambas entradas — "Mipres" se resalta en
  `/inicio` y en cualquier `/formatos/*` (sus páginas hijas), "PQR" solo en
  `/sugerencias` exacto.
- **Responsive verificado end-to-end con Claude in Chrome** (ya que
  `resize_window` no cambió el viewport real de la ventana del usuario —
  probablemente porque estaba maximizada —, se simuló un viewport de
  375×812 con un `<iframe>` inyectado por script apuntando a la misma app,
  aprovechando que comparte cookies por ser mismo origen; se limpió al
  terminar): sin overflow horizontal a nivel de documento en `/sugerencias`,
  `/formatos/cargar` y `/formatos/consultar` (con y sin resultados); el
  sidebar colapsa a overlay con backdrop en `<900px` (ya existía, solo se
  confirmó); la tabla de resultados de "Consultar formato" (13 columnas)
  desborda **solo dentro de su propio contenedor** `.fmt-tabla-scroll`, no
  la página — confirmado midiendo `scrollWidth` del contenedor de la tabla
  (1288px) contra su `clientWidth` (253px) mientras el documento completo
  seguía sin overflow (360px = 360px).

**Repo git**: aún no inicializado — pendiente de que el usuario indique el
repositorio destino (igual que se hizo para Recobros con
`https://github.com/D4t42025/Recobros.git`).

### Modal de SweetAlert2 "sin estilo" — bug real encontrado y corregido (2026-09-29), corrige un falso "falso positivo" del 2026-09-02

**Antecedente (2026-09-02, nota original de esta sección, dejada abajo tachada por contexto
histórico)**: el usuario reportó el modal de `Tran`/`TranT`/`Alerta` (SweetAlert2)
apareciendo sin estilo — logo, texto y botón "Gracias" en bruto, pegado a la esquina inferior
izquierda. Se investigó, se reprodujo con **F5 de Visual Studio** (Depurar) y se atribuyó a
**CSS Hot Reload/Browser Link** de VS, con la recomendación de usar **Ctrl+F5 de Visual
Studio** ("Iniciar sin depurar"). Esa causa (Browser Link interfiriendo con el `<style>` que
SweetAlert2 inyecta dinámicamente) es real y sigue siendo válida **para ese escenario
específico** — pero la investigación de entonces solo probó cargas de página sueltas, nunca
el patrón exacto que de verdad dispara el bug real, y por eso concluyó "no es un bug del
código" prematuramente.

**Lo que se descubrió al validar de nuevo (2026-09-29), reportado por el usuario viendo la
app real desplegada por VMware Horizon** (sin Visual Studio de por medio — descarta del todo
la explicación de Browser Link para este caso): el modal sale sin estilo **la primera vez
que se dispara después de llegar a la página por un enlace interno de la app** (navegación
del lado del cliente de Blazor, "enhanced navigation" — sin recarga completa del navegador);
si en cambio se hace **F5 del navegador** (recarga completa) antes de disparar el modal, sale
perfecto. Es decir: el disparador real es "¿hubo una recarga completa del navegador desde la
última vez que se cargó la página, sí o no?", **no** Visual Studio.

**Causa raíz confirmada**: `sweetalert2.min.js` (v11.26.25) no trae una hoja de estilos
separada — inyecta su propio `<style>` con las clases `swal2-*` en `<head>` la primera vez
que se usa, y se queda con una bandera interna de "ya inyecté mis estilos" que nunca vuelve a
revisar. El `<link>` que `App.razor` sí tenía apuntando a `assets/css/sweetalert.css` es de
**SweetAlert v1** (nunca cargado como script — ni siquiera existe `<script
src="assets/js/sweetalert.min.js">` en `App.razor`, solo en `Login.razor`/`Opciones.razor`, y
ninguna de las dos llama a la API v1 tampoco), así que no aportaba nada real: toda la
apariencia del modal dependía por completo de ese `<style>` inyectado por JS. La "enhanced
navigation" de Blazor Web App hace un diff del `<head>` contra el HTML que el servidor
renderiza para la página de destino, y **borra cualquier elemento que no venga en ese HTML**
— incluido el `<style>` que SweetAlert2 agregó dinámicamente, porque nunca fue parte del
`<head>` renderizado por el servidor. Confirmado inspeccionando el DOM en vivo: antes de
navegar por un enlace interno, `document.querySelectorAll('style')` incluye un `<style>` con
`swal2-` en su contenido; justo después de navegar (sin recargar), ese `<style>` ya no está,
aunque `window.Swal` (el JS) sigue disponible y sigue "creyendo" que no hace falta
reinyectarlo.

**Corrección**: se descargó el CSS real de SweetAlert2 en la versión exacta que usa el
proyecto (`assets/css/sweetalert2.min.css`, v11.26.25, pineada igual que el JS) y se
reemplazó el `<link>` de `App.razor` que apuntaba al v1 sin usar. Al ser parte del HTML que
el servidor renderiza en cada navegación, este `<link>` sí sobrevive el diff de "enhanced
navigation" — el modal queda bien estilado sin importar cuántos saltos de navegación interna
hayan pasado antes, sin necesidad de F5. **Verificado en vivo** (`cwformatos`, CIA
`890330031`): reproducido el bug primero (login → clic en tile "Consultar formato" → clic en
"Consultar" vacío → modal roto, igual que reportó el usuario), corregido el `<link>`, y
repetida la misma secuencia además de un tercer salto interno (`/inicio` → "Cargar formato" →
botón vacío) — el modal salió perfecto las dos veces, sin ningún F5 de por medio.

**Alcance**: solo afecta a las páginas que usan `Tran`/`TranT`/`Swal.fire` —
`CargarFormato.razor` y `ConsultarFormato.razor` (vía `dialogos.js`), y `Login.razor`/
`Opciones.razor` (que llaman `Swal.fire` directo desde `login.js`/`opciones.js`, y traían sus
propios `<link>`/`<script>` de SweetAlert v1 redundantes y sin usar — no se tocaron por no ser
la causa del bug, pero quedan como código muerto detectado de paso). **`Juntas.razor` y
`Profesionales.razor` nunca usan SweetAlert** — muestran sus mensajes con un banner de texto
simple (`<p class="fmt-ayuda">`), así que no les aplica este bug en absoluto, ni antes ni
después de esta corrección.

---

### Dependencias NuGet — revisión de vulnerabilidades y actualizaciones (2026-10-13)

Tras el aviso de Visual Studio ("paquetes con vulnerabilidades"):

- **`System.Security.Cryptography.Pkcs` 9.0.13 (High/Moderate, GHSA-846f-f764-76mx y GHSA-2fmv-rv2c-59x7)**: dependencia
  transitiva de `Microsoft.Data.SqlClient`; se fijó la versión segura **10.0.13** con una referencia explícita en
  `Contingencia.Infrastructure.csproj` (`dotnet list package --vulnerable` ya no reporta nada).
- **Actualizados** (parches/menores; compilan y se probaron): `Microsoft.Data.SqlClient` 7.0.2 → 7.1.1 (conexión verificada a
  `DQUALITYREC` y `DQUALITY`), `Dapper` 2.1.79 → 2.1.89, `Microsoft.Extensions.*` 10.0.11 → 10.0.13, `BouncyCastle.Cryptography`
  2.6.2 → 2.7.0 (el acta de Junta con firma se genera sin errores).
- **`itext7` / `itext7.bouncy-castle-adapter` 8.0.5 → 9.8.0** (salto de versión mayor, hecho a pedido del usuario). Único cambio de
  código: en `ActaPdfBuilder` el pie de página con número de página pasó de `IEventHandler`/`Event` (`iText.Kernel.Events`) a
  `AbstractPdfDocumentEventHandler`/`AbstractPdfDocumentEvent` (`iText.Kernel.Pdf.Event`). Verificado: el acta de Junta (2 páginas,
  con pie "Página: N / Copia generada desde IMPERIUM.NET.CO…") y el formulario MIPRES de 2 tecnologías (5 páginas, casillas, dígitos
  por celda y firma) se generan correctamente.
- **`SixLabors.ImageSharp` reemplazado por `SkiaSharp` 4.153.1 (MIT, sin licencia comercial)**. ImageSharp 3.x tenía 5 avisos
  (3 High, 2 Moderate) y la corrección solo existe en 4.x, que exige licencia comercial de Six Labors (las compilaciones *Release*
  fallan sin ella). Se usaba solo en `ActaPdfBuilder` (recorte de la firma del profesional en el acta): ahora
  `SKBitmap.Decode` (RGBA sin premultiplicar) + recorrido del búfer de bytes para hallar el rectángulo con tinta (mismos umbrales:
  alfa > 16 y no casi blanco > 240, margen 6 px) + `ExtractSubset` + `Encode(Png)`. Se agregó `SkiaSharp.NativeAssets.Linux.NoDependencies`
  (nativo `libSkiaSharp.so` sin fontconfig ni librerías gráficas del sistema); `dotnet publish -r linux-x64` lo incluye. Verificado en
  Windows con firma PNG transparente (lienzo 756×200 → recortada a 171×76 pt en el acta), firma JPG con fondo blanco (igual),
  sin firma y con bytes corruptos (la celda sale solo con el rótulo "Firma:"). El resto de menciones a ImageSharp en este documento
  son históricas.
- Estado final: `dotnet list package --vulnerable --include-transitive` y `--outdated` no reportan nada en la solución Contingencia.

### Prueba de regresión tras la actualización de paquetes (2026-10-13)

Compilación *Release* de la solución sin errores (ni avisos de licencia) y prueba con sesión real en Chrome (apps servidas desde la
salida de compilación, base `DQUALITYREC`):

- **Menú `/`**: 8 tarjetas del perfil SANITASMIPRES, sin imágenes rotas.
- **Formatos**: Consultar nueva contingencia (3 radicados; PDF del formulario MIPRES de los 3: 200, `application/pdf`, 3 páginas con
  una tecnología y 5 con dos; Excel `InformeContingencias.xlsx`), wizard Nueva contingencia (catálogo de causales cargado), Consultar
  formato (1.394 filas; Excel `InformeFormatos.xlsx` de 140 KB), Cargar formato y PQR cargan sin errores.
- **Juntas**: consulta con datos reales, Excel `JuntaProfesional.xlsx` (1,5 MB), acta PDF de una prescripción (200, 2 páginas, logo),
  Profesionales lista el registro y sus contadores.
- **Arnés de backend en ambos servidores (`DQUALITYREC` y `DQUALITY` 45.169.253.168)**: registro de una contingencia con 2 tecnologías
  (Medicamento + Procedimiento) → consulta (2 tecnologías en orden) → actualización de archivos → PDF del formulario con firma
  (SkiaSharp + iText 9); filas de prueba borradas al terminar (3 y 0 contingencias, como antes).
- **Recobros** (`Imperium.Web`): menú y las 16 páginas del módulo cargan sin errores; Indicadores muestra gráficas con datos y exporta
  `Producción.xlsx`.
- No se probó en ejecución: la firma dentro del acta de Junta desde la interfaz (no había una junta con firmas; sí se verificó el
  recorte con SkiaSharp en el arnés), ni el envío real de correos/PQR.

## Paso a producción con una base de datos nueva (en curso desde 2026-10-14; base desplegada el 2026-10-14)

El usuario pidió que Formatos + Juntas (esta app) funcione en producción contra una **base de datos nueva y propia del cliente**. Plan por pasos:

| # | Paso | Estado |
|---|---|---|
| 1 | Extraer el esquema exacto de las 24 tablas y las 34 SP que usa la app a scripts versionados (`Sql/Produccion/`) | ✅ 2026-10-14 |
| 2 | Decisiones del cliente nuevo (quién es, CIA/NIT, servicio de afiliados, hosting, correo, token SISPRO) | ✅ respondidas: CIA 890330031 + 800006850, módulos, afiliados por WS o formulario, Windows/IIS con subdominio creado, base `FormatosCT`, correo ya usado; **pendiente** solo el token `APIPRESCRIPCION` Subsidiado real de 800006850 (hoy provisional = el Contributivo) |
| 3 | SP delgado de filtros (`@INTVALIDA` 136 y 138) en lugar de `sp_DQS_ConsultaFiltros_Core` + datos semilla (cliente, roles, módulos, parámetros, token, usuario) | ✅ 2026-10-14 (el paquete se desplegó y probó en una base vacía) |
| 4 | Carga de catálogos y datos iniciales (herramienta `Herramientas/CopiarCatalogos`) | ✅ ejecutada en `FormatosCT` (2026-10-14), verificada "IGUAL" |
| 5 | Parametrizar lo fijo de Sanitas (catálogo de códigos, URL del WS de afiliados, logo del acta, menú) | ✅ 2026-10-14 |
| 6 | Configuración de producción y hosting (Windows/IIS): variables de entorno, llaves de sesión, carpeta de archivos, HTTPS, despliegue de la base | ✅ código y script de base probados; la parte de IIS está documentada pero **no se probó en un IIS real** |
| 7 | Pruebas contra la base nueva (arnés de backend + Chrome) | ✅ en bases temporales y en producción solo lectura; falta la prueba con la aplicación publicada en IIS |
| 8 | Despliegue, respaldos y entrega | 🔶 **base de producción desplegada (2026-10-14)**; usuario SQL de la aplicación creado; faltan la publicación en IIS, los respaldos y las claves de Turnstile/correo |

### Guía rápida: qué se decidió, qué se hizo y qué falta (resumen del proceso, 2026-10-14)

**Decisiones del usuario, en orden** (el detalle de cada una está en los pasos de abajo):

| # | Decisión | Dónde quedó |
|---|---|---|
| 1 | La app (Formatos + Juntas) va a producción en una **base nueva y propia**, **multi-CIA**: 890330031 (EPS Sanitas) y 800006850 | Paso 2 |
| 2 | 800006850 = **HOSPITAL MARIO GAITAN YANGUAS**, una IPS que usa **solo Formatos** (sin Juntas); Sanitas usa el WS de afiliados; las CIA sin WS usan un **formulario manual de paciente**, parametrizado por CIA | Paso 2, `DQS_MIP_CIA_PARAMETROS` |
| 3 | 800006850 entra con el rol 601 (el de `cwformatos`); usuario único por ahora: **`cwhospital`** (más usuarios, por sede o por persona, se darán de alta después) | Paso 3, `sp_MIP_CrearUsuarioFormatos`, script `06` |
| 4 | Tokens SISPRO de 800006850 entregados; el Subsidiado de `APIPRESCRIPCION` usa **provisionalmente el Contributivo** | Paso 3, script `05` |
| 5 | Migrar la pantalla de **cambio de contraseña** y el **diseño nuevo del login** a Formatos/Juntas y a Recobros; **claves en texto plano por ahora** (sin hash) | Paso 3 (continuación) |
| 6 | Copiar solo catálogos, tokens de Sanitas y los usuarios `cwformatos` y `cwjuntas` (CIA 890330031), **sin adjuntos ni historial**; origen = `DQUALITY` (45.169.253.168) | Paso 4 |
| 7 | Hosting en **Windows (IIS)**, subdominio ya creado, correo = el que ya se usa; base productiva = **`FormatosCT` en 172.25.2.13** | Paso 6 y "Base de producción" |

**Cómo se repite el despliegue de la base desde cero** (un comando; es repetible, lo ya copiado se omite):

```powershell
$env:SQLCMDPASSWORD = '<clave del usuario SQL administrador>'
.\Despliegue\Desplegar-BaseDeDatos.ps1 -Servidor '172.25.2.13' -Base 'FormatosCT' -Usuario '<admin SQL>' `
    -OrigenCadena 'Server=45.169.253.168;Database=DQUALITY;User Id=...;Password=...;TrustServerCertificate=True' `
    -ClaveCwhospital '<clave de cwhospital>' -NuevaClaveCwjuntas '<12 a 20 caracteres con mayuscula, minuscula, numero y simbolo>'
# comprobar sin escribir:  ... -SoloVerificar
```

**Resultado del 2026-10-14 en `FormatosCT`**: 24 tablas, 35 procedimientos, 2 clientes, 3 usuarios, 10 tablas copiadas "IGUAL". Las dos claves nuevas (`cwhospital` y `cwjuntas`) se generaron al azar y **se entregaron al usuario en la conversación; no se guardan en ningún archivo**. La primera vez que cada uno entre puede cambiarla en `/actualizar-contrasena`.

**Orden recomendado para terminar la puesta en producción:**

1. ✅ Usuario SQL de la aplicación `contingencia_app` creado (2026-10-14, ver "Despliegue de la base de producción"); usar **ese** usuario —no `admin`— en `ConnectionStrings__DQUALITY`.
2. Definir las variables de entorno de la tabla C del Paso 6 (incluidas las claves de **Turnstile del subdominio** y el **correo**).
3. Preparar IIS (lista D del Paso 6: WebSocket Protocol, Hosting Bundle net10.0, grupo sin código administrado, carpetas con permiso, certificado) y publicar: `dotnet publish Contingencia.Web -c Release -o <carpeta del sitio>`.
4. Prueba de humo en el subdominio: `/salud` = 200; `/login` con `cwhospital` (ve solo Formatos; `/juntas` lo devuelve a `/inicio`) y con `cwjuntas` (ve Juntas y Profesionales); crear una contingencia de prueba con el formulario manual (800006850) y consultar un afiliado por WS (890330031), y **borrar luego esos datos de prueba**.
5. Probar el login de Recobros con una sesión real (pendiente desde la migración del login-v2).
6. Programar respaldos de `FormatosCT` (modelo FULL: completo + registro de transacciones) y una prueba de restauración.
7. Rotar la clave de la cuenta `admin` de 172.25.2.13 y las que pasaron por la conversación.

**Pendientes con responsable:**

| Pendiente | Quién | Nota |
|---|---|---|
| Confirmar en Cloudflare que el widget permite `formatosct.imperium.net.co` (y, mejor, crear uno propio de producción) | usuario | las claves actuales son las de desarrollo y válidas; sin el hostname permitido el captcha del subdominio fallaría |
| Token `APIPRESCRIPCION` Subsidiado real de 800006850 | usuario | reemplazo: `UPDATE DQS_MIP_TOKEN SET Token='...' WHERE Cia='800006850' AND Detalle='APIPRESCRIPCION' AND Regimen='Subsidiado'` |
| Licencia de 800006850 (vence **2026-12-01**) y de Sanitas (2026-12-31) | usuario | `UPDATE DQS_REC_CLIENTES SET FECHALICENCIA='...' WHERE CIA='...'`; sin licencia vigente el login no devuelve al usuario |
| Pasar las claves a hash | decisión del usuario, aplazada | implica cambiar `sp_DQS_ConsultaUsuario`, `sp_DQS_ActualizaPwd`, `sp_MIP_CambiarClaveUsuario`, el alta y la recuperación de clave |
| Prueba en un IIS real (WebSockets, permisos, certificado, subida de archivos, envío de correo) | quien administre el servidor + Claude | solo se probó en Kestrel en modo Producción |

**Archivos del paquete de producción:** `Sql/Produccion/00`–`07` (base, tablas, procedimientos, filtros delgado, semillas de clientes, seguridad, parámetros por CIA, tokens de 800006850 —**secreto**—, usuario `cwhospital`, ajustes de usuarios copiados); `Despliegue/Desplegar-BaseDeDatos.ps1`; `Despliegue/Crear-UsuarioAplicacion.sql`; `Herramientas/CopiarCatalogos`. Los scripts `Sql/001`–`013` son del historial de las bases de pruebas y **no** se ejecutan en la base nueva.

### Subdominio `formatosct.imperium.net.co` (revisado 2026-10-14)

Consulta de solo lectura desde fuera (DNS, TLS y HTTP), sin cambiar nada:
- **DNS** (Route 53 de `imperium.net.co`): dos registros A hacia IP públicas de AWS (100.48.239.139 y 44.208.127.73) = nodos de un **balanceador de aplicaciones (ALB)**.
- **TLS**: lo termina el balanceador con un certificado de Amazon `CN=imperium.net.co` (**vence el 2026-12-17**; si es de ACM se renueva solo, si no hay que renovarlo). `http://` responde 301 a `https://` (cabecera `Server: awselb/2.0`), así que la redirección 80→443 ya la hace el balanceador.
- **Detrás**: IIS 10 sirviendo solo una página estática "Prueba de publicación IIS" (13-oct); `/login` y `/salud` dan 404: **la aplicación aún no está publicada**.

**Consecuencias para la aplicación** (la petición llega a IIS por el balanceador, probablemente por HTTP):
1. **`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` es obligatoria.** Probado en local (aplicación publicada, Producción, `ASPNETCORE_HTTPS_PORT=443`): sin la variable **todas** las peticiones responden 307 a `https://…` aunque traigan `X-Forwarded-Proto: https` (bucle infinito detrás de un balanceador que entrega por HTTP); con la variable, una petición con `X-Forwarded-Proto: https` responde 200 y una sin la cabecera sigue redirigiendo. Por eso `ASPNETCORE_HTTPS_PORT` ya no se define (ver la tabla C). Con la variable las cookies de sesión salen con el atributo `Secure` porque la aplicación ve HTTPS.
2. **Turnstile** (claves entregadas por el usuario el 2026-10-14): la *site key* es **la misma** que ya está en `appsettings.json` y la clave secreta es **la misma** que la de `appsettings.Development.json`, es decir, el widget de desarrollo se reutiliza en producción. Se comprobó con Cloudflare (`siteverify` con un token falso) que la clave secreta es válida (responde `invalid-input-response`, no `invalid-input-secret`). **No se pudo comprobar** que el widget permita el hostname `formatosct.imperium.net.co` (solo se sabe con un token real): si el captcha falla en el subdominio, agregar ese hostname en el panel de Cloudflare (Turnstile → el widget → Hostnames). Conviene crear un widget propio de producción y dejar el actual solo para desarrollo. La clave secreta no se escribe en ningún archivo del proyecto: va solo en la variable `Turnstile__SecretKey` del sitio.
3. **Blazor Server por el balanceador**: el grupo de destino debe aceptar WebSockets (el ALB los soporta de serie). Si hay **más de un servidor IIS** detrás, activar *sticky sessions* en el grupo de destino (cada circuito de Blazor vive en un servidor) y compartir las llaves de DataProtection (carpeta de red) entre ellos; con uno solo no hace falta.
4. El *tiempo de inactividad* del balanceador (60 s por defecto) no afecta: Blazor envía su latido cada 15 s.
5. **Red**: el servidor IIS debe alcanzar `172.25.2.13` (SQL Server, red privada) y salir a internet hacia SISPRO (`wsmipres.sispro.gov.co`), Cloudflare (Turnstile), el WS de afiliados de Sanitas y el servidor de correo.
6. Si el balanceador reenvía por HTTPS a IIS (certificado propio del servidor) también funciona; la variable no estorba.

**Correo de producción (datos entregados por el usuario el 2026-10-14, tomados de su `web.config` del sistema anterior)**: servidor `smtp.gmail.com` y la cuenta de envíos `dataquality.desarrollo@gmail.com` con su contraseña de aplicación de Gmail. Equivalencias: `Host` → `Smtp__Host`, la cuenta → `Smtp__User`, `pwdcorreo` → `Smtp__Password`. `CorreoAlertas` **no lo usa esta aplicación** (se ignora). El código (`Infrastructure/Correo/EmailSender.cs`) envía por el puerto 587 con SSL, que es lo que Gmail exige. Son los mismos valores que ya estaban en `appsettings.Development.json` (comparados sin imprimirlos), es decir, el mismo remitente con el que se probó el envío de actas y la recuperación de clave. La contraseña no se escribe aquí ni en ningún archivo del proyecto: solo en la variable `Smtp__Password` del sitio. Recomendación: usar una cuenta de envíos distinta para producción y rotar la contraseña de aplicación si esa cuenta se comparte fuera del equipo.

**Script para dejar las variables en IIS** — `Despliegue/Configurar-VariablesIIS.ps1` (ejecutar **en el servidor IIS**, como administrador; PowerShell 5.1 o 7): define con `appcmd` todas las variables de la tabla C en el sitio, repetible, sin secretos en el archivo (se pasan por parámetro) y con `-SoloMostrar` para ver qué haría sin cambiar nada (los secretos salen enmascarados).

```powershell
.\Despliegue\Configurar-VariablesIIS.ps1 -Sitio 'formatosct.imperium.net.co' -ClaveAppSql '<clave de contingencia_app>' `
    -TurnstileSiteKey '<site key>' -TurnstileSecretKey '<secret key>' `
    -SmtpHost '<host>' -SmtpUser '<usuario>' -SmtpPassword '<clave>'
```

Opciones: `-ServidorSql` (172.25.2.13), `-BaseDatos` (FormatosCT), `-UsuarioApp` (contingencia_app), `-CarpetaDatos` (`D:\DatosContingencia`: usa `Llaves` y `Archivos` dentro), `-UrlLogoActa`. Define: `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, la cadena de conexión, las dos carpetas, `Menu__Perfil=FORMATOS_JUNTAS`, Turnstile y, si se pasan, Smtp y logo. **Probado**: la simulación (`-SoloMostrar`) en Windows PowerShell 5.1 (variables correctas, secretos enmascarados, rechaza valores con comillas). **No probado**: la escritura real con `appcmd` (no hay IIS en el equipo de desarrollo). El nombre del sitio es el de IIS (`Get-Website`), que puede no coincidir con el del subdominio. Las carpetas y su permiso para el grupo de aplicaciones se crean a mano (el script lo recuerda al terminar).

**Página de inicio = login (✅ 2026-10-14, a pedido del usuario)**: la raíz `/` ya no muestra la pantalla de Opciones sino que **redirige a `/login`** (302). Quien ya tiene sesión y entra a `/` va a su pantalla en vez de ver el login otra vez: `/juntas` si su único módulo es "Junta Profesionales" y `/inicio` en los demás casos. La pantalla de Opciones sigue existiendo en **`/opciones`** (`Opciones.razor` tiene ahora dos rutas, `/` y `/opciones`, pero la raíz se resuelve antes, en un middleware de `Program.cs`). Los destinos que antes llevaban a `/` (cerrar sesión, "Continuar" tras cambiar la contraseña, sesión caducada) ahora caen en el login o en la pantalla del usuario. Para volver al comportamiento anterior (la raíz muestra Opciones): variable de entorno `Inicio__Pagina=opciones`. Probado con la publicación de producción (Kestrel, sin sesión): `/` → 302 a `/login`, `/login` 200, `/opciones` 200, `/inicio` sin sesión → login con `ReturnUrl`; con `Inicio__Pagina=opciones`, `/` → 200. **No probado aún**: el desvío de quien ya tiene sesión (se verá en el subdominio al iniciar sesión). Solo se cambió Contingencia.Web; Recobros conserva su comportamiento.

**Paquete listo para copiar al servidor** — `Despliegue/Contingencia.Web-produccion.zip` (43 MB; SHA-256 `5308394911174A8BAF5192F181CE206542E38781B7946CD54DB060D9BC13B38F` desde el 2026-10-14 12:31, con el acceso del hospital a Profesionales; el anterior `A93A61AD…7867` ya no vale; se regenera con `dotnet publish Contingencia.Web -c Release -r win-x64 --self-contained false -o <carpeta>` y comprimiendo su contenido). Es la publicación **dependiente del marco** (necesita el .NET 10 Hosting Bundle en el servidor, no el SDK) y solo para `win-x64`: sin `-r win-x64` la carpeta pesa ~500 MB por traer los binarios nativos de todos los sistemas. Trae `web.config` (módulo `AspNetCoreModuleV2`, `inprocess`, `.\Contingencia.Web.exe`), `appsettings.json` (sin secretos: la site key de Turnstile es pública) y **no** trae `appsettings.Development.json`. No incluye `App_Data` ni las carpetas de datos.

#### Publicación en IIS (✅ 2026-10-14): `https://formatosct.imperium.net.co`

Hecha por el usuario en el servidor Windows (IIS 10), siguiendo el Paso 6 con estos datos reales:
- **Sitio** `Formatosct` (`C:\inetpub\wwwroot\Formatosct`, grupo de aplicaciones `Formatosct`, binding `*:80` para el subdominio y `*:83` en localhost; el HTTPS lo termina el balanceador de AWS). El servidor **comparte IIS con otros sitios** (`Imperium`, `Agenda`, `Incapacidades`).
- **Runtimes**: el servidor solo tenía ASP.NET Core 8.0.8 (en 32 bits); se instaló **solo el "ASP.NET Core Runtime 10.0.13 x64"** (no el Hosting Bundle, para **no reiniciar IIS** ni afectar a los otros sitios). El módulo `AspNetCoreModuleV2` ya existente (de un Hosting Bundle anterior) alojó la aplicación en .NET 10 sin problemas, así que el Hosting Bundle no hizo falta. WebSocket Protocol ya estaba instalado.
- **Grupo `Formatosct`**: `enable32BitAppOnWin64 = False`, `managedRuntimeVersion` vacío ("Sin código administrado").
- **Carpetas de datos**: el servidor **no tiene unidad D:**; se usó `C:\DatosContingencia\Llaves` y `C:\DatosContingencia\Archivos` (permiso Modificar a `IIS AppPool\Formatosct`); permiso de lectura a la carpeta del sitio. Respaldo previo del sitio en `C:\inetpub\Formatosct_respaldo`.
- **Paquete**: `Despliegue/Contingencia.Web-produccion.zip` descomprimido en la carpeta del sitio; el SHA-256 del zip copiado coincidió con el original.
- **Variables**: `Configurar-VariablesIIS.ps1 -Sitio 'Formatosct' -CarpetaDatos 'C:\DatosContingencia' …` terminó con las 11 variables en `ok` (primera ejecución real de la escritura con `appcmd`: funciona).

**Comprobado desde fuera (balanceador incluido, 2026-10-14)**: `/salud` → 200 `{"estado":"ok"}` (la app llega a `FormatosCT` con el usuario `contingencia_app`); `/` → 302 a `/login`; `/login` → 200 con la site key de Turnstile y todos sus recursos (`login-v2.css/js`, `login.js`, `blazor.web.js`) en 200; `/opciones` → 200; `/inicio`, `/juntas` y `/api/juntas/acta` sin sesión → 302 al login con `ReturnUrl` y esquema **https** (la aplicación reconoce el HTTPS del balanceador, es decir, `ASPNETCORE_FORWARDEDHEADERS_ENABLED` funciona y no hay bucle de redirección).

**Por revisar**: la cookie anti-falsificación del login salió sin el atributo `Secure` en la respuesta (solo `samesite=strict; httponly`); confirmar en el navegador, tras iniciar sesión, que las cookies de sesión (`ImperiumBridge` y la de autenticación) sí son `Secure`. Falta la prueba de inicio de sesión real (captcha de Turnstile con el hostname del subdominio) con `cwhospital` y `cwjuntas`.

#### CIA del hospital sembrada en la base de pruebas (✅ 2026-10-14, a pedido del usuario, para probar en local)

Los scripts `Sql/Produccion/03`, `04`, `04b` y `06` se aplicaron a **`DQUALITYREC`** (172.25.2.15,1435, la base que usan los proyectos locales; ver `appsettings.Development.json`). Son idempotentes: lo de Sanitas ya existía y se omitió (siguen 2 clientes y 71 roles); se agregó solo lo de **800006850**: 2 filas de cliente (CTC y TUTELA), rol 601, módulo 492, su relación y el usuario **`cwhospital`** con una **clave de prueba** (distinta de la de producción; se pasó por `-v CLAVE_CWHOSPITAL` y no queda escrita en ningún archivo; la conoce el usuario por la conversación). Los parámetros por CIA (`MANUAL` y `PRESTADORES_NIT`) ya estaban de `011`. `sp_DQS_ConsultaUsuario` devuelve la fila del hospital con el nombre, el logo y el rol 601.
- **No se aplicó** `05_Semilla_Tokens_SISPRO_800006850.sql` (credenciales reales de SISPRO del hospital): en local la validación de la prescripción al gestionar un formato de esa CIA no funcionará (no hay token); crear contingencias y consultar sí.
- **No se aplicó** `02b` (el filtro delgado reemplazaría el multiplexor que usa el legado).
- Esto **corrige** lo anotado antes ("no se insertaron en las bases de pruebas"); `DQUALITY` (45.169.253.168) y `FormatosCT` no se tocaron en este paso.
- **Deshacer**: `DELETE` de las filas de `CIA = '800006850'` en `DQS_REC_USUARIOS`, `DQS_REC_ROLMODULO`, `DQS_REC_MODULOS`, `DQS_REC_ROLES` y `DQS_REC_CLIENTES`.

#### El hospital gestiona sus profesionales sin tener Juntas (✅ código y base 2026-10-14, a pedido del usuario: "opción 2")

**Por qué**: el paso 3 del wizard "Nueva contingencia" (Profesional tratante) solo permite elegir entre los profesionales ya registrados de la CIA (`DQS_MIP_PROFESIONALES`, sin texto libre y sin botón de crear; el único "+ Crear" del wizard es el de causales). Esos profesionales se crean en `/profesionales`, que exigía el módulo "Junta Profesionales", y el hospital (800006850) no lo tiene a propósito. Resultado: el hospital no podía completar ninguna contingencia. Estado de los datos al detectarlo: `DQUALITYREC` 1 profesional (Sanitas); `FormatosCT` 0 (los profesionales no se copiaron).

**Qué se hizo** (acceso solo a esa pantalla, sin Juntas):
- Módulo nuevo **"Profesionales" (ID 494)** en `DQS_REC_MODULOS`, asignado en `DQS_REC_ROLMODULO` al rol 601 **solo de la CIA 800006850**. Se usó el 494 porque el 493 ya existe en otra CIA con otro significado ("Covid"). Está en `Sql/Produccion/04_Semilla_Seguridad.sql` (idempotente) y se aplicó el 2026-10-14 a **`DQUALITYREC`** y a **`FormatosCT`**. Comprobado con `sp_DQS_ConsultaModxRol` (CIA, luego rol): hospital/601 → Contingencia + Profesionales; Sanitas/601 → Contingencia (sin cambios); Sanitas/60 → Junta Profesionales (sin cambios).
- Política nueva **`ModuloProfesionales`** (`ImperiumPolicies`, `Program.cs`): exige el módulo "Profesionales" **o** el de Juntas (Juntas conserva su acceso). `Profesionales.razor` pasó de la política `ModuloJuntas` a esta; `/juntas` y los endpoints `/api/juntas/*` siguen exigiendo solo Juntas, así que el hospital sigue sin entrar a Juntas.
- `AppLayout.razor`: el enlace **Profesionales** aparece con cualquiera de los dos módulos; el de **Junta Profesional** solo con Juntas.
- Compila en Release sin errores ni advertencias.

**Efecto a tener en cuenta**: `sp_DQS_MIP_ProfesionalesSincronizarUsuario` crea por cada profesional un **usuario espejo** en `DQS_REC_USUARIOS` copiando el rol, el token y el `ID_EMPRESA` del usuario que lo crea y mostrando una clave generada una sola vez. Si `cwhospital` (rol 601) crea un profesional, ese usuario nuevo es de la CIA 800006850 con rol 601 (puede iniciar sesión y, por ser 601, también ve Formatos y Profesionales). Es la vía para dar más usuarios al hospital, por persona. Las firmas que se suben van a `Almacenamiento:RutaBase` (`C:\DatosContingencia\Archivos` en producción).

**Publicado en producción (2026-10-14, tras validar en local)**: el usuario detuvo el grupo `Formatosct`, descomprimió `Contingencia.Web-produccion.zip` (SHA-256 `5308394911…B38F`) sobre el sitio y lo arrancó. Verificado desde fuera después: `/salud` 200 `{"estado":"ok"}`, `/` → 302 `/login`, `/login` y `/opciones` 200, y `/inicio`, `/juntas`, `/profesionales` y `/api/juntas/acta` sin sesión → 302 al login. Eso no distingue la versión nueva de la anterior; la prueba definitiva es iniciar sesión con `cwhospital` y ver "Profesionales" en el menú.

**Pendiente**: probar en el navegador con `cwhospital` (local y subdominio): ver "Profesionales" en el menú, crear un profesional y elegirlo en el paso 3; `/juntas` debe seguir devolviéndolo a `/inicio`. Publicar el paquete nuevo en IIS (la base ya está lista). Deshacer: borrar de `DQS_REC_ROLMODULO` y `DQS_REC_MODULOS` las filas con `IDMODULO`/`ID_MODULO` = '494' de la CIA 800006850.

#### Actualización del servidor: compilado del 2026-10-21

**Alineación local ↔ GitHub (2026-10-21, tras `git fetch`)**: `main` local = `origin/main` = `6493440` (repositorio `D4t42025/formatosct`), 0 commits de diferencia en ambos sentidos, árbol de trabajo limpio y sin *stash*. El compilado salió de ese commit exacto (consta en `VERSION.txt` dentro del paquete).

**Qué trae respecto de lo publicado el 2026-10-14**: los commits `5edfeb7` a `6493440` (Juntas: PDF y archivos en Azure, migración de archivos del legado; Formatos y Nueva Contingencia de la CIA 890330031 en Azure; botones "Eliminar" que borran también en la base; iconos y diseño del panel).

**Paquete**: `Despliegue/Contingencia.Web-produccion-20261021.zip` (42,9 MB, win-x64, dependiente del marco, sin `appsettings.Development.json`; el del 2026-10-14 queda al lado como respaldo). SHA-256 `DB557F0AA9BA5AD378B25D729A2012B8412F66EA21F0D6AB90507D3D690B962C`. Compilado con `dotnet publish Contingencia.Web -c Release -r win-x64 --self-contained false`. Prueba de humo local en modo Producción **sin** variables de Azure: `/` → 302 a `/login`, `/login` y `/opciones` 200, `/inicio`, `/juntas` y `/profesionales` sin sesión → 302 al login, `/salud` 503 (base inexistente a propósito); arranca sin error.

**Requisito NUEVO en el servidor (el publicado el 10-14 no lo tiene)**: la variable de entorno **`Almacenamiento__Azure__ConnectionString`** (cadena de conexión de la cuenta de Azure Storage, secreta). Juntas guarda y lee sus archivos **siempre** en Azure Blob (contenedor `juntas` por defecto, `Almacenamiento__Azure__Contenedor`); Formatos y Nueva Contingencia lo hacen para las CIA de `Almacenamiento:Azure:CiasFormatosAzure`/`CiasContingenciasAzure` (por defecto solo **890330031**), contenedor `formatosct` (`Almacenamiento__Azure__ContenedorFormatos`). El hospital **800006850** y cualquier otra CIA siguen en disco (`C:\DatosContingencia\Archivos`). Sin la cadena, fallan abrir cargues y actas en `/juntas` y subir o ver formatos y contingencias de Sanitas (el error sale al usarlas, no al arrancar). El servidor además necesita salida HTTPS (443) a `*.blob.core.windows.net`. Para definirla sin volver a escribir las demás variables: `Despliegue/Configurar-AzureIIS.ps1` (nuevo, mismo estilo que `Configurar-VariablesIIS.ps1`: secreto por parámetro, `-SoloMostrar`, rechaza cadenas sin `AccountName=`/`AccountKey=` o con comillas; simulación probada en Windows PowerShell 5.1, **la escritura real con `appcmd` no se probó** porque aquí no hay IIS). La clave de la cuenta no pasa por el agente ni por este documento.

**Base de datos: no hay que cambiar nada para este compilado.** Los dos procedimientos nuevos (`sp_MIP_EliminaFormatoCnt` y `sp_MIP_EliminaContingencia`, scripts `014` y `015`) ya están en `FormatosCT` (aplicados el 2026-10-18 12:45) y de los 32 procedimientos que nombra el código no falta ninguno en esa base. **Hueco de documentación/paquete**: `Sql/Produccion/02_ProcedimientosAlmacenados.sql` aún **no incluye** esos dos, así que una base nueva creada desde cero con el paquete los perdería; pendiente agregarlos.

**Pasos de actualización en el servidor** (ya se hizo así el 2026-10-14): copiar el zip a `C:\Temp` y comprobar el SHA-256 → `Stop-WebAppPool Formatosct` (esperar `Stopped`) → respaldo `Copy-Item C:\inetpub\wwwroot\Formatosct C:\inetpub\Formatosct_v2 -Recurse` → `Expand-Archive … -DestinationPath C:\inetpub\wwwroot\Formatosct -Force` → `Configurar-AzureIIS.ps1 -Sitio 'Formatosct' -AzureConnectionString '<cadena>'` → `Start-WebAppPool Formatosct` → `Invoke-WebRequest http://localhost:83/salud`. Volver atrás: detener el grupo, restaurar la copia y arrancar. **No probado**: el acceso real a Azure desde el servidor y la publicación en IIS (se verifica después desde fuera y con un usuario).

**Publicado (2026-10-21)**: el usuario siguió esos pasos en el servidor: `Configurar-AzureIIS.ps1` terminó con las 3 variables en `ok` (la escritura real con `appcmd` sí funciona), arrancó el grupo `Formatosct` y `http://localhost:83/salud` respondió 200 `{"estado":"ok"}`. Verificado desde fuera por el balanceador: `/salud` 200, `/login` y `/opciones` 200 (con la site key de Turnstile), `/` → 302 a `/login`, y `/inicio`, `/juntas`, `/profesionales`, `/api/juntas/acta`, `/api/juntas/cargue` y `/api/formatos/archivo` sin sesión → 302 al login con esquema https. **Falta comprobar con usuarios reales que Azure responde desde el servidor**: `cwjuntas` abre un cargue o el acta en `/juntas`; `cwformatos` (Sanitas) sube y abre un formato; `cwhospital` (en disco) sigue funcionando.

### Paso 1 (✅): esquema de la base nueva

Carpeta `Contingencia.Infrastructure/Sql/Produccion/`, generada por script desde el catálogo de `DQUALITYREC` (no se tocó ninguna base):

- `01_Tablas.sql` — **24 tablas** (sin `DQS_LOG_CONSULTAFILTROS`, que solo usaba el envoltorio del procedimiento de filtros original), idempotente (`IF OBJECT_ID(...) IS NULL CREATE TABLE`), con tipos, `IDENTITY`, defaults, claves primarias e índices. No hay claves foráneas entre ellas. Tablas: `DQS_REC_USUARIOS`, `DQS_REC_ROLES`, `DQS_REC_ROLMODULO`, `DQS_REC_MODULOS`, `DQS_REC_CLIENTES`, `DQS_REC_IPS`, `DQS_REC_NOTIFICACIONES`, `DQS_OPT_LOGUEO`, `ImperiumSesionPuente`, `DQS_MIP_FORMATOS`, `DQS_MIP_COD_HAB`, `DQS_MIP_TOKEN`, `DQS_MIP_PROFESIONALES`, `DQS_MIP_PROFESIONALESACTA`, `DQS_MIP_JUNTASC`, `DQS_MIP_CRUZAR`, `DQS_MIP_CONTINGENCIAS`, `DQS_MIP_CONTINGENCIAS_TEC`, `DQS_MIP_CONT_PARAMETRICAS`, `DQS_MIP_AFILIADOS_TOKEN`, `DQS_MIP_DCI`, `DQS_MIP_CODIGOS`, `DQS_REC_CARGUECIE10`, `DQS_MIP_CIA_PARAMETROS`.
- `02_ProcedimientosAlmacenados.sql` — **34 SP** (31 + `sp_MIP_ConsultaCiaParametros`, `sp_MIP_CrearUsuarioFormatos` y `sp_MIP_CambiarClaveUsuario`) con `CREATE OR ALTER PROCEDURE` (las 35 de la app menos `sp_DQS_ConsultaFiltros` y `sp_DQS_ConsultaFiltros_Core`, que se resuelven con la versión delgada de `02b_ProcedimientoFiltrosDelgado.sql`, ver "Paso 3" más abajo).
- **Validación (2026-10-14)**: todos los lotes de los scripts pasan el análisis sintáctico de SQL Server (`SET PARSEONLY ON`, sin errores); el cierre de dependencias de las 31 SP (`sys.sql_expression_dependencies` + búsqueda de nombres en el texto) queda dentro de las 24 tablas y las 34 SP, sin SQL dinámico y sin referencias a otras bases.
- Orden de ejecución completo en la base nueva (ver "Paso 3"): `01` → `02` → `02b` → `03` → `04` → `04b` → `05` → `06`, y luego los catálogos del paso 4. Los scripts `001`–`013` anteriores NO se ejecutan en la base nueva: su resultado ya está incluido en `01`/`02`.
- Revisado en el paso 3: `sp_DQS_ConsultaUsuario` (copia de producción sin el `UPDATE` de la clave fija de `cwjuntas`; el resto igual, depende de `DQS_REC_CLIENTES`/`DQS_REC_IPS`) y `sp_DQS_ActualizaPwd` (depende de `DQS_REC_NOTIFICACIONES`).

### Paso 2 (⏳ en curso): decisión 1 — multi-CIA (2026-10-14)

Respuesta del usuario: la base nueva debe ser **multi-CIA**; arranca con **890330031** (EPS Sanitas) y **800006850**.

Hallazgos (consultados en `DQUALITYREC` y `45.169.253.168/DQUALITY`):

- **La app ya es multi-CIA en lo esencial**: la CIA sale siempre de la sesión y 16 de las 24 tablas tienen columna `CIA` (usuarios, roles, módulos, rol-módulo, clientes, notificaciones, bitácora, puente de sesión, formatos, token SISPRO, profesionales, actas, juntas, contingencias y sus tecnologías, códigos). Los SP filtran por CIA.
- **La CIA 800006850 no existe en ninguna de las dos bases** (0 filas en usuarios, roles, clientes, token y formatos): hay que crearla desde cero (cliente, roles, módulos, usuarios, token SISPRO). Según el catálogo de prestadores (`DQS_MIP_COD_HAB`, columna `Extra_II_NroIDPrestador`) ese NIT aparece en centros de salud de Soacha/Granada/Sibaté, es decir, parece una **IPS/ESE (prestador), no una EPS** — por confirmar con el usuario.
- **No son por CIA (catálogos compartidos, correcto así)**: `DQS_MIP_COD_HAB`, `DQS_MIP_CRUZAR`, `DQS_MIP_DCI`, `DQS_REC_CARGUECIE10`, `DQS_MIP_CONT_PARAMETRICAS`, `DQS_REC_IPS`. `DQS_MIP_CODIGOS` sí tiene `CIA` pero todo está bajo `'NEPS'` y el código filtra ese valor fijo (sirve a todas las CIA).
- **Puntos que NO sirven tal cual para dos CIA** (a resolver en el paso 5): `DQS_MIP_AFILIADOS_TOKEN` y el servicio de afiliados son únicos y de Sanitas (sin `CIA`); el menú usa un único `Menu:Perfil` global; el logo y el texto "EPS Sanitas" están fijos en el layout, el login y el wizard (la marca por CIA ya existe en `DQS_REC_CLIENTES.CLIENTE`/`LOGO` y en `ImperiumSesionPuente.Logo`/`NombreCliente`); los destinos por rol (601 Formatos, 60 Juntas, 29 gestión) asumen los mismos ID de rol en todas las CIA.

### Paso 2 — respuestas del usuario (2026-10-14) y parametrización de afiliados por CIA (✅ implementado)

Respuestas: **800006850 es una IPS y usa solo Formatos** (sin Juntas); **890330031 (Sanitas) usa el web service de afiliados de Sanitas**; **800006850 no tiene web service**, así que en el paso 4 "Paciente" debe habilitarse un **formulario para digitar los datos del paciente**, y eso debe quedar **parametrizado por CIA** para cualquier CIA que no cuente con web service.

**Base de datos** (`Sql/011_MipCiaParametros.sql`, aplicado el 2026-10-14 en `DQUALITYREC` y en `DQUALITY` 45.169.253.168):

- Tabla `dbo.DQS_MIP_CIA_PARAMETROS` (`CIA`, `CLAVE`, `VALOR`, `DESCRIPCION`, `ACTIVO`, `FECHAACTUALIZACION`; PK `CIA`+`CLAVE`) — genérica, para otras opciones por CIA a futuro.
- SP `sp_MIP_ConsultaCiaParametros(@Cia)` — `CLAVE`/`VALOR` activos de la CIA.
- Claves: `AFILIADOS_ORIGEN` = `WS` (web service de afiliados) o `MANUAL` (formulario); `AFILIADOS_ENTIDAD` = nombre de la EPS que se muestra en el panel del paciente (solo modo WS). **Una CIA sin fila `AFILIADOS_ORIGEN` usa `MANUAL`**: nunca llama al web service de otra entidad.
- Semilla: `890330031` → `WS` + `AFILIADOS_ENTIDAD='EPS Sanitas'`; `800006850` → `MANUAL`. Se administra por SQL, p. ej. `UPDATE DQS_MIP_CIA_PARAMETROS SET VALOR='WS' WHERE CIA='…' AND CLAVE='AFILIADOS_ORIGEN'` (no hay pantalla). Para dar de alta una CIA nueva sin web service no hace falta insertar nada (el valor por defecto ya es manual).
- `DQS_MIP_AFILIADOS_TOKEN` y el web service de afiliados siguen siendo únicos (de Sanitas): solo se usan en las CIA con `WS`.

**Aplicación**:

- `Infrastructure/CiaParametros/` (`ICiaParametrosRepository`, `CiaParametrosRepository`) y `Application/CiaParametros/` (`ICiaParametrosService`, `CiaParametrosService`, `ParametrosCia`, `OrigenAfiliados`), registrados en `Program.cs`.
- `NuevaContingencia.razor`, paso 4: `OnInitializedAsync` lee los parámetros de la CIA de la sesión. Modo `WS`: igual que antes (documento + "Buscar"), y la etiqueta "EPS" ahora sale de `AFILIADOS_ENTIDAD` en vez de estar fija como "EPS Sanitas". Modo `MANUAL`: sin botón "Buscar"; formulario con tipo y número de documento, nombres*, apellidos*, sexo*, fecha de nacimiento* (no futura), régimen, municipio y teléfono, más fecha, hora y ámbito de atención. Validación en `ValidarPacienteManual()`; el dictado por voz del paso 4 suma nombres, apellidos, municipio y teléfono en este modo. Los datos viajan por los mismos campos de `SolicitudNuevaContingencia`, sin cambios en SP ni tablas de contingencias.

**Pruebas (2026-10-14)**: arnés de backend en ambos servidores — `890330031` = WS (EPS Sanitas), `800006850` = manual y una CIA inexistente = manual; en Chrome con el usuario de Formatos de Sanitas — modo WS (búsqueda real de un afiliado, panel con "EPS Sanitas" tomado del parámetro) y modo manual (cambiando temporalmente `AFILIADOS_ORIGEN` a `MANUAL` en la base de pruebas, ya restaurado a `WS`): el formulario aparece sin "Buscar", el formulario vacío muestra "Debe ingresar el número de documento del paciente." y, completo, avanza al paso 5. No se registró una contingencia manual de punta a punta desde la interfaz; el registro con estos mismos campos ya se probó con el arnés de backend.

**Cliente 800006850 confirmado (2026-10-14)**: **HOSPITAL MARIO GAITAN YANGUAS** (IPS, `TOKEN='IPS'` en `DQS_REC_CLIENTES`, `FECHALICENCIA` 2026-12-01, `MarcaP='2'`, logo `https://imperium.net.co/Img/DQS/800006850/Logo_HMGY.jpg`, dos filas `TIPO`=`CTC` y `TUTELA` como Sanitas). Sus filas de `DQS_REC_CLIENTES` están en `Sql/Produccion/03_Semilla_Clientes.sql` (junto con las 2 de Sanitas; idempotente, validado por sintaxis) — **no se insertaron en las bases de pruebas/168**. La app ya toma nombre y logo del cliente desde esos datos (claim `LogoEmpresa` en el layout), no del código.

**Paso 2 "IPS" del wizard restringido a las sedes de la IPS (✅)** — parámetro nuevo `PRESTADORES_NIT` en `DQS_MIP_CIA_PARAMETROS` (agregado a `011_MipCiaParametros.sql`, que es idempotente y se volvió a ejecutar en `DQUALITYREC` y en `45.169.253.168`): `800006850` → `800006850`; `890330031` sin fila (sin restricción). Si la CIA tiene el parámetro, `FormatosService.BuscarPrestadoresAsync`/`ObtenerDetallePrestadorAsync` reciben el NIT y el SQL de `FormatosRepository` agrega `AND (@Nit IS NULL OR [Extra_II_NroIDPrestador] = @Nit)` (la búsqueda por nombre/código quedó entre paréntesis). Con NIT, el texto vacío lista todas las sedes (31 para 800006850, algunas con `Habilitado='NO'`, que el panel informativo ya muestra) y el detalle de un prestador ajeno se rechaza (null), por lo que no se puede elegir una IPS de otro NIT ni escribiéndola a mano. Solo se aplicó al wizard (paso 2); `CargarFormato`, `ConsultarFormato` y `ConsultarNuevaContingencia` siguen buscando en todo el catálogo (la firma de los métodos acepta el NIT opcional, pendiente de decidir si se aplica ahí). Pruebas con arnés en ambos servidores (10 verificaciones cada uno, todas OK): IPS busca "HOSPITAL" → 2 sedes del HMGY; lista 31 sedes con texto vacío; no ve "A SANCHEZ RADIOLOGOS" (de otro NIT); la EPS sigue viendo todo y sin buscar con texto vacío. No se probó en la interfaz porque aún no existe un usuario de la CIA 800006850 (roles, módulos y usuarios son el paso 3).

**Pendiente para 800006850**: NIT y token de SISPRO MIPRES (`DQS_MIP_TOKEN`), usuarios/roles/módulos de Formatos (paso 3) y decidir si `CargarFormato`/consultas también se limitan a sus sedes.

### Paso 3 (🔶 en curso) — acceso solo a Formatos para 800006850, tokens SISPRO y correo (2026-10-14)

Respuestas del usuario: (1) la CIA **800006850 solo accede a Formatos**, **Juntas no debe tener acceso**, y sus usuarios entran habitualmente con el **mismo rol que `cwformatos`** (rol 601); (2) entregó los tokens de SISPRO MIPRES de la CIA; (3) el **correo** para envíos es el que ya se usa.

**Acceso: se gobierna por módulo y ahora también se exige en el servidor.**
- Rol 601 "Formatos" → módulo 492 "Contingencia" (Formatos); rol 60 "Juntas" → módulo 49 "Junta Profesionales"; rol 29 "Supervisor EPS" → ambos. Para 800006850 solo se siembra el rol 601 con el módulo 492 (`Sql/Produccion/04_Semilla_Seguridad.sql`, junto con los roles/módulos de Sanitas que usa esta app: 29, 60, 601 y módulos 49, 492; idempotente, sintaxis validada). Sin rol 60 ni módulo 49 la CIA no ve Juntas ni Profesionales en el menú.
- **Hueco cerrado**: antes `/juntas` y `/profesionales` solo exigían sesión (`[Authorize]`), de modo que un usuario de Formatos que escribiera la URL entraba. Ahora la política `ModuloJuntas` (`Application/Auth/ImperiumPolicies.cs`: exige el módulo "Junta Profesionales" en el claim `imperium:modulo`) protege `Juntas.razor`, `Profesionales.razor` y los endpoints `/api/juntas/acta` y `/api/juntas/cargue`; el acceso denegado redirige a `/inicio` (`AccessDeniedPath`). Los usuarios de Juntas (rol 60) y supervisores (29) conservan el acceso porque sus roles incluyen el módulo 49. Limitación conocida: la política se evalúa al cargar la URL (y en los endpoints); una navegación interna dentro de un circuito ya abierto no se vuelve a evaluar, pero el enlace no existe en el menú de quien no tiene el módulo.
- Prueba (2026-10-14, Chrome, usuario `cwformatos`): `/juntas` y `/profesionales` redirigen a `/inicio`; `/api/juntas/acta` y `/api/juntas/cargue` redirigen en vez de responder; `/formatos/consultar` sigue respondiendo 200. Regresión con el usuario de Juntas de Sanitas (rol 60): `/juntas` y `/profesionales` abren con normalidad y `/api/juntas/acta` devuelve el PDF del acta (200, `application/pdf`).
- No se tocó el acceso inverso (un usuario de Juntas puede abrir páginas de Formatos escribiendo la URL); no fue solicitado.

**Tokens SISPRO de 800006850** — `Sql/Produccion/05_Semilla_Tokens_SISPRO_800006850.sql` (**contiene secretos**, no publicar; el repositorio no es git, pero tratar el archivo como credencial). Filas de `DQS_MIP_TOKEN` (`Cia`, `NIT`=800006850, `Token`, `Detalle`, `Regimen`): `APIPRESCRIPCION2` Contributivo y Subsidiado, `APIPRESCRIPCION` Contributivo y `APIDIRIPS` Contributivo (ojo: en Sanitas el equivalente se llama `APIDIREPS`; la app no lee ese detalle). La app usa `APIPRESCRIPCION` para validar la prescripción al gestionar un formato y `APIPRESCRIPCION2` en Juntas con token IPS. **Falta**: `APIPRESCRIPCION` Subsidiado (solo se entregó el Contributivo) — un afiliado subsidiado no podría validarse; confirmar con el usuario. No se insertaron en las bases de pruebas/168. Los tokens de Sanitas no se versionan: se copian de la base actual al cargar datos (paso 4).

**Correo**: se usa el mismo remitente SMTP que ya usa la app (`Smtp:Host`/`User`/`Password`). En producción esos tres valores deben ir como variables de entorno (`Smtp__Host`, `Smtp__User`, `Smtp__Password`), no en `appsettings`. Hoy el usuario y la clave de ese correo están en `appsettings.Development.json` (junto con la clave secreta de Turnstile de desarrollo): conviene sacarlos del archivo y rotar la clave.

**Pendiente**: usuarios de HMGY (en Sanitas cada IPS tiene un usuario con `USUARIO` = código de habilitación de la sede, `CEDULA` = NIT, `NOMBRES` = nombre, `ROL`=601, `ID_EMPRESA`=`NEPS_UT`; falta definir cómo se crean los de HMGY, su `ID_EMPRESA` y el manejo de la clave), el SP delgado de filtros 136/138 y el correo de producción en variables de entorno.

#### Paso 3 (continuación, 2026-10-14): token Subsidiado, usuario `cwhospital` y hallazgos de seguridad del login

**Token `APIPRESCRIPCION` Subsidiado de 800006850**: el usuario aclaró que **no es el mismo token** que el Contributivo, así que **no se reutiliza**. Queda sin fila en `DQS_MIP_TOKEN` hasta que lo entregue (mientras tanto un afiliado subsidiado no puede validarse al gestionar un formato).

**Usuarios de HMGY**: por ahora **uno solo, `cwhospital`** (rol 601, compartido como `cwformatos`); más adelante podrán pedirse más, por sede o por persona.

- `Sql/012_MipCrearUsuarioFormatos.sql` (aplicado el 2026-10-14 en `DQUALITYREC` y `45.169.253.168`; solo el procedimiento): `dbo.sp_MIP_CrearUsuarioFormatos(@Cia, @Usuario, @Cedula, @Nombres, @Apellidos, @Email, @Clave, @IdEmpresa, @Planta)`. Procedimiento de administración (no lo llama la app) para dar de alta usuarios de rol 601 en una CIA: valida que la CIA exista en `DQS_REC_CLIENTES` (`TIPO='CTC'`, sin esa fila el login no devuelve nada), que tenga el rol 601 en `DQS_REC_ROLES`, que el usuario no exista (`USUARIO` es la PK: único entre **todas** las CIA) y clave de ≥ 8 caracteres; inserta con `ROL='601'`, `ESTADO='Activo'`, `MARCA=0`, `PLANTA='NACIONAL'`, `TOKEN=''`, `ID_EMPRESA` = la CIA (solo se usa en Juntas), **`PWDTEMP=NULL` y `FechaPwd='2099-12-31'`** (ver hallazgo 3). Para crear otro usuario basta volver a llamarlo.
- `Sql/Produccion/06_Usuario_cwhospital.sql`: crea `cwhospital` (CIA 800006850, cédula/ID = NIT, nombre HOSPITAL MARIO GAITAN YANGUAS, sin correo) **sin guardar la clave en el archivo**: se pasa al ejecutar con `sqlcmd … -v CLAVE_CWHOSPITAL="<clave>" -i 06_Usuario_cwhospital.sql`; sin la variable sqlcmd aborta y no crea nada; si el usuario ya existe no lo toca. Va después de 01–05. Pruebas (ambas bases, todo en transacción con rollback): rechaza CIA inexistente, clave corta, usuario repetido y CIA sin sembrar; el alta válida deja la fila esperada y `sp_DQS_ConsultaUsuario` la devuelve; no quedó ningún usuario de prueba. El script 06 en seco contra pruebas: sin variable aborta; con variable y la CIA sin sembrar rechaza sin escribir.

**Hallazgos de seguridad del login heredado (resolver antes de producción)**:

1. **Claves en texto plano**: `DQS_REC_USUARIOS.CLAVE` guarda la clave tal cual y `sp_DQS_ConsultaUsuario` la compara con `CLAVE = @STRPWD` (diseño del legado, límite 50 caracteres por `@STRPWD VARCHAR(50)`). Pendiente decidir si se migra a hash (implica cambiar el SP de login, `sp_DQS_ActualizaPwd`, el alta de usuarios y la recuperación de clave).
2. **Clave fija de `cwjuntas`**: `sp_DQS_ConsultaUsuario` ejecuta en cada intento de login (de cualquier usuario) `UPDATE DQS_REC_USUARIOS SET CLAVE='juntas2023**' WHERE USUARIO='cwjuntas'`. **Se eliminó de la copia de `02_ProcedimientosAlmacenados.sql`** (queda un comentario). Sigue presente en el SP de las bases actuales (pruebas y 168), que no se modificaron porque lo comparte el legado.
3. **Cambio de clave no migrado**: el login manda a `ActContraseña.aspx` (página del legado, sin equivalente en esta app) cuando `PWDTEMP` = la clave ingresada o cuando `FechaPwd` es hoy. En una base nueva sin el legado eso es un callejón sin salida. Por eso `cwhospital` se crea con `PWDTEMP=NULL` y `FechaPwd='2099-12-31'`. Efectos que siguen vigentes: la recuperación de clave por correo (`sp_DQS_ActualizaPwd`, que fija `PWDTEMP`=clave) lleva al usuario a esa página; y los usuarios copiados de Sanitas tienen `FechaPwd='2026-12-31'` (ese día los mandará allí). Falta migrar una pantalla de "Cambiar contraseña" o neutralizar esas dos rutas.
4. **Licencia del cliente**: el login solo devuelve al usuario si `DQS_REC_CLIENTES.FECHALICENCIA` (fila `CTC`) es ≥ hoy; la de HMGY vence el **2026-12-01** y la de Sanitas figura al 2026-12-31. Renovar con `UPDATE DQS_REC_CLIENTES SET FECHALICENCIA = '…' WHERE CIA = '…'`.

#### Paso 3 (continuación, 2026-10-14): cambio de contraseña y diseño nuevo del login (✅ implementado)

Decisiones del usuario: (1) **`APIPRESCRIPCION` Subsidiado de 800006850 usa por ahora el mismo token que el Contributivo** (provisional; el real es distinto y lo entregará después — el script `05_Semilla_Tokens_SISPRO_800006850.sql` ya trae esa fila con el comentario y el `UPDATE` para reemplazarlo); (2) **migrar la pantalla de cambio de contraseña** y **llevar el diseño nuevo del login (ya hecho en el proyecto original) a Juntas/Formatos y a Recobros**; (3) **las claves siguen en texto plano, sin pasar a hash por ahora**.

**Base de datos** — `Sql/013_MipCambiarClaveUsuario.sql` (aplicado en `DQUALITYREC` y `45.169.253.168`): `sp_MIP_CambiarClaveUsuario(@Usuario, @ClaveActual, @ClaveNueva)`. A diferencia del legado (`ActContraseña.aspx` comparaba contra `Session("pwdini")` y llamaba `sp_DQS_ActualizaPwd2` sin verificar), **verifica en la base** que la clave actual sea correcta y el usuario esté activo; aplica los mismos efectos (`CLAVE`, `FECHA_ACT_PWD`, `PWDTEMP=''`, `FechaPwd` + 90 días, fila "Contraseña actualizada" en `DQS_REC_NOTIFICACIONES`) en una transacción. Diferencia deliberada: el legado sumaba 90 días a la `FechaPwd` anterior (si ya estaba vencida o era hoy, la nueva podía seguir vencida y el usuario quedaba atrapado); aquí se suma a MAX(`FechaPwd`, hoy), y una `FechaPwd` lejana (`2099-12-31`, la de `cwhospital`) se conserva + 90. Rechaza los usuarios compartidos `CIFormatosC`, `CMFormatosC` y `cwformatos`, como el legado. Pruebas en ambas bases (en transacción, con rollback): clave actual incorrecta, vacíos, usuario compartido e inexistente rechazados; el cambio válido deja `CLAVE` nueva, `PWDTEMP` vacío, `FechaPwd` = hoy + 90, notificación registrada, la clave anterior ya no sirve y el login con la nueva funciona. El legado también llamaba `sp_DQS_InsertarBitacora`, que **no existe** en estas bases (el `Try/Catch` lo ocultaba); no se migró (la trazabilidad queda en `DQS_REC_NOTIFICACIONES`).

**Aplicación**: `Infrastructure/Login/ILoginRepository.CambiarClaveAsync`; `Application/Login/CambioClaveService` con la política del original y sus mismos mensajes y orden (confirmación igual → distinta de la actual → 12–20 caracteres → minúscula → mayúscula → número → símbolo → sin `_TEMPORAL` → sin espacios); página **`/actualizar-contrasena`** (`ActualizarContrasena.razor`, `[Authorize]`, acepta `?msj=` como el original; al éxito muestra "Contraseña actualizada" y un botón "Continuar" a `/`). `LoginService.DeterminarDestino` ahora manda a esa ruta (en vez de a `ActContraseña.aspx` del legado) cuando la clave es temporal o `FechaPwd` es hoy. Se conserva la precedencia heredada: el rol 601 pisa el aviso de "clave temporal" y solo el vencimiento del día (`FechaPwd` = hoy) fuerza la pantalla. Pruebas con Chrome y la sesión del usuario de Juntas (sin cambiar su clave): política (clave corta), confirmación distinta y clave actual incorrecta muestran el mensaje esperado; la verificación contra la base funciona. **Prueba de punta a punta con sesión real (2026-10-14)**: con un usuario de prueba desechable (rol 601, CIA 890330031, `FechaPwd` = hoy) el usuario inició sesión en Chrome y cambió su clave; la base lo confirma: `CLAVE` distinta de la inicial, `PWDTEMP=''`, `FechaPwd` = hoy + 90 días (2027-01-12), `FECHA_ACT_PWD` actualizada y una fila "Contraseña actualizada" en `DQS_REC_NOTIFICACIONES`; después navegó a `/inicio`. Usuario y filas de prueba borrados. No se comprobó visualmente si el login lo mandó solo a la pantalla (el usuario la completó sin reportar el paso) ni el botón "Continuar"; en Recobros no se probó con sesión.

**Login con el diseño nuevo (login-v2)** — migrado de `Login3.aspx` (Contingencia) y `Login.aspx` (Recobros): `wwwroot/New_Login/login-v2.css` y `login-v2.js` son **copias** de los del proyecto original (regenerar desde allá, no editarlas aquí; se enlazan con `?v=20261015`), los ajustes propios van en `login-blazor.css`; el marco común (fondo animado, contacto, panel, WhatsApp) está en `Components/Layout/LoginLayout.razor` (idéntico en los dos proyectos); `Login.razor` y `ActualizarContrasena.razor` solo aportan su título y formularios. La lógica del servidor de `Login.razor` no cambió (captcha Turnstile en Contingencia y reCAPTCHA en Recobros dentro de `.captcha-slot`, recuperación de clave por correo, mensajes de error en el panel). `js/login.js` ya no maneja el cambio entre "Iniciar sesión" y "Recuperar contraseña" (lo hace `login-v2.js`; quedó solo el captcha y `alerta()`). Se conservó el "¿Olvidó su clave?" con la recuperación por correo (decisión anterior del usuario); el `Login3.aspx` original solo apunta a WhatsApp. Verificado en Chrome en ambos proyectos: el diseño se ve completo (el campo usuario necesitó `type="text"` explícito para tomar estilo), el captcha carga, la vista de recuperar alterna, el error del servidor se muestra y no hay errores de consola. La pantalla de cambio de contraseña se compactó (marcadores de posición cortos y reglas en 3 líneas) para que la tarjeta quepa en una ventana de ~800 px.

**Alcance en Recobros**: la página `/actualizar-contrasena` existe también allí, pero el login de Recobros no manda a ella (heredado: el chequeo de 2FA pisa siempre el aviso de clave temporal; ver el comentario en `LoginService.DeterminarDestino` de `Imperium.Application`), así que solo se llega escribiendo la ruta o desde un enlace futuro; no se cambió ese flujo.

### Paso 3 (✅ 2026-10-14): SP delgado de filtros y paquete de despliegue completo

**El problema**: `sp_DQS_ConsultaFiltros` (con su envoltorio de registro en `DQS_LOG_CONSULTAFILTROS`) delega en `sp_DQS_ConsultaFiltros_Core`, un multiplexor de más de 13.000 líneas con un `IF @INTVALIDA = N` por cada página de todo el sistema y ~110 tablas ajenas a esta app. Formatos/Juntas solo usan dos ramas, y las dos son un `SELECT` simple.

**La solución** — `Sql/Produccion/02b_ProcedimientoFiltrosDelgado.sql` crea `dbo.sp_DQS_ConsultaFiltros` con **el mismo nombre y los mismos 13 parámetros** (el código de la app no cambia) pero solo con:

| `@INTVALIDA` | Consulta (copiada tal cual del `_Core`) | La usan |
|---|---|---|
| 136 | `SELECT NIT, TOKEN FROM DQS_MIP_TOKEN WHERE Cia=@STRCIA AND Regimen=@STREGIMEN AND DETALLE=@STROPCION ORDER BY NIT` | `MipresPrescripcionValidator`, `JuntaProfesionalApiClient` |
| 138 | `SELECT CODIGO, DESCRIPCION, CAMPO FROM DQS_MIP_CRUZAR ORDER BY CAMPO, CODIGO` | `JuntasService` (acta PDF), `NuevaContingencia` (ámbito, forma, vía, dosis, frecuencia) |

Cualquier otro `@INTVALIDA` lanza un error explícito (`THROW 50001`) en vez de devolver vacío, para detectar al instante si una página nueva necesita otra rama. No registra llamadas, por lo que se quitó `DQS_LOG_CONSULTAFILTROS` del paquete (24 tablas). **No debe ejecutarse en `DQUALITYREC`/`DQUALITY`**: reemplazaría el multiplexor que usa el legado.

**Prueba de equivalencia** (en `DQUALITYREC`, creando el SP con otro nombre dentro de una transacción que se revierte): idéntico resultado al original en 36 combinaciones CIA × detalle × régimen para el 136 y en las 40.088 filas del 138 (mismo orden y columnas); `@INTVALIDA=17` falla con el mensaje esperado; no quedó nada.

**Paquete de despliegue completo** (`Sql/Produccion/`), en este orden, con la base destino seleccionada:

| Archivo | Qué hace |
|---|---|
| `00_CrearBaseDeDatos.sql` | crea la base con la intercalación **Modern_Spanish_CI_AS** (obligatoria, ver paso 4); el nombre llega como variable de sqlcmd `NOMBRE_BD` |
| `01_Tablas.sql` | 24 tablas |
| `02_ProcedimientosAlmacenados.sql` | 34 SP (incluye `sp_MIP_ConsultaCiaParametros`, `sp_MIP_CrearUsuarioFormatos`, `sp_MIP_CambiarClaveUsuario`; `sp_DQS_ConsultaUsuario` sin la clave fija de `cwjuntas`) |
| `02b_ProcedimientoFiltrosDelgado.sql` | `sp_DQS_ConsultaFiltros` delgado |
| `03_Semilla_Clientes.sql` | `DQS_REC_CLIENTES` de 890330031 y 800006850 |
| `04_Semilla_Seguridad.sql` | roles y módulos (800006850 solo Formatos: rol 601 + módulo 492) |
| `04b_Semilla_ParametrosCia.sql` | `DQS_MIP_CIA_PARAMETROS` (origen de afiliados, entidad, NIT de sedes) |
| `05_Semilla_Tokens_SISPRO_800006850.sql` | **secreto**: tokens SISPRO de 800006850 (el Subsidiado de `APIPRESCRIPCION` es provisional) |
| `06_Usuario_cwhospital.sql` | usuario `cwhospital`; la clave se pasa con `sqlcmd -v CLAVE_CWHOSPITAL="..."` |
| *(copia de datos con `CopiarCatalogos`, ver paso 4)* | catálogos, tokens de Sanitas y los usuarios `cwformatos` y `cwjuntas` |
| `07_Ajustes_UsuariosCopiados.sql` | tras la copia: quita el vencimiento de clave de `cwformatos` (compartido, no puede cambiarla) |

Ejemplo: `sqlcmd -S <servidor> -d <base> -U <usuario> -P <clave_sql> -C -f65001 -b -i 01_Tablas.sql` (repetir por archivo; el 06 lleva además `-v CLAVE_CWHOSPITAL="<clave>"`). Los tokens de Sanitas y los catálogos (`DQS_MIP_COD_HAB`, `DQS_MIP_CRUZAR`, `DQS_MIP_DCI`, `DQS_MIP_CODIGOS`, `DQS_REC_CARGUECIE10`, `DQS_MIP_CONT_PARAMETRICAS`) son el paso 4.

**Prueba de despliegue en una base vacía** (2026-10-14, SQL Server 2016 SP1 LocalDB, base temporal ya eliminada): los 8 scripts corrieron sin errores y dejaron 24 tablas, 35 procedimientos, 4 clientes, 4 roles, 5 rol-módulo, 4 parámetros y el usuario; con el código real de la aplicación apuntando a esa base: login de `cwhospital` (cliente "HOSPITAL MARIO GAITAN YANGUAS", logo, rol 601, sin clave temporal), clave incorrecta rechazada, **módulos del rol 601 = solo "Contingencia"** (sin "Junta Profesionales") y el rol 60 de Sanitas conserva el suyo, parámetros por CIA, filtro 136 (con un token falso) y 138, cargar y consultar un formato, registrar una contingencia con paciente manual y dos tecnologías, consulta aislada por CIA (otra CIA no la ve), PDF del formulario con catálogos vacíos y cambio de clave con login posterior. Sin fallos. Limitación: el 138 devuelve 0 filas hasta cargar los catálogos (paso 4) y no se probó el servidor web contra esa base (captcha), solo los servicios.

### Paso 4 (🔶 2026-10-14): herramienta de copia de catálogos y datos

**Herramienta**: `Herramientas/CopiarCatalogos` (programa .NET 10, multiplataforma, fuera de la solución web; `SqlBulkCopy` en streaming, sin archivos intermedios ni scripts de datos). Copia de una base **origen** (p. ej. `DQUALITYREC`) a la **base nueva** ya creada con `Sql/Produccion/00`–`06`:

```bash
# las cadenas pueden ir en variables de entorno para no dejar claves en el historial:
export CONTINGENCIA_ORIGEN="Server=...;Database=DQUALITYREC;User Id=...;Password=...;TrustServerCertificate=True"
export CONTINGENCIA_DESTINO="Server=...;Database=DQS_MIPRES;User Id=...;Password=...;TrustServerCertificate=True"
dotnet run -c Release --project Herramientas/CopiarCatalogos -- [opciones]
```

| Opción | Efecto |
|---|---|
| *(ninguna)* | copia los **catálogos**: `DQS_MIP_COD_HAB` (199.423 filas), `DQS_MIP_CRUZAR` (40.088), `DQS_MIP_DCI` (6.607), `DQS_MIP_CODIGOS` (206.557), `DQS_REC_CARGUECIE10` (12.634), `DQS_MIP_CONT_PARAMETRICAS` (7, conserva los `ID`) y `DQS_REC_IPS` (467) |
| `--tokens <cia[,cia]>` | `DQS_MIP_TOKEN` de esas CIA (credenciales SISPRO, **secretas**) y `DQS_MIP_AFILIADOS_TOKEN` (token del WS de afiliados de Sanitas, **secreto**). Para 890330031; los de 800006850 salen del script `05` |
| `--usuarios <cia[,cia]>` | `DQS_REC_USUARIOS` de esas CIA (Sanitas: 8.370, claves en texto plano, **secretas**) |
| `--historial <cia[,cia]>` | formatos, profesionales, actas, juntas y contingencias (con sus tecnologías) de esas CIA (Sanitas: 5.577 formatos, 145.281 registros de juntas…). Solo las filas: **los archivos en disco (`App_Data/mediauploader`) se copian aparte** |
| `--reemplazar` | si el destino ya tiene filas de esa tabla (o de esa CIA) las borra y copia de nuevo; sin esto la tabla se **omite** (la herramienta es repetible) |
| `--verificar` | no escribe nada: compara conteos origen/destino |

Cada tabla va en una transacción del destino: si algo falla o el conteo no coincide, esa tabla se revierte. Una tabla omitida porque el destino la tiene distinta del origen, o inexistente, cuenta como fallo y el programa termina con código de salida 1. Los tokens se copian directo entre bases: nunca pasan por archivos del repositorio ni se imprimen.

**Hallazgo — intercalación (collation)**: `DQUALITYREC` y `DQUALITY` usan `Modern_Spanish_CI_AS` en la base y en **todas** sus columnas de texto (266 y 265 columnas revisadas). Los scripts `01`–`06` no declaran `COLLATE` por columna: la heredan de la base, así que la base nueva **debe crearse con esa misma intercalación** (`Sql/Produccion/00_CrearBaseDeDatos.sql`). En la primera prueba la base temporal se creó con la del servidor local (inglés) y `SqlBulkCopy` rechazó las 16 tablas ("el id de configuración regional no coincide"); además de la copia, de la intercalación dependen las comparaciones y búsquedas de la app. La herramienta ahora compara las intercalaciones de las dos bases al conectar y se detiene con un mensaje claro si difieren.

**Corrección de cifras**: en el listado de pasos previo se citaron `DQS_MIP_COD_HAB` como ~399 mil filas (45 MB) y `DQS_REC_IPS` como 934; eran el doble por contar dos veces las particiones. Los valores reales son 199.423 y 467 (y `DQS_MIP_AFILIADOS_TOKEN` tiene 1 fila, no 3).

**Pruebas (2026-10-14)**: base vacía en LocalDB (SQL Server 2016) con intercalación `Modern_Spanish_CI_AS`, esquema `01`–`04b` y copia desde `DQUALITYREC` con `--tokens 890330031 --usuarios 890330031 --historial 890330031`: las 16 tablas copiadas sin fallos en ~56 s (el grueso: `DQS_MIP_COD_HAB` 30 s, `DQS_MIP_CODIGOS` 13 s, `DQS_MIP_JUNTASC` 5 s). Verificación de contenido: conteo y suma de `CHECKSUM(*)` por fila idénticos en las 14 tablas comparables, `DQS_REC_IPS` idéntica por conteo y huella de sus columnas de texto (su columna `Logo` es `image` y no admite `CHECKSUM`; en `DQUALITYREC` está vacía) y los `ID` de `DQS_MIP_CONT_PARAMETRICAS` conservados (7, 8, 9, 10, 11, 12, 15). Segunda ejecución: todo "omitida (el destino ya la tiene completa)". `--verificar`: todo "IGUAL". Con una fila borrada del destino: `--verificar` la marca DIFERENTE, la copia sin `--reemplazar` avisa que está INCOMPLETA (código de salida 1) y con `--reemplazar` se rehace y queda igual. Con una tabla ausente en el destino: error claro indicando ejecutar `01_Tablas.sql`. Base temporal eliminada.

**Decisiones del usuario (2026-10-14) y alcance de la primera carga**:
1. **Usuarios**: no se copian todos los de Sanitas; solo **`cwformatos` y `cwjuntas` de la CIA 890330031** (`--usuarios 890330031 --solo-usuarios cwformatos,cwjuntas`) y, para la otra CIA, **un usuario de Formatos: `cwhospital`** (CIA 800006850, script `06`; el nombre `cwformatos` no puede repetirse porque `USUARIO` es la clave primaria de `DQS_REC_USUARIOS`, único entre todas las CIA). Más usuarios se darán de alta después con `sp_MIP_CrearUsuarioFormatos`.
2. **Sin historial ni adjuntos**: no se usa `--historial` y `App_Data/mediauploader` empieza vacío (decisión del usuario: "puede iniciar en 0").
3. **Servidor de origen: `DQUALITY` (45.169.253.168)**, de donde se toman catálogos, tokens de Sanitas y esos dos usuarios.

**Comando de producción** (con la base nueva ya creada con `00` y desplegados `01`–`06`):

```bash
export CONTINGENCIA_ORIGEN="Server=45.169.253.168;Database=DQUALITY;User Id=...;Password=...;TrustServerCertificate=True"
export CONTINGENCIA_DESTINO="Server=<servidor>;Database=DQS_MIPRES;User Id=...;Password=...;TrustServerCertificate=True"
dotnet run -c Release --project Herramientas/CopiarCatalogos -- --tokens 890330031 --usuarios 890330031 --solo-usuarios cwformatos,cwjuntas
sqlcmd -S <servidor> -d DQS_MIPRES -U ... -P ... -C -f65001 -b -i Sql/Produccion/07_Ajustes_UsuariosCopiados.sql
dotnet run -c Release --project Herramientas/CopiarCatalogos -- --tokens 890330031 --usuarios 890330031 --solo-usuarios cwformatos,cwjuntas --verificar
```

**Ensayo con el origen real (2026-10-14)**: base vacía en LocalDB (`Modern_Spanish_CI_AS`), `00`–`06` + copia desde `DQUALITY` + `07`: 10 tablas copiadas sin fallos (catálogos: `DQS_MIP_COD_HAB` 199.423, `DQS_MIP_CRUZAR` 40.088, `DQS_MIP_DCI` 6.607, `DQS_MIP_CODIGOS` **208.096**, `DQS_REC_CARGUECIE10` 12.634, `DQS_MIP_CONT_PARAMETRICAS` **6**, `DQS_REC_IPS` 467; 4 tokens de Sanitas; token del WS de afiliados; 2 usuarios). Con el código real de la app contra esa base: `cwformatos` y `cwjuntas` inician sesión con la clave copiada (CIA 890330031, "EPS SANITAS", rol 601 con solo el módulo "Contingencia" y rol 60 con solo "Junta Profesionales"); la base queda con exactamente 3 usuarios (esos dos + `cwhospital`); los filtros 136 devuelven el NIT 800251440 y el token de Sanitas para Contributivo y Subsidiado; el token del WS de afiliados está presente; el 138 trae las 40.088 filas con los campos del wizard; la IPS 800006850 ve sus 31 sedes y no las de otros NIT, y Sanitas ve todos; parámetros por CIA correctos. Sin fallos; base de ensayo eliminada.

**Diferencias entre `DQUALITYREC` y `DQUALITY` (168)**: `DQS_MIP_CODIGOS` tiene 208.096 filas en la 168 frente a 206.557 en pruebas (la 168 está más al día); `DQS_MIP_CONT_PARAMETRICAS` tiene 6 causales en la 168 y 7 en pruebas (la séptima, "Por resolución", se creó en pruebas); el resto coincide en conteo. Al copiar desde la 168 la base nueva queda con las 6 causales de la 168 (las causales nuevas se pueden crear desde el botón "+ Crear" del wizard).

**Hallazgos de seguridad de esos dos usuarios (resolver al desplegar)**:
- **`cwjuntas` conserva la clave fija `juntas2023**`** del legado (el `sp_DQS_ConsultaUsuario` original se la reasigna en cada login y está escrita en el código de ese procedimiento). La copia de producción de ese SP ya no la reasigna, pero la clave queda conocida: **cambiarla justo después de la copia**, p. ej. `EXEC dbo.sp_MIP_CambiarClaveUsuario @Usuario='cwjuntas', @ClaveActual='<la actual>', @ClaveNueva='<nueva de 12–20 caracteres>'` (o desde la pantalla "Actualizar contraseña" entrando como `cwjuntas`; su vencimiento del 2026-12-31 también lo llevará a esa pantalla).
- **`cwformatos` no puede cambiar su clave** (compartido): `sp_MIP_CambiarClaveUsuario` lo rechaza como el legado. Su `FechaPwd` de origen es `2026-12-31`; ese día el login lo habría mandado a la pantalla de cambio y no podría completarla, por eso `07_Ajustes_UsuariosCopiados.sql` le pone `2099-12-31`. Si hace falta rotar su clave, se hace por SQL directo.


### Paso 5 (✅ 2026-10-14): valores fijos de Sanitas en el código

Revisión de todo lo que estaba escrito en el código para una sola entidad y qué se hizo con cada cosa:

| Qué estaba fijo | Qué se hizo |
|---|---|
| Catálogo de códigos MIPRES: `CodigosRepository` filtraba `DQS_MIP_CODIGOS` por `CIA='NEPS'` fijo | Nuevo parámetro por CIA **`CODIGOS_CATALOGO`** (sin fila = `'NEPS'`, mismo comportamiento de hoy). `ICodigosRepository`/`ICodigosService.BuscarAsync` reciben el catálogo y `NuevaContingencia` lo toma de `ParametrosCia.CodigosCatalogo` |
| URL del web service de afiliados (`imperiumgitsanitas.net:51044`) en `AfiliadoApiClient` | Nuevo parámetro por CIA **`AFILIADOS_WS_URL`** (sin fila = la de Sanitas; en blanco también). Solo importa para CIA en modo `WS`. El token (`DQS_MIP_AFILIADOS_TOKEN`) sigue siendo único: una segunda entidad con web service necesitará su propio token (no existe aún esa tabla por CIA) |
| Texto "EPS Sanitas" en el panel del paciente | Ya hecho en el paso 2: sale de `AFILIADOS_ENTIDAD` |
| Logo y nombre del cliente | Ya salían de datos (`DQS_REC_CLIENTES` → claims `LogoEmpresa` y `NombreCliente`); solo hizo falta sembrar el cliente (script `03`) |
| Menú de entrada: todos los perfiles apuntaban a pantallas del sistema anterior (`Login.aspx`, `Login2.aspx`, `Login7.aspx`…) o a sitios de Sanitas | Nuevo perfil **`FORMATOS_JUNTAS`** en `MenuOpcionesService` con solo lo migrado: "Formatos de Contingencia" y "Gestor Juntas" (ambos a `/login`; el rol decide si entra a Formatos o a Juntas) y "Help Soporte". Para producción: `Menu__Perfil=FORMATOS_JUNTAS` (el `appsettings.json` de desarrollo sigue en `SANITASMIPRES`) |
| Logo del encabezado del acta de Junta: `ActaPdfBuilder` descargaba `https://imperium.net.co/img/minsalud.jpg` en **cada** acta y, si no había internet o ese sitio caía, el acta entera fallaba | `ActaPdfBuilder` ahora recibe la configuración: **`Acta:UrlLogo`** puede ser una URL o la ruta de un archivo local (sin internet); se obtiene una sola vez por proceso y, si no se puede, el acta se genera igual con el recuadro del logo vacío |

**Convenciones que se dejan como están (no son un error, pero hay que respetarlas al crear CIA nuevas)**:
- **IDs de rol**: el login decide el destino por el ID de rol (`601` → `/inicio` Formatos, `60` → `/juntas`, `27`/`29`/`30` → "Consultar formato" con el ícono de gestión, `35` → consulta web MIPRES) y el menú por módulo (`492` Contingencia, `49` Junta Profesionales). Los roles y módulos son filas por CIA, así que toda CIA nueva debe sembrarse con **los mismos IDs** (como hace `04_Semilla_Seguridad.sql`).
- **Servicios de gobierno**: las URL de SISPRO/MIPRES (`wsmipres.sispro.gov.co`) y de Cloudflare Turnstile son las mismas para todas las CIA.
- **Datos de contacto del login** (teléfono, correo, dirección, WhatsApp) y la política de datos son de Imperium (el proveedor), no de un cliente.
- **Imágenes de las plantillas de correo** (`dqualitysolutions.com.co`): siguen apuntando a ese sitio; si el servidor de correo/receptores no tienen salida a ese sitio, los mensajes salen sin logo (no bloquea el envío).

**Pruebas (2026-10-14)**: compila en Release sin errores. Con un arnés: parámetros por CIA (sin filas → manual, `NEPS`, URL nula; con filas → los valores recortados; en blanco → defectos); catálogo de códigos (`NEPS` devuelve 98 procedimientos para el prefijo `8902`, un catálogo inexistente devuelve vacío, texto vacío no consulta); URL del WS con un servidor HTTP simulado (sin URL, o en blanco → la de Sanitas; con URL de la CIA → esa); acta con logo desde archivo local (imagen incluida), con URL inalcanzable y con ruta inexistente (el acta se genera igual, sin imagen); perfil `FORMATOS_JUNTAS` (3 tarjetas, dos a `/login`) y perfiles anteriores intactos (`SANITASMIPRES` 8, `IPS` 7). En Chrome con una sesión de Sanitas: wizard hasta el paso 4 con la búsqueda real de afiliado (URL por defecto, panel "EPS Sanitas") y la búsqueda de códigos del paso 7 (Procedimiento) devolviendo resultados del catálogo `NEPS`. No se probó el acta de Junta de punta a punta en pantalla ni un segundo web service de afiliados (no existe).

### Paso 6 (✅ 2026-10-14): despliegue en producción — Windows / IIS

El usuario confirmó que el despliegue es en **Windows**, que el **subdominio ya está creado** y que **solo falta la base de datos**.

**A. Base de datos — un solo comando** (`Despliegue/Desplegar-BaseDeDatos.ps1`, Windows PowerShell 5.1 o PowerShell 7; requiere `sqlcmd` y, para la copia, el SDK de .NET o el programa publicado `CopiarCatalogos`):

```powershell
$env:SQLCMDPASSWORD = '<clave del usuario SQL>'      # asi no queda en la linea de comandos; o use -AutenticacionWindows
.\Despliegue\Desplegar-BaseDeDatos.ps1 -Servidor '<servidor SQL>' -Base 'DQS_MIPRES' -Usuario '<usuario SQL>' `
    -OrigenCadena 'Server=45.169.253.168;Database=DQUALITY;User Id=...;Password=...;TrustServerCertificate=True' `
    -ClaveCwhospital '<clave de cwhospital, minimo 8>' -NuevaClaveCwjuntas '<12 a 20 caracteres con mayuscula, minuscula, numero y simbolo>'
```

Hace, en este orden y deteniéndose en el primer error: crea la base (con `Modern_Spanish_CI_AS` y comprueba que quedó así), `01`, `02`, `02b`, semillas `03`, `04`, `04b`, tokens `05`, usuario `cwhospital` `06`, copia desde `DQUALITY` (catálogos, tokens de Sanitas, `cwformatos` y `cwjuntas`), `07`, **cambia la clave fija de `cwjuntas`** por `-NuevaClaveCwjuntas` (si no se indica, avisa que sigue la conocida) y una verificación final (conteos origen/destino y 24 tablas / 3 usuarios). Es repetible: lo ya copiado se omite. Con `-SoloVerificar` solo compara. Opciones: `-SaltarCopia`, `-ExeCopia <ruta a CopiarCatalogos.exe>` (si el servidor no tiene el SDK: `dotnet publish Herramientas/CopiarCatalogos -c Release -r win-x64 --self-contained false -o <carpeta>`).

*Prueba (2026-10-14)*: contra una base vacía de LocalDB con Windows PowerShell 5.1 y `DQUALITY` (168) como origen: las 8 etapas sin errores (24 tablas, 35 procedimientos, 3 usuarios, 2 clientes; las 10 tablas copiadas con conteos iguales), `cwjuntas` ya no tiene la clave fija y entra con la nueva, segunda ejecución omite todo y `-SoloVerificar` da "sin fallos". Errores que se corrigieron en la prueba: un `if` de PowerShell devolvía `-E` como texto y al expandirlo `sqlcmd` recibía `-` y `E` por separado; y una función no puede llamarse `Sqlcmd` (PowerShell no distingue mayúsculas y taparía el programa real).

**B. Cambios en la aplicación para producción**
- **Carpeta de archivos configurable** (`Almacenamiento:RutaBase`, `Infrastructure/Almacenamiento/RutaAlmacenamiento.cs`): por defecto sigue siendo `<app>\App_Data`; en producción apunta a una carpeta fuera de la de publicación (p. ej. `D:\DatosContingencia`) para que publicar de nuevo no borre ni pise los archivos. Dentro mantiene `mediauploader\formatos|contingencias|Files-Gestion-Usuarios\Firmas...`; las rutas guardadas en base de datos son relativas a esa raíz. La usan los 5 servicios que leen o escriben archivos.
- **Llaves de DataProtection persistentes** (`DataProtection:RutaLlaves`): sin esto cada reinicio o reciclaje del sitio cierra todas las sesiones. En Windows las llaves se cifran con DPAPI (a nivel de equipo). Verificado: se crea el archivo `key-*.xml` cifrado.
- **`GET /salud`** (anónimo): 200 `{"estado":"ok"}` si la aplicación llega a la base y 503 si no (sin detalles del error). Es la prueba de humo del despliegue.
- **`appsettings.Development.json` ya no se publica** (`CopyToPublishDirectory="Never"` en `Contingencia.Web.csproj`): hasta ahora viajaba en la publicación con la cadena de conexión de pruebas, la clave del correo y la clave secreta de Turnstile de desarrollo. Verificado: la publicación solo trae `appsettings.json`.
- La publicación (`dotnet publish -c Release`) genera un `web.config` con `AspNetCoreModuleV2` en modo `inprocess` y no incluye `App_Data`.

**C. Variables de entorno de producción** (todas por entorno, nunca en archivos del repositorio):

| Variable | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DQUALITY` | cadena de la base nueva (con un usuario SQL de mínimo privilegio, no `sa`) |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | **`true`** — obligatoria detrás del balanceador de AWS (ver "Subdominio" abajo): la aplicación lee `X-Forwarded-Proto`/`X-Forwarded-For` y sabe que la petición original fue HTTPS |
| `ASPNETCORE_HTTPS_PORT` | **No definirla** con el balanceador: él ya redirige 80→443 y, si se define sin la variable anterior, la aplicación redirige cada petición a HTTPS y entra en **bucle**. Solo se usa si se publica sin balanceador (entonces `443`: `http://…` responde 307 a `https://…`) |
| `DataProtection__RutaLlaves` | carpeta de llaves, p. ej. `D:\DatosContingencia\Llaves` |
| `Almacenamiento__RutaBase` | carpeta de archivos, p. ej. `D:\DatosContingencia\Archivos` |
| `Menu__Perfil` | `FORMATOS_JUNTAS` |
| `Turnstile__SiteKey` / `Turnstile__SecretKey` | las del widget de Cloudflare creado **para el subdominio** (las del `appsettings.json` son de desarrollo) |
| `Smtp__Host` / `Smtp__User` / `Smtp__Password` | el correo de envíos que ya se usa |
| `Acta__UrlLogo` | opcional: URL o ruta local del logo del acta (si el servidor no sale a internet) |

**D. Lista de comprobación en el servidor Windows (IIS)** — *documentada, no ejecutada en un IIS real*:
1. Característica de Windows **WebSocket Protocol** de IIS (Blazor Server la necesita) y el **.NET Hosting Bundle** de la versión de la aplicación (net10.0); luego `iisreset`.
2. Certificado TLS del subdominio en el *binding* HTTPS del sitio.
3. Grupo de aplicaciones: **Sin código administrado**, canalización integrada.
4. Crear las dos carpetas de la tabla C y dar permiso **Modificar** a `IIS AppPool\<nombre del grupo>`; el grupo de aplicaciones también necesita **leer** la carpeta del sitio.
5. Definir las variables de la tabla C (incluida `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`) en el sitio (p. ej. `appcmd set config "<sitio>" -section:system.webServer/aspNetCore /+"environmentVariables.[name='NOMBRE',value='VALOR']" /commit:apphost`; así quedan en `applicationHost.config` en texto claro, accesible solo a administradores del servidor).
6. Publicar: `dotnet publish Contingencia.Web -c Release -o <carpeta del sitio>` y reciclar el grupo.
7. Abrir `https://<subdominio>/salud` (200), `/login` y entrar con `cwhospital`; con `cwjuntas` comprobar que ve Juntas y con `cwhospital` que `/juntas` lo devuelve a `/inicio`.

**E. Prueba de la aplicación publicada en modo Producción** (2026-10-14, Kestrel en vez de IIS, contra la base desplegada en LocalDB): `/salud` 200 con la base y **503 con una base inexistente**; `/login` y `/` 200 (el menú muestra las 3 tarjetas de `FORMATOS_JUNTAS`); `/inicio`, `/juntas` y `/profesionales` sin sesión 302 al login; con `ASPNETCORE_HTTPS_PORT=443` las rutas responden 307 a `https://`; llave de DataProtection creada y cifrada con DPAPI en la carpeta indicada. **No probado**: IIS real (módulo ASP.NET Core, permisos del grupo de aplicaciones, WebSockets, certificado), la subida de archivos a `Almacenamiento:RutaBase` desde la pantalla, el envío de correo ni el inicio de sesión con captcha real del subdominio (requiere las claves de Turnstile del subdominio).

**Pendiente del usuario**: crear el widget de Turnstile del subdominio (o indicar quién), el nombre del servidor SQL y la cadena de conexión de producción, y las claves del correo para las variables; el nombre del subdominio no se necesita para el código.

### Base de producción: `FormatosCT` en 172.25.2.13 (confirmada por el usuario y desplegada, 2026-10-14)

El usuario pegó una cadena de conexión (comentada, de un `web.config` antiguo) que apunta a la base **`FormatosCT`** en el servidor **172.25.2.13** y confirmó que **esa es la base productiva de Formatos y Juntas**. Consulta previa de solo lectura, sin modificar nada:

- Servidor `EC2AMAZ-IMPERIU` (instancia EC2), **SQL Server 2017 Enterprise (14.0.3475)**, intercalación del servidor **`Modern_Spanish_CI_AS`** (la misma que las bases actuales); la cuenta usada es **sysadmin**. Otras bases del servidor: `BKINSERCOOP`, `DQUALITY` (otra, de 2024), `PowerFitDB`, `Prototipo`, `Soporte`.
- **`FormatosCT` fue creada el 2026-10-14**, con intercalación `Modern_Spanish_CI_AS`, nivel de compatibilidad 140, modelo de recuperación FULL, y está **vacía** (0 tablas, 0 procedimientos): lista para `Despliegue/Desplegar-BaseDeDatos.ps1 -Servidor 172.25.2.13 -Base FormatosCT`. SQL Server 2017 soporta todo lo que usan los scripts (`CREATE OR ALTER`, `THROW`, `TRY_CONVERT`, `OPENJSON`).
- La cadena pegada traía `Max Pool Size=900000` y `Connect Timeout=900000`: no se replican (para la aplicación basta algo como `Max Pool Size=100;Connect Timeout=30`).
- **Seguridad**: esa cuenta (`admin`) es sysadmin y su clave estaba en texto claro en un `web.config`; la aplicación **no** debe conectarse con ella. Se propone un inicio de sesión propio de la aplicación solo con lectura, escritura y ejecución sobre `FormatosCT`. La clave de `admin` conviene rotarla (quedó escrita también en la conversación).
- Aviso: en este servidor `DQUALITY` (2024) es una base distinta de la `DQUALITY` de 45.169.253.168 usada como origen de los catálogos.

#### Despliegue de la base de producción (✅ 2026-10-14)

Se ejecutó `Despliegue/Desplegar-BaseDeDatos.ps1 -Servidor 172.25.2.13 -Base FormatosCT -Usuario admin` con `DQUALITY` (45.169.253.168) como origen. Las 8 etapas terminaron sin errores y la verificación final dio "IGUAL" en las 10 tablas copiadas.

**Contenido de `FormatosCT`** (comprobado con consultas de solo lectura después del despliegue): 24 tablas, 35 procedimientos, intercalación `Modern_Spanish_CI_AS`.
- **Clientes**: 890330031 EPS SANITAS (licencia hasta 2026-12-31) y 800006850 HOSPITAL MARIO GAITAN YANGUAS (licencia hasta **2026-12-01**), filas `CTC` y `TUTELA` de cada una.
- **Roles y módulos**: 890330031 → 601 Formatos (Contingencia), 60 Juntas (Junta Profesionales), 29 Supervisor EPS (ambos); 800006850 → solo 601 Formatos (Contingencia), sin Juntas.
- **Parámetros por CIA**: 890330031 `AFILIADOS_ORIGEN=WS` y `AFILIADOS_ENTIDAD=EPS Sanitas`; 800006850 `AFILIADOS_ORIGEN=MANUAL` y `PRESTADORES_NIT=800006850`.
- **Tokens SISPRO**: 4 de Sanitas (NIT 800251440) y 5 de 800006850 (incluido el `APIPRESCRIPCION` Subsidiado provisional, igual al Contributivo).
- **Usuarios (3)**: `cwformatos` y `cwjuntas` de 890330031 y `cwhospital` de 800006850. `cwformatos` con `FechaPwd` 2099-12-31; **`cwjuntas` ya no tiene la clave fija del sistema anterior** (se le asignó una clave aleatoria nueva, entregada al usuario por la conversación, y su `FechaPwd` quedó en 2027-03-31); `cwhospital` con una clave aleatoria nueva. Las claves **no** quedan escritas en archivos ni en esta documentación; ambos usuarios pueden cambiarlas en `/actualizar-contrasena` (`cwformatos`, por ser compartido, no).
- **Catálogos**: `DQS_MIP_COD_HAB` 199.423, `DQS_MIP_CRUZAR` 40.088, `DQS_MIP_DCI` 6.607, `DQS_MIP_CODIGOS` 208.096, `DQS_REC_CARGUECIE10` 12.634, `DQS_MIP_CONT_PARAMETRICAS` 6, `DQS_REC_IPS` 467; token del WS de afiliados de Sanitas copiado.
- `sp_DQS_ConsultaFiltros` delgado: 136 devuelve el token de 800006850 y 138 las 40.088 filas del cruce.

**Prueba de la aplicación contra esta base** (solo lectura): la aplicación publicada en modo Producción con la cadena de `FormatosCT` respondió `/salud` 200 `{"estado":"ok"}` y `/` 200; no se creó ningún dato de prueba en producción (las pruebas de escritura se hicieron en bases temporales).

**Usuario SQL de la aplicación (✅ creado el 2026-10-14 en 172.25.2.13: `contingencia_app`)** — `Despliegue/Crear-UsuarioAplicacion.sql`: crea un inicio de sesión de mínimo privilegio (`db_datareader`, `db_datawriter`, `EXECUTE` y `VIEW DEFINITION` sobre `dbo`) para que la aplicación **no** use la cuenta `admin` (sysadmin). Se verificó la combinación de permisos en una base temporal de LocalDB suplantando un usuario sin inicio de sesión con los mismos permisos: puede derivar los parámetros de un procedimiento, ejecutar el login, los filtros, insertar y consultar un formato y escribir/borrar en una tabla; **no** puede crear, borrar ni alterar tablas ni crear usuarios. Uso: `sqlcmd -S 172.25.2.13 -U <admin> -C -f65001 -b -v NOMBRE_BD="FormatosCT" USUARIO_APP="contingencia_app" CLAVE_APP="<clave>" -i Despliegue\Crear-UsuarioAplicacion.sql`.

**Corrección de `Desplegar-BaseDeDatos.ps1`** durante esta prueba: con `-SaltarCopia` la verificación final intentaba comparar con un origen inexistente; ahora la omite.

**Ejecución en producción (2026-10-14)**: `Crear-UsuarioAplicacion.sql` se ejecutó con la cuenta `admin` contra `FormatosCT`. Creó el inicio de sesión `contingencia_app` (clave aleatoria de 24 caracteres, política de contraseñas de Windows activa, sin vencimiento; **no está escrita en ningún archivo**: se entregó al usuario por la conversación y debe guardarse solo en la variable `ConnectionStrings__DQUALITY` del servidor), el usuario de la base con `db_datareader`, `db_datawriter`, `EXECUTE` y `VIEW DEFINITION` sobre `dbo`, y su base por defecto `FormatosCT`. Comprobado conectándose **como ese usuario** (no `admin`): `SUSER_NAME()` = `contingencia_app`, no es sysadmin, lee las tablas, puede derivar los parámetros de los procedimientos, ejecuta `sp_DQS_ConsultaFiltros`/`sp_MIP_ConsultaCiaParametros` y **no puede crear tablas**. Cadena para la aplicación: `Server=172.25.2.13;Database=FormatosCT;User Id=contingencia_app;Password=<clave>;TrustServerCertificate=True;Connect Timeout=30;Max Pool Size=100`. Si se necesita cambiar su clave: `ALTER LOGIN [contingencia_app] WITH PASSWORD = '<nueva>'` y actualizar la variable de entorno. En una primera ejecución falló solo el paso de pasar las variables a `sqlcmd` (la clave contenía un `=`), sin crear nada; la clave definitiva solo usa letras, números y `# ! @ * + -`.

**Pendiente**: publicar en IIS con las variables de entorno; claves de Turnstile del subdominio y del correo; **respaldos** de `FormatosCT` (la base está en modelo de recuperación FULL: programar copias completas y de registro de transacciones); renovar la licencia de 800006850 antes del **2026-12-01** (`UPDATE DQS_REC_CLIENTES SET FECHALICENCIA ...`); y rotar la clave de la cuenta `admin`.

## Verificación de cobertura y compatibilidad (2026-09-02)

Con las 5 páginas del flujo de rol 601 completas, se hizo una pasada final
de control de calidad transversal (no específica de una sola página):

- **Cobertura SP/tablas re-confirmada por script automático**: se extrajeron
  por `grep` todos los `sp_*` invocados desde C# (7: `sp_DQS_
  ActualizaMarcaUsuario`, `sp_DQS_ActualizaPwd`, `sp_DQS_ConsultaModxRol`,
  `sp_DQS_ConsultaUsuario`, `sp_MIP_ConsultaFormatosCNT`, `sp_MIP_
  InsertaFormatoCnt`, `sp_opt_ingresalogueo`) y todas las tablas referenciadas
  en SQL embebido (`DQS_MIP_COD_HAB`, `dbo.ImperiumSesionPuente`) — **las dos
  listas coinciden exactamente** con las tablas de `## Tablas` y
  `## Procedimientos almacenados` de abajo, 0 objetos sin documentar.
- **Restyle de "Cargar formato" y "Consultar formato"**: ambas páginas usaban
  clases de Bootstrap (`form-control`, `btn btn-info`, `card`, `row g-3`,
  etc.) que **nunca tuvieron efecto** — Bootstrap solo se carga en
  `Login.razor` (vía su propio `<HeadContent>`, para esa página únicamente);
  `AppLayout` no lo carga globalmente, así que toda página bajo ese layout
  se veía con controles de navegador sin estilo. Corregido reemplazando esas
  clases por un sistema propio (`wwwroot/css/formatos.css`, cargado
  globalmente desde `App.razor`) que replica el mismo lenguaje visual que ya
  usa Recobros en sus propias páginas internas (`Imperium.Web/Components/
  Pages/Indicadores.razor.css`: tarjetas blancas redondeadas con sombra
  suave, azul `#2b6ff2` en degradado para el botón primario, azul claro para
  el secundario, tabla con encabezado azul) — mismo criterio de "no usar
  Bootstrap donde el resto de la app migrada no lo usa", en vez de arreglar
  el síntoma cargando Bootstrap solo para estas dos páginas.
- **Compatibilidad con Linux auditada** (motivada por el objetivo declarado
  de que toda la migración eventualmente corra fuera de Windows/SQL Server):
  - Los 4 proyectos apuntan a `net10.0` puro (ningún `net10.0-windows`), sin
    `UseWindowsForms`/`UseWPF`.
  - Sin API's Windows-only en el código (`System.Drawing`, `Microsoft.Win32`,
    `Registry`, `System.DirectoryServices`) — cero coincidencias.
  - `ClosedXML 0.105.1` (exportación a Excel, compartido con Recobros) **no
    depende de `System.Drawing.Common`** — sus dependencias reales son
    `SixLabors.Fonts` (multiplataforma, trae su propia fuente embebida) y
    `DocumentFormat.OpenXml`; confirmado leyendo el `.nuspec` del paquete
    instalado, no asumido.
  - Todas las rutas de archivo se arman con `Path.Combine`/`Path.GetFileName`
    (nunca concatenación con `\` literal) — `FormatosArchivoService`
    incluido. Sin fugas de mayúsculas/minúsculas: se comparó cada `src=`/
    `href=` de `.razor`/`.css` contra el nombre real del archivo en
    `wwwroot` (case-sensitive, como exige Linux) — las 50 referencias
    estáticas de la app coinciden exactamente.
  - `Microsoft.Data.SqlClient` con `TrustServerCertificate=True` (ya usado
    así en ambas apps) no depende de certificados del almacén de Windows.
  - **No se encontró ningún impedimento de código para correr en Linux** —
    dicho esto, esta auditoría fue estática (lectura de código y
    dependencias); no se hizo un `dotnet publish -r linux-x64` real ni se
    corrió el binario resultante en un contenedor/VM Linux, así que la
    verificación en tiempo de ejecución (arranque real de Kestrel en Linux,
    conectividad a SQL Server desde ese SO) queda pendiente de una prueba
    real si se decide desplegar ahí.
- **Verificación de punta a punta a nivel HTTP** (sin usar la herramienta de
  navegador — instrucción vigente del usuario): se corrió `dotnet run` local
  y se probó con `curl`: `/` y `/login` responden `200` sin trazas de
  excepción; `/inicio`, `/formatos/cargar`, `/formatos/consultar` y
  `/api/formatos/archivo` sin sesión responden `302` hacia `/login` (cookie
  de autenticación funcionando); todos los assets estáticos nuevos
  (`css/formatos.css`, `js/dialogos.js`, `js/descargas.js`, imágenes)
  resuelven en `200`.
- **Flujo autenticado completo probado por script (2026-09-02, a pedido del
  usuario)**: se activó temporalmente `Turnstile:Skip=true` en
  `appsettings.Development.json` — bypass que ya existía en
  `TurnstileValidator.EsValidoAsync`, doblemente protegido (solo aplica si
  `IHostEnvironment.IsDevelopment()` **y** el flag están ambos activos, así
  que nunca se activa en producción por accidente) — y se ejecutó con
  `curl` el login real de `cwformatos` end-to-end: `POST /login` con usuario/
  clave reales → `302` a `/inicio` con cookie `ImperiumBridge` → `GET
  /inicio` consume el token puente y setea `ContingenciaWebAuth`, redirige
  de nuevo a `/inicio` → `200` con el layout completo ("Hola, FORMATOS
  UNIVERSAL", "EPS SANITAS", sidebar "Formato contingencia", las 2
  tarjetas). Con esa sesión real también se probó `GET /api/formatos/
  archivo`: con un radicado real de `cwformatos` pero sin el archivo físico
  en este equipo (nunca se copió del servidor original) dio `404` como se
  espera; se creó un archivo de prueba en la ruta exacta que calcula
  `FormatosArchivoService.RutaFisica` y ahí sí lo sirvió en `200` con el
  contenido correcto (confirma que el chequeo de pertenencia + existencia
  de archivo funciona en ambos sentidos) — archivo de prueba eliminado
  después. `POST /cerrar-sesion` con esa misma sesión devolvió `MARCA` a
  `0` en `DQS_REC_USUARIOS` e invalidó la sesión (`/inicio` volvió a dar
  `302`). Al terminar, `Turnstile:Skip` se quitó de nuevo del archivo
  (vuelve a exigirse el captcha real).
- **Flujo interactivo (clic a clic) probado con Claude in Chrome (2026-09-02,
  a pedido del usuario, "pero con google chrome in claude no puedes
  validar?")**: a diferencia del panel de navegador sandboxeado que causaba
  los cierres de sesión reportados antes (`mcp__Claude_Browser__*`, en desuso
  desde entonces), esta vía usa el Chrome real del usuario vía la extensión
  "Claude in Chrome" — mecanismo distinto, sin problemas de estabilidad en
  esta sesión. Se hizo login real con captcha real (sin bypass), se navegó
  a `/formatos/consultar`, se buscó por un radicado real de `cwformatos`
  (mismo resultado ya verificado por SP), se exportó a Excel (el archivo
  llegó de verdad a la carpeta de Descargas de Windows, columnas y datos
  verificados leyendo el `.xlsx` directamente — sin la columna `Estado`,
  como se espera) y se probó "Cargar formato".
- **Bug real encontrado y corregido en el propio flujo de prueba**:
  `Tran(msg, mod)` en `wwwroot/js/dialogos.js` ejecutaba
  `window.location = mod` en el callback de confirmación **sin importar si
  `mod` venía vacío** — con el `<base href="/">` que exige Blazor,
  `window.location = ""` no es un no-op (como sí lo sería sin `<base>`):
  resuelve a `/` y expulsa al usuario del formulario a la pantalla de
  Opciones, **perdiendo todo lo que había escrito**, cada vez que aparecía
  un mensaje de validación simple (`Tran(resultado.Mensaje, "")`, usado en
  ambas páginas de Formatos). Confirmado en vivo con Claude in Chrome antes
  del fix (clic en "Gracias" → navegaba a `/`) y después (clic en "Gracias"
  → se queda en la misma página). La corrección tiene dos partes: (1) solo
  navegar si `mod` trae un valor real, y (2) llamar a `swal.close()` cuando
  no navega — necesario porque el diálogo se abre con `closeOnConfirm:
  false`, así que antes del fix el modal **siempre** se cerraba como efecto
  colateral de la navegación forzada; al quitar esa navegación, sin el
  `swal.close()` explícito el modal se quedaba pegado en pantalla (también
  confirmado en vivo, mismo ciclo de prueba). Este bug no se detectó en la
  verificación previa por `curl`/`pyodbc` porque esa vía nunca ejecuta
  JavaScript — solo apareció al probar el circuito interactivo real.
  Re-verificado en ambas páginas (Cargar y Consultar formato) tras el fix.
  **Sigue sin probarse por script/curl** (y no tiene sentido intentarlo ahí):
  la lógica de negocio detrás de los botones ya estaba verificada contra la
  base de pruebas por separado; lo que sí solo se puede confirmar con un
  navegador real es la ejecución de JavaScript como este bug, que es
  precisamente lo que esta ronda con Claude in Chrome permitió atrapar.
- **SweetAlert migrado de v1 a v2 (2026-09-02, a pedido explícito del
  usuario: "usa la version 2")**: la app traía copiados de Recobros dos
  archivos, `assets/js/sweetalert.min.js` (v1, el proyecto original
  `t4t5/sweetalert`, ya deprecado — identificado por su tamaño minificado,
  16977 bytes, y por su API `swal({...}, callback)`, sin número de versión
  embebido) y `assets/js/sweetalert2.min.js` (v2, versión **11.26.25**, sí
  trae el número embebido) — pero `App.razor` solo cargaba el v1; el v2
  estaba copiado pero sin usar. Se migró: `App.razor` ahora carga
  `sweetalert2.min.js` (que además inyecta su propio `<style>` en tiempo de
  ejecución — ya no hace falta `assets/css/sweetalert.css`, que solo
  aplicaba al v1 y se deja sin tocar por no ser necesario borrarlo), y se
  reescribieron TODAS las funciones que llamaban al `swal()` global de v1
  (`window.swal`) a la API de v2 (`window.Swal.fire({...}).then(...)`):
  `Tran`/`Tran2`/`TranT` en `wwwroot/js/dialogos.js` (las únicas realmente
  usadas hoy, por Cargar/Consultar formato), `alerta()` en
  `wwwroot/js/login.js` (código sin invocar actualmente, migrado por
  consistencia), y `Tran`/`Alerta`/`Alerta2`/`Tran2`/`Enviar` en
  `wwwroot/js/opciones.js` (copiado tal cual de Recobros; solo `Alerta` está
  cableada desde `Opciones.razor`, vía `JS.InvokeVoidAsync("Alerta", ...)`
  para el mensaje "Usuario no corresponde al aplicativo escogido"). De paso,
  v2 simplifica el bug de `Tran` recién corregido: al confirmar, v2 cierra
  el modal solo (a diferencia de v1 con `closeOnConfirm:false`, que exigía
  el `swal.close()` manual agregado en el fix anterior) — se mantuvo, eso
  sí, la regla de "solo navegar si `mod` trae algo". **Verificado en vivo
  con Claude in Chrome**: los diálogos de validación en Cargar/Consultar
  formato siguen mostrando el mismo mensaje y cerrando correctamente sin
  expulsar del formulario.
- **Bug real encontrado y corregido: "Ver documento" descargaba un blob
  genérico sin extensión** (reportado por el usuario con una captura real:
  el navegador mostraba una descarga completada llamada "archivo", 354 KB,
  en vez de abrir el PDF). Causa confirmada: el endpoint `GET /api/
  formatos/archivo` llamaba `Results.File(ruta)` sin especificar
  `contentType` — sin eso, ASP.NET Core sirve `application/octet-stream`
  (confirmado antes en esta misma sesión vía `curl`, sin que se hubiera
  conectado ese dato con el síntoma hasta que el usuario lo reportó desde
  la UI), y un navegador fuerza la descarga de cualquier contenido
  `octet-stream` en vez de renderizarlo inline, tomando como nombre el
  último segmento de la URL ("archivo", de `/api/formatos/archivo`) porque
  tampoco se envía `Content-Disposition`. Corregido calculando el
  content-type real a partir de la extensión (`.pdf`→`application/pdf`,
  `.png`, `.jpg`/`.jpeg`, `.tiff`/`.tif`, `.doc`, `.docx`, `.xlsx`, con
  `application/octet-stream` como último recurso) y pasándolo a
  `Results.File(ruta, contentType)` — deliberadamente **sin**
  `fileDownloadName`, porque eso forzaría `Content-Disposition: attachment`
  y rompería la intención real de "Ver" (abrir el PDF en la pestaña, no
  descargarlo). **Verificado en vivo con Claude in Chrome** contra un
  archivo real: el radicado `2026090200000000005574` que el propio usuario
  había cargado antes desde su Visual Studio (mismo `App_Data` físico en
  disco, compartido entre su sesión de VS en el puerto 7277 y este servidor
  de prueba en el 5154) — tras el fix, "Ver" abre el PDF real directamente
  en el visor nativo de Chrome con su título real, ya no descarga nada.

### Re-auditoría de compatibilidad con Linux (2026-09-03, tras Cargas/Firma)

Repetida la misma auditoría estática de la sección anterior sobre todo lo
agregado en la ronda de Junta Profesional (consulta+PDF+Cargas+Firma), no
solo el código de esta última ronda:

- **Sin APIs Windows-only**: `grep` de `System.Drawing`, `Microsoft.Win32`,
  `System.DirectoryServices`, `Registry.`, `WindowsIdentity` sobre los 4
  proyectos — cero coincidencias.
- **Sin rutas con separador hardcodeado**: todos los servicios de archivos
  nuevos (`JuntasArchivoService`, igual que `FormatosArchivoService`) arman
  rutas exclusivamente con `Path.Combine`/`Path.GetFileName`/
  `Path.GetExtension` — cero concatenaciones con `\` o `/` literales.
  `IWebHostEnvironment.ContentRootPath` (no `Server.MapPath`, que no existe
  en ASP.NET Core) sigue siendo la base, igual que Formatos.
- **Paquetes NuGet nuevos, sin dependencias nativas**: `itext7`/
  `itext7.bouncy-castle-adapter`/`BouncyCastle.Cryptography` (ver "Página
  8") son 100% administrados (`AnyCPU`), sin P/Invoke ni componentes
  nativos por plataforma — confirmado revisando `dotnet list package
  --include-transitive`.
- **Sin `net10.0-windows`/`UseWindowsForms`/`UseWPF`** en ningún `.csproj`
  de la solución (se repitió la misma búsqueda que en la auditoría
  anterior, sigue en cero).
- **Sensibilidad a mayúsculas de nombres de archivo cargados por el
  usuario** (relevante porque Linux sí distingue mayúsculas y Windows no):
  el nombre de archivo que `IJuntasArchivoService` usa para construir la
  URL "Ver" (`ArchivoCargue.Url`) sale del mismo `FileInfo.Name` que
  devuelve `DirectoryInfo.GetFiles()` al listar — nunca se recalcula ni se
  compara por fuera con otra fuente, así que no hay forma de que el
  nombre usado para verlo no coincida exactamente con el nombre real en
  disco, sea cual sea el sistema operativo.
- **No se hizo** (igual que en la auditoría anterior) un `dotnet publish
  -r linux-x64` real ni se corrió el binario en un contenedor/VM Linux —
  esta sigue siendo una auditoría estática de código y dependencias, no
  una prueba de ejecución real en ese sistema operativo.

### Validación final (2026-09-03, a pedido del usuario: "valida que todo lo migrado de juntas y formatos esté en el md y que todo sea compatible en Linux")

Pasada de cierre sobre **toda la solución** (Formatos + Juntas juntos, no
solo lo agregado en la última ronda):

- **Cobertura re-confirmada por script automático**: los 15 `sp_*`
  invocados desde C# (extraídos por `grep` de los 61 archivos `.cs` de la
  solución) coinciden exactamente, uno a uno, con las 15 filas de
  `## Procedimientos almacenados`; las tablas referenciadas en SQL directo
  embebido (`DQS_MIP_COD_HAB`, `DQS_MIP_FORMATOS`,
  `dbo.ImperiumSesionPuente`) también están documentadas — 0 objetos
  usados sin documentar.
- **Dos inconsistencias numéricas encontradas y corregidas en este mismo
  documento** (arrastradas de ediciones incrementales anteriores, no
  afectaban al código): el encabezado de "Módulos/páginas migrados" decía
  "(8...)" cuando la tabla de abajo siempre tuvo 7 filas; "Tablas tocadas"
  decía "(14)" cuando la lista nombra 13 tablas distintas (`DQS_REC_
  ROLMODULO`/`ROLES`/`MODULOS` cuentan como 3 en la fila 7, no como 1) —
  ambos corregidos a los números reales (7 y 13).
- **Referencias a assets estáticos, verificadas una por una contra el
  disco (sensible a mayúsculas, como exige Linux)**: se extrajeron las 29
  referencias `src=`/`href=` locales de todos los `.razor` de
  `Contingencia.Web/Components` y se comparó cada una, byte a byte de
  mayúsculas/minúsculas, contra el archivo real en `wwwroot` — **las 29
  coinciden exactamente**, 0 faltantes o con case distinto.
- **Paquetes NuGet, árbol transitivo completo revisado (no solo los
  directos)**: 0 coincidencias de `System.Drawing`/`System.Drawing.Common`
  en ninguna profundidad de la dependencia. Único paquete transitivo que
  amerita una nota (no un problema): `System.Security.Cryptography.
  ProtectedData` (viene de la cadena de `Microsoft.Data.SqlClient`/
  `Microsoft.IdentityModel`) implementa DPAPI, que solo funciona en
  Windows — pero esta app nunca lo invoca directamente (`grep` de
  `ProtectedData.` en código propio: 0 resultados) ni usa autenticación
  integrada de Windows contra SQL Server (sin `Integrated Security`/
  `Trusted_Connection` en ningún `appsettings*.json`, todo por usuario/
  contraseña) — el ensamblado carga sin problema en Linux, simplemente
  nunca se ejercita ese código.
- **Build final limpio**: `dotnet build Contingencia.Core.slnx` — 0
  errores, 2 advertencias preexistentes sin relación con Linux
  (`CS8602` en `Login.razor`, desreferencias nullable ya conocidas).

---
