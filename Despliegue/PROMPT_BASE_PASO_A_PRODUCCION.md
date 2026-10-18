# Prompt base: llevar una aplicación migrada a producción con base de datos nueva

Uso: completar los datos entre `[corchetes]` y pegar todo lo que sigue como **primer mensaje** en Claude Code, con la carpeta de la solución abierta.
Basado en el proceso real de Formatos + Juntas (Contingencia 2026 core) del 2026-10: ver `Contingencia.Infrastructure/BASE_DE_DATOS.md`, sección "Paso a producción", y la carpeta `Despliegue/`.

---

## PROMPT (copiar desde aquí)

Responde siempre en español. Vas a ayudarme a llevar a producción la solución **[nombre de la solución / carpeta]** (Blazor Server, .NET [versión], Dapper con procedimientos almacenados, SQL Server) contra una **base de datos nueva**. Ya funciona en pruebas. Necesito que el proceso quede repetible y documentado.

### Datos del proyecto (los completo yo; si falta alguno, pregúntame antes de seguir)
- Solución y proyecto web: [ruta]
- Base de pruebas (la que usa el proyecto local): [servidor / base]. Base de origen de los catálogos: [servidor / base]
- Base de producción nueva: [servidor / base], ya creada vacía: [sí/no]
- CIA/clientes que arrancan en producción: [NIT, nombre, tipo, módulos que usan, si usan servicio web de afiliados o formulario manual]
- Usuarios iniciales a crear o copiar: [lista]
- Hosting: [IIS / Linux], subdominio [url], ¿hay balanceador (AWS ALB u otro) delante? [sí/no]
- Qué NO se copia (adjuntos, historial): [indicar]
- Documento de documentación de la BD: [ruta de BASE_DE_DATOS.md]

### Reglas de trabajo (obligatorias)
1. **Nada se ejecuta en producción sin mi autorización explícita en el chat.** Antes de cualquier cambio en una base o servidor real, muéstrame qué vas a ejecutar, en qué base, qué efecto tiene y cómo se revierte. Las consultas de solo lectura sí puedes hacerlas.
2. **Secretos**: nunca en archivos del repositorio, en la documentación, en commits ni en capturas. Las claves se pasan por variable de entorno o parámetro. Si una clave pasó por el chat, recomiéndame rotarla. Las claves que generes para mí me las entregas una sola vez en el chat.
3. **Documenta cada paso en `BASE_DE_DATOS.md` al mismo tiempo que lo haces**: qué se decidió, qué se hizo, qué se verificó, qué NO se probó y cómo se deshace. La documentación y el código van juntos, siempre.
4. **Decisiones**: si hay una decisión mía, dame máximo 3 opciones con una recomendación. Si es algo con un valor por defecto razonable, elígelo y dímelo.
5. **Honestidad**: dime qué probaste y qué no. Si algo falla, dime el mensaje exacto. No des por hecho que algo funciona si no lo verificaste.
6. **Git**: haz commits pequeños y por tema; no mezcles en un commit trabajo pendiente que no es de esta tarea. No hagas `push` sin que te lo pida.
7. **Entorno**: uso Windows. Escribe los scripts como archivos (`.ps1`, `.sql`) en vez de comandos largos en línea. Los scripts deben ser idempotentes (poder repetirse).

### Fases (haz una, verifica, documenta y espera mi visto bueno antes de la siguiente)

**Fase 0. Diagnóstico (solo lectura).** Lista las tablas y procedimientos que usa el código. Compara la definición de esos procedimientos entre producción actual (si existe) y pruebas, e indica qué cambió y en qué dirección (no sobrescribas lo más nuevo). Revisa lo que está fijo en el código para un solo cliente (NIT, URLs, textos, logos, catálogos) y propón cómo parametrizarlo por CIA. Entrégame un plan por pasos con las preguntas que necesites.

**Fase 1. Esquema versionado.** Genera scripts numerados (`00_CrearBaseDeDatos`, `01_Tablas`, `02_Procedimientos`, semillas) desde la base de pruebas. La base se crea con la **misma intercalación (collation)** que el origen; verifícala antes de copiar datos. Valida la sintaxis con `SET PARSEONLY ON` y comprueba el cierre de dependencias.

**Fase 2. Multi-CIA y parametrización.** Lo que cambia por cliente va en una tabla de parámetros por CIA, no en el código. Los IDs de roles y módulos se siembran iguales en cada CIA. El acceso se exige **también en el servidor** (políticas de autorización por módulo), no solo ocultando el menú.

**Fase 3. Copia de datos.** Herramienta de copia con `SqlBulkCopy`, una transacción por tabla, repetible y con modo `--verificar` (conteos y huellas). Primero ensayo en una base temporal con el origen real. Sin historial ni adjuntos salvo que lo indique.

**Fase 4. Seguridad.** Crea un usuario SQL de la aplicación con mínimo privilegio (lectura, escritura, ejecución de procedimientos y ver definiciones; nunca `sysadmin`) y pruébalo suplantándolo. Revisa el login heredado (claves en texto plano, claves fijas dentro de procedimientos, contraseñas temporales) y repórtame los hallazgos.

**Fase 5. Hosting.** Prepara el paquete de publicación `dotnet publish -c Release -r win-x64 --self-contained false` (sin `appsettings.Development.json`) y un script que defina las variables de entorno del sitio sin secretos escritos. Entrégame los pasos exactos para el servidor, con comprobación de cada uno y cómo volver atrás. Incluye una ruta `/salud` que pruebe la conexión a la base.

**Fase 6. Verificación.** Pruebas locales, luego contra pruebas, luego solo lectura en producción y, tras publicar, desde fuera (`/salud`, redirecciones, rutas protegidas). El inicio de sesión real con captcha lo hago yo manualmente. Todo dato de prueba que se cree se borra al final (pídeme confirmación).

**Fase 7. Entrega.** Respaldos de la base nueva, rotación de las claves que pasaron por el chat, fechas de licencias, lista de pendientes con responsable.

### Trampas conocidas (ya nos pasaron; evítalas)
- **Intercalación distinta** entre origen y destino hace fallar `SqlBulkCopy` y las comparaciones de texto.
- **Detrás de un balanceador que entrega por HTTP**: definir `ASPNETCORE_HTTPS_PORT` provoca un bucle de redirecciones. Hace falta `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` y no definir esa variable.
- **IIS compartido con otros sitios**: instalar el *Hosting Bundle* reinicia todo IIS. Si ya existe el módulo de ASP.NET Core, basta instalar solo el *ASP.NET Core Runtime x64* de la versión nueva, sin reiniciar nada.
- El servidor puede tener solo runtimes de **32 bits** o no tener unidad `D:`; verifica antes (`dotnet --list-runtimes` en `C:\Program Files\dotnet`, y las unidades).
- **Parámetros nuevos en el medio de un procedimiento** (sin valor por defecto) rompen las llamadas posicionales antiguas. Agrega al final con valor por defecto, o coordina el despliegue.
- Una clave con `=` o comillas rompe `sqlcmd -v` y `appcmd`; genera claves solo con letras, números y `# ! @ * + -`.
- El nombre real del sitio de IIS (`Get-Website`) puede no coincidir con el del subdominio.
- Un paquete de publicación sin `-r win-x64` pesa ~500 MB por traer binarios de todos los sistemas.
- La cookie de sesión debe salir con `Secure`; compruébalo en el navegador tras iniciar sesión.
- Los procedimientos del legado pueden tener comportamientos ocultos (por ejemplo reasignar una clave en cada login): léelos antes de copiarlos.

### Cómo quiero tus respuestas
Al terminar cada fase: **qué hiciste, qué verificaste, qué no pudiste probar, qué necesito decidir o entregarte, y cuál es el siguiente paso.** Corto y concreto.

### Primer paso
No toques nada todavía. Lee el proyecto y la documentación existente, ejecuta la Fase 0 y devuélveme el plan por pasos, los datos que te falten y los riesgos que veas.

## FIN DEL PROMPT
