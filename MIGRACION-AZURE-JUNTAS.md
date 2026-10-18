# Migración de almacenamiento del módulo Juntas a Azure Blob Storage

Documento de memoria del proceso: migración incremental ("por partes") del
almacenamiento de documentos del módulo **Juntas** de Contingencia 2026 core,
de disco local (`App_Data/mediauploader`) a **Azure Blob Storage**, más un
ajuste de UI de la tabla de resultados.

## 1. Contexto

Contingencia tiene dos modos de navegación (independientes entre sí, cada uno
habilitado por un claim distinto vía `ClaimsPrincipalExtensions.TieneModuloExacto`
en `AppLayout.razor`):

| Modo | Claim | Rutas | Páginas con carga de archivos |
|---|---|---|---|
| **Formatos** | `Contingencia` | `/inicio`, `/formatos/contingencias/nueva`, `/formatos/contingencias/consultar` | `CargarFormato.razor`, `NuevaContingencia.razor` |
| **Juntas** (Gestión) | `Junta Profesionales` / `Profesionales` | `/juntas`, `/profesionales` | `Juntas.razor`, `Profesionales.razor` |

Cada página usa un servicio de archivos propio:
- `IFormatosArchivoService` — soporte único (PDF/TIFF/PNG).
- `IContingenciaArchivoService` — soportes múltiples + firma.
- `IJuntasArchivoService` — cargues múltiples por prescripción (**migrado en este proceso**).
- `IProfesionalFirmaArchivoService` — firma/canvas de profesionales.

Solo **Juntas** fue migrado en esta iteración; los demás siguen en disco.

## 2. Diseño de la migración

- **Cuenta de Azure Storage**: `gitsanitas`.
- **Contenedor**: `juntas`.
- **Convención de nombre de blob** (igual a la ruta en disco, para que una
  futura migración de archivos existentes sea un simple "subir con el mismo
  nombre relativo"):
  ```
  {cia-normalizada}/JuntasP/{prescripcion}/{archivo}
  ```
- **Normalización de CIA compartida entre EPS**: la lógica que antes estaba
  duplicada se extrajo a un helper compartido:
  `Contingencia.Infrastructure/Almacenamiento/JuntasBucketCompartido.cs`
  (usado tanto por la implementación en disco como por la de Azure).

### Cambio de contrato de la interfaz

`IJuntasArchivoService` exponía `RutaFisica(...)` (una ruta de disco), lo cual
es incompatible con blobs (no existe ruta local). Se reemplazó por:

```csharp
Task<Stream?> AbrirAsync(string cia, string prescripcion, string nombreArchivo);
```

Esto permite que el endpoint `/api/juntas/cargue` en `Program.cs` sea agnóstico
al origen del stream (disco o blob) y use siempre:

```csharp
var stream = await archivoService.AbrirAsync(cia, prescripcion, archivo);
return Results.File(stream, contentType);
```

(`contentType` ahora se resuelve por el nombre del archivo, no por la ruta
física, ya que esta última ya no existe como concepto).

### Archivos tocados

| Archivo | Cambio |
|---|---|
| `Contingencia.Web/Contingencia.Web.csproj` | + `Azure.Storage.Blobs` 12.30.1 |
| `Contingencia.Infrastructure/Almacenamiento/JuntasBucketCompartido.cs` | **Nuevo** — normalización de CIA compartida (helper estático) |
| `Contingencia.Web/Services/IJuntasArchivoService.cs` | `RutaFisica` → `AbrirAsync` |
| `Contingencia.Web/Services/JuntasArchivoService.cs` | Implementación en disco, actualizada a la nueva interfaz (queda compilando pero **sin registrar en DI**, como referencia/fallback) |
| `Contingencia.Web/Services/JuntasArchivoServiceAzure.cs` | **Nuevo** — implementación contra `BlobContainerClient` (`Listar`, `GuardarAsync`, `Eliminar`, `TieneArchivos`, `AbrirAsync`) |
| `Contingencia.Web/Program.cs` | DI de `IJuntasArchivoService` → `JuntasArchivoServiceAzure`; endpoint `/api/juntas/cargue` reescrito a `async` + streaming |

### Configuración (nunca en código ni en git)

```jsonc
"Almacenamiento": {
  "Azure": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=gitsanitas;AccountKey=...;EndpointSuffix=core.windows.net",
    "Contenedor": "juntas" // opcional, default "juntas"
  }
}
```

Guardada únicamente vía `dotnet user-secrets` en la máquina del usuario
(`UserSecretsId` del proyecto: `9bd49ffc-601a-43d3-8245-552755916cf9`), **nunca**
en `appsettings.json` ni commiteada.

```powershell
cd "Contingencia.Web"
dotnet user-secrets set "Almacenamiento:Azure:ConnectionString" "DefaultEndpointsProtocol=https;AccountName=gitsanitas;AccountKey=...;EndpointSuffix=core.windows.net"
```

> Recordatorio: `dotnet user-secrets set` solo se aplica al reiniciar la app
> (`dotnet run` / `dotnet watch` / F5), no en caliente.

## 3. Incidentes resueltos durante la migración

1. **`BlobContainerClient.GetBlobs(...)` exige argumentos explícitos** en la
   versión 12.30.1 del SDK (no se puede omitir `traits`/`states`/`cancellationToken`
   por nombre) → se pasaron los 4 parámetros explícitos en `Listar()` y
   `TieneArchivos()`.
2. **`FormatException: No valid combination of account information found`** al
   probar `/juntas`: la `AccountKey` guardada en `user-secrets` tenía **87
   caracteres en vez de 88** (un carácter se perdió al pegar el valor en la
   consola) → Base64 inválido. Solución: copiar la cadena de conexión completa
   con el botón "Copiar" de Azure Portal (nunca escribirla a mano) y volver a
   correr `dotnet user-secrets set`.
3. **Nota de seguridad**: en el proceso, la clave real de la cuenta quedó
   pegada en texto plano en la conversación con el asistente. Se recomendó al
   usuario **rotar/regenerar la clave** en Azure Portal (Storage Account →
   Claves de acceso → regenerar key1) como precaución, ya que el historial de
   chat no es un almacén seguro de secretos.

## 4. Ajuste de UI en la tabla de resultados de Juntas

**Síntoma reportado**: las columnas de iconos (Pdf, Cargas, Firma, Correo) en
la tabla de `Juntas.razor` (`/juntas` → Consultar) ocupaban demasiado espacio
horizontal, igual que cualquier columna de texto, empujando las demás columnas.

**Solución**: se agregó la clase `fmt-col-icono` a esos 4 `<th>`/`<td>` y una
regla CSS en `wwwroot/css/formatos.css` que fija un ancho angosto (`2.2rem`) y
centra el contenido, además de reducir el tamaño de los iconos/botones
(de `1rem` a `0.85rem`, padding reducido). Archivos:
- `Contingencia.Web/Components/Pages/Juntas.razor` (clases `fmt-col-icono`)
- `Contingencia.Web/wwwroot/css/formatos.css` (`.fmt-col-icono`, `.fmt-icono-btn` reducido)

## 5. Lentitud al "Consultar" en Juntas (post-migración a Azure)

**Síntoma reportado**: el botón "Consultar" quedaba mucho tiempo en estado
"Consultando..." tras la migración a Azure.

**Causa raíz**: la columna "Cargas" de la grilla llamaba
`JuntasArchivoService.TieneArchivos(cia, f.NoPrescripcion)` **por cada fila**
para decidir el ícono (✓ ya tiene archivos / ☁ cargar). Con la implementación
en disco esto era una operación local instantánea; con Azure Blob Storage cada
llamada es un round-trip HTTP de "listar blobs" — con 25 filas por página eso
son 25 llamadas de red secuenciales solo para pintar la tabla.

**Solución**: se agregó `IJuntasArchivoService.ObtenerPrescripcionesConArchivosAsync(cia, prescripciones)`,
que hace **una sola** llamada de listado (prefijo `{cia}/JuntasP/`) y agrupa
los resultados por prescripción. `Juntas.razor` la invoca una vez justo
después de `ConsultarAsync` y guarda el resultado en el campo
`prescripcionesConArchivos` (HashSet), que es lo que ahora consulta cada fila
de la grilla (operación en memoria, sin red). Tras subir/eliminar un archivo
en el modal "Cargas" se actualiza puntualmente esa misma entrada del set
(`Add`/`Remove`), sin re-disparar la consulta completa.

Archivos tocados en este ajuste:
- `Contingencia.Web/Services/IJuntasArchivoService.cs` (+ `ObtenerPrescripcionesConArchivosAsync`)
- `Contingencia.Web/Services/JuntasArchivoServiceAzure.cs` (implementación con `GetBlobsAsync` + agrupación por prefijo)
- `Contingencia.Web/Services/JuntasArchivoService.cs` (implementación en disco, por paridad de interfaz)
- `Contingencia.Web/Components/Pages/Juntas.razor` (campo `prescripcionesConArchivos`, uso en la grilla y actualización puntual en los modales)

## 7. Consulta lenta en rangos largos (ej. un mes completo)

**Síntoma reportado**: consultar Juntas con un rango de un mes (ej. 01/09–30/09)
se siente muy lento — el botón se queda en "Consultando..." sin más
información durante mucho tiempo.

**Causa raíz (no es un bug de esta migración, es una limitación externa ya
documentada en `BASE_DE_DATOS.md`)**: la API de SISPRO/MIPRES
(`wsmipres.sispro.gov.co`) no tiene un endpoint de "rango de fechas" — solo
"por un día" (`JuntaProfesionalXFecha`). Para un rango de N días hay que hacer
N llamadas HTTP reales, cada una de 300ms a 30s (latencia variable del
servicio del gobierno, fuera de control de esta app). Esto ya se paralelizaba
(máx. 8 llamadas simultáneas, NIT/token resueltos una sola vez), pero para un
mes (~30 días) seguía sintiéndose lento.

**Mejoras aplicadas**:
1. **Paralelismo subido de 8 a 15** llamadas simultáneas
   (`JuntasService.MaxLlamadasEnParalelo`) — reduce a la mitad el número de
   "rondas" de espera para un rango largo.
2. **Resultados progresivos**: `IJuntasService.ConsultarAsync` ahora acepta un
   callback opcional `onProgreso` que se dispara cada vez que termina una
   llamada individual (un día o una prescripción), con sus filas + cuántas de
   las N llamadas totales ya terminaron. `Juntas.razor` lo usa para:
   - Pintar los resultados de cada día **tan pronto llegan**, en vez de
     esperar a que termine todo el rango (percepción de velocidad mucho
     mejor, aunque el tiempo total de la última llamada sea el mismo).
   - Mostrar el progreso real en el botón y un mensaje de ayuda: "Consultando
     día a día (X de Y)...".
   - El callback usa `InvokeAsync` para volver al hilo del circuito de Blazor
     antes de tocar el estado del componente (las tareas en paralelo corren en
     hilos del pool, no en el circuito) — evita condiciones de carrera con el
     renderizado de la grilla.

Archivos tocados:
- `Contingencia.Application/Juntas/IJuntasService.cs` (+ parámetro `onProgreso`)
- `Contingencia.Application/Juntas/JuntasService.cs` (`MaxLlamadasEnParalelo` 8→15, `EjecutarEnParaleloAsync` reporta progreso por tarea completada)
- `Contingencia.Web/Components/Pages/Juntas.razor` (progreso en vivo, campos `diasProcesados`/`diasTotales`, grilla se llena incrementalmente)

**Techo real que sigue existiendo**: la llamada más lenta del lote sigue
marcando el piso de tiempo total (la API del gobierno puede tardar hasta ~30s
en un solo día) — esto no se puede eliminar desde esta app, solo paralelizar
alrededor de eso. Un mes completo con datos reales probablemente siga tomando
decenas de segundos, pero ahora el usuario ve resultados aparecer
progresivamente en vez de una pantalla en blanco.

## 9. Más performance: tabla y respuesta de la API

**Tabla (renderizado)**:
- Se agregó `@key` a cada `<tr>` (combinando NoPrescripcion+Fecha+Consecutivo) —
  ayuda al algoritmo de diffing de Blazor a reutilizar filas existentes en vez
  de recrearlas en el DOM cuando la lista cambia (paginación, resultados que
  llegan progresivamente). La tabla ya estaba paginada (25 filas por página,
  `FilasPagina` con `Skip/Take`), así que cada repintado solo toca como máximo
  25 filas, nunca el total del rango consultado.

**Respuesta de la API (SISPRO)**:
1. **Compresión GZip/Deflate** habilitada en el `HttpClient` de
   `IJuntaProfesionalApiClient` (`AutomaticDecompression`, no estaba activada
   por defecto) — si el servidor del gobierno la soporta, baja el peso de cada
   respuesta JSON y el tiempo de transferencia, sobre todo en días con muchos
   registros. `MaxConnectionsPerServer` fijado en 20 y `PooledConnectionLifetime`
   en 5 min para que el pool de conexiones no sea cuello de botella frente al
   tope de 15 llamadas en paralelo.
2. **Cache en memoria (3 min) por día/prescripción**, antes de aplicar el
   filtro de Estado (`JuntasService`, vía `IMemoryCache`, ya registrado en
   `Program.cs`). Caso de uso real: el usuario consulta un rango, cambia el
   combo de Estado y vuelve a pulsar Consultar — con la cache, esa segunda
   consulta no vuelve a golpear la API del gobierno para nada (responde con
   lo ya descargado), solo re-filtra en memoria. TTL corto a propósito: es
   información que puede cambiar (nuevas actas) y no se quiere servir datos
   viejos por mucho tiempo.

Archivos tocados: `Contingencia.Web/Program.cs` (`AddHttpClient` con
`SocketsHttpHandler`), `Contingencia.Application/Juntas/JuntasService.cs`
(`IMemoryCache` + `GetOrCreateAsync` en `ConsultarUnaAsync`),
`Contingencia.Web/Components/Pages/Juntas.razor` (`@key` en las filas).

**Lo que esto NO soluciona**: la primera consulta de un rango nuevo (sin
cache) sigue atada a la latencia real de la API del gobierno por cada día —
eso sigue siendo un techo externo. Lo que se ganó es que consultas repetidas
(muy comunes al ajustar filtros) y el renderizado de la grilla ya no agregan
tiempo extra propio de esta app.

## 10. Tipo Tecnología: de letra suelta a chip con texto + ícono + color

**Pedido del usuario**: la columna "Tipo Tecnología" venía mostrando una sola
letra (M/N/P/D/S), poco clara para el usuario. Se pidió texto completo en vez
de la letra, con un ícono por tipo diferenciado por colores.

**Significado de cada letra** (confirmado por el usuario):
| Letra | Significado |
|---|---|
| M | Medicamentos |
| N | Nutricionales |
| P | Procedimientos |
| D | Dispositivos |
| S | Servicios |

**Implementación** (`Juntas.razor`):
- Nuevo helper `InfoTipoTecnologia(codigo)` (record struct `TipoTecnologiaInfo`
  con `Texto`/`Icono`/`Clase`), mismo patrón que `TraducirEstado` ya existente.
- La celda ahora renderiza un "chip" (mismo lenguaje visual que `fmt-estado`):
  ícono + texto completo, con color propio por tipo:
  - Medicamentos → `fa-pills`, morado.
  - Nutricionales → `fa-apple-whole`, verde.
  - Procedimientos → `fa-stethoscope`, azul (color de marca).
  - Dispositivos → `fa-crutch`, naranja.
  - Servicios → `fa-hand-holding-medical`, verde azulado (teal).
  - Cualquier valor no reconocido cae en un chip gris con `fa-circle-question`
    y muestra el código tal cual (no se pierde información si aparece una
    letra nueva no contemplada).
- El export a Excel también se actualizó para usar el texto completo en vez
  de la letra (antes exportaba la letra cruda), para que el archivo descargado
  sea igual de legible que la pantalla.

**CSS** (`formatos.css`): nueva clase base `.fmt-chip-tecnologia` (igual
estructura que `.fmt-estado`: píldora redondeada, `white-space: nowrap`) +
una clase de color por tipo (`.fmt-chip-medicamentos`, `.fmt-chip-nutricionales`,
`.fmt-chip-procedimientos`, `.fmt-chip-dispositivos`, `.fmt-chip-servicios`,
`.fmt-chip-desconocido`).

Archivos tocados: `Contingencia.Web/Components/Pages/Juntas.razor` (helper +
celda de tabla + export), `Contingencia.Web/wwwroot/css/formatos.css` (chips).

## 12. Consulta de un solo día sin filtros: sigue lenta (límite real, no de código)

**Caso reportado** (con captura): Fecha Inicio = Fecha Fin = mismo día, sin
N° Prescripción ni Tipo/Número de documento. El botón se queda en
"Consultando..." sin más información.

**Por qué este caso específico no se beneficia de nada de lo anterior**:
- Al ser un solo día, no hay nada que paralelizar (`diasTotales` siempre es 1)
  — el fix de paralelismo (sección 7) no aplica aquí.
- Sin filtro de documento, `ConsultarPorFechaAsync` usa
  `JuntaProfesionalXFecha/{nit}/{token}/{fecha}` — el endpoint **sin ningún
  parámetro de filtro adicional** (confirmado en `BASE_DE_DATOS.md`): trae
  todos los registros de esa CIA para ese día, sin paginar del lado del
  servidor. Es la consulta más pesada posible para un solo día, y es **una
  sola llamada HTTP obligatoria** — no hay forma de dividirla o pedirla en
  paralelo.
- La cache de 3 minutos (sección 9) solo ayuda en consultas **repetidas**; la
  primera vez que se pide un día nuevo siempre paga el costo real de la API.

**Conclusión honesta**: para este escenario puntual, la velocidad depende
100% de qué tan rápido responda la API del gobierno ese día en particular
(puede variar de instante a ~30s según su propia carga) — no hay ninguna
optimización de este lado que lo acelere más.

**Lo que sí se mejoró — percepción y transparencia, no velocidad real**:
- El botón ahora muestra un cronómetro en vivo: `Consultando... (Xs)` (o
  `(X/Y · Zs)` si hay varios días), actualizado cada segundo mediante un
  "ticker" en segundo plano (`TickerProgresoAsync`, cancelado al terminar la
  consulta) — así el usuario ve que sigue trabajando, no que se congeló.
- Pasados 4 segundos sin respuesta, aparece un mensaje explicando que la API
  del gobierno puede tardar hasta ~30s en un solo día sin filtros, para que
  el usuario entienda que no es un error de la aplicación.

Archivos tocados: `Contingencia.Web/Components/Pages/Juntas.razor`
(`segundosTranscurridos`, `tickerCts`, `TickerProgresoAsync`, botón y mensaje
de ayuda).

**Sugerencia pendiente de decisión del usuario**: si el N° Prescripción o el
Tipo/Número de documento se informan siempre que sea posible, la consulta cae
en el endpoint `JuntaProfesionalXPaciente` o `JuntaProfesional` (por
prescripción), que sí trae un conjunto acotado de datos y normalmente
responde mucho más rápido que "todo el día sin filtro". No se puede forzar
desde el código sin cambiar el comportamiento esperado por el usuario.

## 14. Columnas de texto libre (Justificación Técnica / Observaciones) ensanchaban la tabla

**Síntoma reportado** (con captura): un texto largo en "Justificación Técnica"
hacía que esa columna (y por lo tanto toda la tabla) se ensanchara mucho,
desproporcionado frente al resto de columnas cortas.

**Primer intento (con bug)**: se aplicó una clase `fmt-col-texto-largo` tanto
al `<th>` como al `<td>` de "Observaciones" y "Justificación Técnica", con
recorte a 3 líneas vía `display: -webkit-box; -webkit-line-clamp: 3`.

**Regresión detectada por el usuario** (con captura): los encabezados
"OBSERVACIONES" y "JUSTIFICACIÓN TÉCNICA" quedaron superpuestos, como si
fueran una sola columna. Causa: `display: -webkit-box` **reemplaza** el
`display: table-cell` implícito de un `<th>`, lo que rompe el algoritmo de
la tabla para calcular el ancho de esa columna — el encabezado deja de
participar correctamente en el layout de la fila y se renderiza montado
sobre la celda vecina.

**Fix definitivo**: la clase ahora se aplica **solo al `<td>`** (nunca al
`<th>`, que vuelve a ser texto plano sin clase) y usa recorte de **una sola
línea** con elipsis (`white-space: nowrap; overflow: hidden; text-overflow:
ellipsis`) en vez de `-webkit-box`/`line-clamp` — compatible con el modelo
normal de celda de tabla, sin alterar su `display`. El texto completo sigue
sin perderse: cada celda conserva `title="@f.Observaciones"` /
`title="@f.JustificacionTecnica"`, tooltip nativo al pasar el mouse.

Archivos tocados: `Contingencia.Web/Components/Pages/Juntas.razor` (clase
retirada de los `<th>`), `Contingencia.Web/wwwroot/css/formatos.css`
(`.fmt-tabla td.fmt-col-texto-largo`, selector corregido para no afectar
encabezados, recorte de una línea en vez de multilínea).

**Nota importante**: "Observaciones" y "Justificación Técnica" son campos
distintos (`f.Observaciones` y `f.JustificacionTecnica` respectivamente) —
en ningún punto del código se mezcló su contenido; solo compartían la misma
clase de estilo visual, que fue la causa del bug visual de los encabezados.

## 16. Estado (EstJM): también a chip con color + ícono por estado

Mismo tratamiento que ya se hizo para "Tipo Tecnología" (sección 10), ahora
aplicado a la columna "Estado". Antes todos los estados se veían con el
mismo pill azul genérico (clase `.fmt-estado`), sin diferenciarse entre sí
salvo por el texto.

**Cambio de código**: se extrajo un `record struct ChipInfo(Texto, Icono,
Clase)` reutilizable (antes era `TipoTecnologiaInfo`, específico de esa
columna) y se agregó `InfoEstado(codigo)` con el mismo patrón que
`InfoTipoTecnologia`. Se reemplazó `TraducirEstado` (solo texto) por este
nuevo helper en la celda de la tabla.

| Código EstJM | Texto | Color | Ícono |
|---|---|---|---|
| `1` | No requiere junta | Gris | `fa-circle-info` |
| `2` | Pendiente evaluación | Amarillo | `fa-hourglass-half` |
| `3` | Aprobada | Verde | `fa-circle-check` |
| `4` | No aprobada | Rojo | `fa-circle-xmark` |
| otro | (código crudo) | Gris | `fa-circle-question` |

**Export a Excel**: de paso se corrigió para que exporte el texto traducido
del Estado (`InfoEstado(f.EstJM).Texto`) en vez del código crudo (`1`/`2`/
`3`/`4`) — igual que ya se hacía para Tipo Tecnología, por consistencia.

**Nota**: la clase `.fmt-estado` (pill azul genérico) **no se eliminó** del
CSS porque sigue en uso en otras páginas (`ConsultarFormato.razor`,
`ConsultarNuevaContingencia.razor`, `Profesionales.razor`) — solo se dejó
de usar en `Juntas.razor`.

Archivos tocados: `Contingencia.Web/Components/Pages/Juntas.razor`
(`ChipInfo`, `InfoEstado`, celda de tabla, export Excel),
`Contingencia.Web/wwwroot/css/formatos.css` (`.fmt-chip-no-requiere`,
`.fmt-chip-pendiente`, `.fmt-chip-aprobada`, `.fmt-chip-no-aprobada`).

## 17. Ordenamiento de la grilla por encabezado

A pedido del usuario: poder ordenar la tabla de resultados haciendo clic en
cualquier encabezado de columna (excepto las 4 columnas de íconos: Pdf,
Cargas, Firma, Correo — esas no tienen un criterio de orden razonable).

**Comportamiento**: primer clic en un encabezado ordena ascendente; un
segundo clic sobre la **misma** columna invierte a descendente; clic en
**otra** columna reinicia a ascendente sobre la nueva. El encabezado activo
muestra una flecha resaltada (`fa-sort-up`/`fa-sort-down`); el resto muestra
`fa-sort` tenue (doble flecha) indicando "ordenable, sin usar aún".

**Orden "natural" en vez de alfabético puro**: todos los campos de
`JuntaProfesionalDetalle` llegan como `string` desde la API (incluso fechas
y consecutivos numéricos). Ordenar como texto puro habría dejado "10" antes
que "9", o fechas fuera de orden cronológico. Se implementó un
`OrdenNaturalComparer` que intenta parsear ambos valores como `decimal`
primero, luego como `DateTime`, y solo si ninguno aplica cae a comparación
de texto (`StringComparison.OrdinalIgnoreCase`).

**Columnas "Tipo Tecnología" y "Estado"**: se ordenan por el **texto
traducido** (p.ej. "Aprobada", "Medicamentos"), no por el código crudo
subyacente (M/N/P/D/S, 1/2/3/4) — así el orden visual coincide con lo que
el usuario ve en pantalla.

**Dónde vive el orden**: es 100% en memoria sobre `resultados` (ya cargados
por `ConsultarAsync`), igual que la paginación — no hay ninguna llamada
adicional a la API de SISPRO al ordenar. `FilasPagina` ahora pagina sobre
`ResultadosOrdenados` en vez de `resultados` directamente. Cambiar de
columna u orden reinicia a la página 1 (evita quedar en una página vacía si
el total de páginas cambia... en este caso no cambia, pero evita confusión
de "¿dónde quedó mi fila?").

Archivos tocados: `Contingencia.Web/Components/Pages/Juntas.razor`
(`ColumnasOrdenables`, `SelectoresOrden`, `OrdenarPor`, `IconoOrden`,
`ResultadosOrdenados`, `OrdenNaturalComparer`, encabezados `<th>` generados
con `@foreach` en vez de estar escritos a mano uno por uno),
`Contingencia.Web/wwwroot/css/formatos.css` (`.fmt-th-ordenable`, cursor
pointer + hover + estilo del ícono de orden).

## 18. Pendientes / preguntas abiertas

- [ ] ¿Migrar también los archivos ya existentes en
  `App_Data/mediauploader/.../JuntasP/...` hacia Azure, o dejar que convivan
  (nuevos cargues en Azure, históricos solo accesibles manualmente)? **Sin
  respuesta del usuario aún** — por ahora solo los cargues nuevos van a Azure.
- [ ] Posible lentitud reportada al dar clic en "Consultar" en `/juntas` — no
  investigada todavía (no está necesariamente relacionada con la migración de
  Azure, podría ser la consulta a la API de SISPRO/MIPRES).
- [ ] Migrar `Formatos`/`Contingencias`/`Profesionales` a Azure (explícitamente
  fuera de alcance de esta iteración, "por partes").

## 19. Acta PDF: régimen real, hora de expedición real, Dosis/Vía Administración traducidas

Pedido del usuario: el acta PDF de Juntas (tanto el link "Ver acta" de la
tabla como el que se envía por correo) tenía 4 problemas — (1) sin hora en
"Fecha y hora Expedición", (2) "Usuario Régimen" siempre vacío, (3) Dosis y
Vía Administración mostrando códigos crudos en vez de texto legible, y (4)
quería confirmar contra qué base de datos trabaja la app actualmente.

**(4) Base de datos actual**: SQL Server `172.25.2.15,1435`, base
`DQUALITYREC` (clave de conexión `DQUALITY` en `appsettings.Development.json`,
entorno de desarrollo).

**Verificación en vivo contra la API real de SISPRO** (2026-10-17): se agregó
instrumentación temporal (removida después de confirmar) para capturar el
JSON crudo de `JuntaProfesionalXFecha` y `PrescripcionXNumero` para datos
reales de producción (CIA 890330031, fecha 2026-10-16). Hallazgos clave:

- **FProceso/FechaActa (objeto "acta") nunca traen hora** — siempre vienen en
  `00:00:00` o solo fecha. La ÚNICA hora real que entrega la API está en
  `PrescripcionXNumero` → `HPrescripcion` (ej. `"12:31:53"`).
- **`HPrescripcion` NO es "Historia Clínica"** pese a su nombre — es la HORA
  real de la prescripción. El legado (y la migración hasta ahora) lo usaba
  por error para la celda "Número Historia Clínica". Esta API no expone
  ningún campo de historia clínica en absoluto.
- **Dosis y DosisUM son dos campos separados** que la API siempre entrega
  juntos (ej. `"Dosis":"1000", "DosisUM":"0168"`) — el modelo
  `MipresMedicamento`/`MipresProductoNutricional` nunca capturaba `DosisUM`
  (no existía la propiedad), así que esa unidad se perdía por completo.
  `DosisUM` se traduce contra `DQS_MIP_CRUZAR` (`Campo='DosisUM'`, ej.
  `"0168"` → `"mg"`) y se combina con la cantidad cruda: `"1000 mg"`.
- **`CodVA`/`CodViaAdmon`** (Vía Administración) sí es un código simple, se
  traduce contra `DQS_MIP_CRUZAR` (`Campo='CodVA'`, ej. `"042"` →
  `"INTRAVENOSA"`), mismo catálogo para Medicamentos y Productos
  Nutricionales (el modelo de Nutricionales solo cambia el nombre de la
  propiedad, no el espacio de códigos).

**Cambios implementados**:

1. **Hora de expedición real**: `FechaExpedicion` ahora combina
   `datosPrescripcion.FPrescripcion` (fecha) + `HPrescripcion` (hora,
   recortada a `HH:mm` para calzar con la etiqueta del PDF) en vez de
   `acta.FProceso` (que nunca tuvo ni podía tener hora). Se agregó la
   propiedad `FPrescripcion` a `MipresDatosPrescripcion` (no existía en el
   modelo pese a venir en el JSON).
2. **"Número Historia Clínica" queda vacío**: como `HPrescripcion` ahora se
   usa correctamente para la hora y esta API no tiene ningún campo real de
   historia clínica, la celda ya no muestra el valor erróneo (la hora
   disfrazada de historia clínica) — queda en blanco hasta que se identifique
   un campo real para esto. **Cambio de comportamiento visible**: antes la
   celda mostraba una hora con apariencia de número de historia; ahora
   aparece vacía. Avisar si en algún proceso interno se dependía de ese valor.
3. **Usuario Régimen real**: `IJuntasService.GenerarActaPdfAsync` ahora recibe
   un parámetro `regimen` (saneado a `Contributivo`/`Subsidiado`, con
   fallback a `Contributivo` si llega otra cosa) que se usa para resolver
   credenciales (antes siempre hardcodeado a `Contributivo` sin importar el
   filtro de búsqueda) y se imprime en la celda "Usuario Régimen". Viaja
   desde `Juntas.razor` (campo `regimen` del filtro de búsqueda) hasta el
   link `UrlActa` (`&regimen=...`) y el endpoint `/api/juntas/acta` (nuevo
   query param `regimen`, no sensible: solo afecta qué credenciales de
   SISPRO se usan, la CIA sigue viniendo de la sesión autenticada), y hasta
   la llamada de generación del PDF por correo.
4. **Dosis y Vía Administración traducidas**: en `JuntasService`, tras cargar
   el cruce `DQS_MIP_CRUZAR` (mismo mecanismo que ya usaba Municipio/
   Prestador/Diagnóstico/Ámbito), se recorren los Medicamentos y Productos
   Nutricionales traduciendo `DosisUM` y `CodVA`/`CodViaAdmon` con un nuevo
   helper `ResolverConRespaldo` — a diferencia de `BuscarCruzar` (que devuelve
   `""` si no hay coincidencia, apropiado para Municipio/etc.), este deja el
   código crudo si el catálogo no lo tiene, para no perder información del
   acta por un nombre de Campo que resulte no coincidir con algún código
   nuevo.

**Verificación**: se generó un acta real (prescripción
`20261016187002781187`, CIA 890330031, régimen Contributivo) y se confirmó en
el PDF resultante: `Fecha y hora Expedición(AAAA-MM-DD HH:mm) → 2026-10-16
12:31`, `Usuario Régimen → Contributivo`, `Dosis → 1000 mg`,
`Vía Administración → INTRAVENOSA`. Build de los 3 proyectos (`Web`,
`Application`, `Infrastructure`) sin errores.

Archivos tocados: `Contingencia.Application/Juntas/IJuntasService.cs`
(parámetro `regimen`), `Contingencia.Application/Juntas/JuntasService.cs`
(saneo de régimen, combinación fecha+hora, traducción Dosis/Vía
Administración, helper `ResolverConRespaldo`), `Contingencia.Infrastructure/
Juntas/DatosActaPdf.cs` (campo `UsuarioRegimen`), `Contingencia.Infrastructure/
Juntas/ActaPdfBuilder.cs` (celda "Usuario Régimen" ya no hardcodeada vacía),
`Contingencia.Infrastructure/Mipres/JuntaProfesionalApiModels.cs`
(`FPrescripcion` en `MipresDatosPrescripcion`, `DosisUM` en
`MipresMedicamento`/`MipresProductoNutricional`, comentario aclarando
`HPrescripcion`), `Contingencia.Web/Program.cs` (query param `regimen` en
`/api/juntas/acta`), `Contingencia.Web/Components/Pages/Juntas.razor`
(`UrlActa` pasa `regimen`, llamada de correo pasa `regimen`).

**Pendiente/abierto**: confirmar con el usuario si dejar "Número Historia
Clínica" vacío es aceptable, o si existe algún otro campo/fuente (fuera de
esta API de SISPRO) de donde debería salir ese dato.

**Resuelto (2026-10-17)**: el usuario confirmó que el dato simplemente no
existe ("no ese queda vacio no se tiene el dato") — queda cerrado, sin más
acción.

## 20. Mapa completo de tablas y SP: navegación Login → módulo Juntas

Pedido del usuario: documentar qué tablas y procedimientos almacenados se
usan en **todo** el recorrido desde el login hasta los módulos de Juntas,
para referencia/memoria del proceso. Investigación hecha leyendo
directamente `LoginRepository.cs`, `LoginService.cs`,
`SesionPuenteRepository.cs`, `Program.cs` (políticas de autorización),
`AppLayout.razor` (menú lateral) y `JuntasRepository.cs`/
`FiltrosRepository.cs`, cruzado con lo ya confirmado en
`Contingencia.Infrastructure/BASE_DE_DATOS.md` (OBJECT_DEFINITION de cada SP).

### 20.1 Paso 1 — Login (`LoginService.IniciarSesionAsync`)

| Orden | SP | Parámetros | Tabla(s) leídas/escritas | Para qué |
|---|---|---|---|---|
| 1 | `sp_DQS_ConsultaUsuario` | `@STRUSU, @STRPWD` | Lee `DQS_REC_USUARIOS` (join `DQS_REC_CLIENTES`/`DQS_REC_IPS` para logo, NIT, textos de carta). **Efecto secundario**: `UPDATE` de la clave fija de `cwjuntas` (`'juntas2023**'`) en cada intento de *cualquier* usuario (bug legado conocido, documentado en `BASE_DE_DATOS.md`) | Autentica usuario y trae datos de cliente/empresa para la sesión |
| 2 (solo si éxito) | `sp_DQS_ConsultaModxRol` | `@STRCIA, @STRROL` | Lee `DQS_REC_ROLMODULO` ⋈ `DQS_REC_ROLES` ⋈ `DQS_REC_MODULOS` por CIA+IDROL/ID_MODULO | Trae la lista de módulos visibles para ese usuario (incluye o no "Junta Profesionales") |
| 3 | `sp_DQS_ActualizaMarcaUsuario` | `@STRCIA, @STRUSUARIO, @INTMARCA` | `UPDATE` marca en `DQS_REC_USUARIOS` (1=login OK, 2=bloqueado) | Marca de sesión / bloqueo tras 3 intentos fallidos |
| 4 | `sp_opt_ingresalogueo` | `@CIA, @USUAPP, @IP` (envuelto en `try/catch`, ignora error) | `INSERT` en `DQS_OPT_LOGUEO` | Registro de auditoría de sesión activa |

Si no hay fila en paso 1 → se cuenta el intento fallido en caché (20 min,
no en BD) y al llegar a 3 se llama igual el paso 3 con `@INTMARCA=2`
(bloqueo), con `@STRCIA=""` (réplica fiel del legado, que tampoco resuelve
la CIA real en esa rama).

### 20.2 Paso 2 — Tabla puente de sesión (no es SP, es tabla propia de esta app)

`SesionPuenteRepository` escribe/lee **`dbo.ImperiumSesionPuente`** (misma
tabla física que usa Recobros, base `DQUALITY`/`DQUALITYREC`): al loguear
se hace `INSERT` con todos los datos de sesión + el resultado de
`sp_DQS_ConsultaModxRol` serializado como JSON en la columna `ModulosJson`,
con un token (`Guid`) de vigencia 10 minutos. `PuenteSesionMiddleware` lee
la cookie `ImperiumBridge`, consume (`SELECT` + `DELETE`, uso único) esa
fila y arma los `Claims` de la cookie de autenticación de esta app
(incluye un claim `imperium:modulo` por cada módulo en `ModulosJson`).

### 20.3 Paso 3 — Redirección según rol (`LoginService.DeterminarDestino`, en memoria, sin SP)

| `RolParaModulos` | Destino |
|---|---|
| `60` | `/juntas` (el único módulo que ve ese rol — usuario de pruebas `cwjuntas`) |
| `601` | `/inicio` |
| `35` | `ConsultaMipresWEB.aspx` (legado, sin migrar) |
| `27`, `29`, `30` | `/formatos/consultar` |
| otro | `InicioMIPRES.aspx` (legado) |

### 20.4 Paso 4 — Autorización de la página/menú Juntas (sin SP, basado en claims)

- `Juntas.razor` tiene `@attribute [Authorize(Policy = ImperiumPolicies.ModuloJuntas)]`.
- La política, en `Program.cs`, exige `contexto.User.TieneModuloExacto("Junta Profesionales")` —
  el claim viene del `ModulosJson` guardado en el paso 2 (texto exacto que
  trae `sp_DQS_ConsultaModxRol` desde `DQS_REC_MODULOS.MODULO`).
- El menú lateral (`AppLayout.razor`) muestra la tarjeta/enlace "Juntas"
  solo si `TieneModuloExacto("Junta Profesionales")` es verdadero — mismo
  claim, sin consulta adicional a BD (ya quedó en la cookie de sesión).

### 20.5 Dentro del módulo Juntas — búsqueda y acta PDF

| SP | Parámetros | Tabla(s) | Llamado desde |
|---|---|---|---|
| `sp_DQS_ConsultaFiltros` `@INTVALIDA=136` | 13 parámetros (solo CIA/Regimen/Detalle importan) | Lee `DQS_MIP_TOKEN` | `FiltrosRepository.ConsultarAsync` → credenciales NIT/TOKEN para llamar la API externa SISPRO/MIPRES (la búsqueda de actas en sí **no toca BD**, es 100% API externa) |
| `sp_DQS_ConsultaFiltros` `@INTVALIDA=138` | igual | Lee `DQS_MIP_CRUZAR` (columnas `CODIGO`, `DESCRIPCION`, `CAMPO`) | `JuntasService` — traduce decenas de códigos del acta (municipio, IPS, diagnóstico, Dosis UM, Vía Administración, forma farmacéutica, CUPS, dispositivo, etc.) |
| `sp_DQS_ConsultaProfesionalAñadido` | `@CIA, @ACTA` | Lee `DQS_MIP_PROFESIONALESACTA` ⋈ `DQS_MIP_PROFESIONALES` | `JuntasRepository.ConsultarProfesionalesActaAsync` — panel "Profesionales asignados" + tabla de firmas del acta en PDF |

### 20.6 Modal "Firma"/asignación de profesionales

| SP | Parámetros | Tabla(s) | Llamado desde |
|---|---|---|---|
| `sp_DQS_ConsultaProfesional` | `@cia, @doc, @nom, @prof, @sede, @serv` | Lee `DQS_MIP_PROFESIONALES` (búsqueda `LIKE` parcial) | `JuntasRepository.BuscarProfesionalesAsync` |
| `sp_DQS_AsignaProfesional` | `@CIA, @ACTA, @DOC` | `INSERT` en `DQS_MIP_PROFESIONALESACTA` (`ESTADO='APROBADA Y FIRMADA'`) | `JuntasRepository.AsignarProfesionalAsync` |
| `sp_DQS_EliminaProfesional` | `@CIA, @ACTA, @DOC` | `DELETE` en `DQS_MIP_PROFESIONALESACTA` | `JuntasRepository.EliminarProfesionalAsync` |
| `sp_DQS_ActualizaProfesional` | `@CIA, @ACTA` | `UPDATE DQS_MIP_PROFESIONALESACTA SET ESTADO='ENVIADA A EPS'` | `JuntasRepository.ActualizarEstadoActaAsync` (tras notificar por correo) |

### 20.7 Cargue de documentos ("Cargas")

| SP | Parámetros | Tabla(s) | Llamado desde |
|---|---|---|---|
| `sp_DQS_ActualizaJunta` | `@Cia, @MIPRES, @TIPOT, @CONT` | `DELETE` + `INSERT` en `DQS_MIP_JUNTASC` (columnas `CIA, MIPRES, TIPOT, CONT, FECHA`) | `JuntasRepository.RegistrarCargueAsync` — una vez por archivo subido; el archivo en sí va a Azure Blob Storage (sección 1–17 de este documento), no a BD |

### 20.8 Resumen visual del recorrido

```
Login (usuario/clave)
  └─ sp_DQS_ConsultaUsuario → DQS_REC_USUARIOS (+ DQS_REC_CLIENTES/DQS_REC_IPS)
       └─ éxito ─ sp_DQS_ConsultaModxRol → DQS_REC_ROLMODULO ⋈ DQS_REC_ROLES ⋈ DQS_REC_MODULOS
            └─ sp_DQS_ActualizaMarcaUsuario → DQS_REC_USUARIOS (marca)
            └─ sp_opt_ingresalogueo → DQS_OPT_LOGUEO (auditoría)
            └─ INSERT dbo.ImperiumSesionPuente (token + ModulosJson)
                 └─ cookie "ImperiumBridge" → PuenteSesionMiddleware
                      └─ SELECT+DELETE dbo.ImperiumSesionPuente → claims (imperium:modulo)
                           └─ rol=60 → redirige a /juntas (Policy ModuloJuntas = claim "Junta Profesionales")
                                └─ Buscar: sp_DQS_ConsultaFiltros(136) → DQS_MIP_TOKEN → API SISPRO/MIPRES
                                └─ Acta PDF: sp_DQS_ConsultaFiltros(138) → DQS_MIP_CRUZAR (traducciones)
                                           + sp_DQS_ConsultaProfesionalAñadido → DQS_MIP_PROFESIONALESACTA ⋈ DQS_MIP_PROFESIONALES
                                └─ Modal Firma: sp_DQS_ConsultaProfesional / AsignaProfesional / EliminaProfesional / ActualizaProfesional
                                └─ Cargue archivo: sp_DQS_ActualizaJunta → DQS_MIP_JUNTASC (+ Azure Blob)
```

Todo lo anterior ya estaba parcialmente disperso en
`Contingencia.Infrastructure/BASE_DE_DATOS.md` (confirmado ahí leyendo
`OBJECT_DEFINITION` de cada SP); esta sección lo consolida en un solo
recorrido ordenado específico para el módulo Juntas, desde el login.

## 21. Validación pruebas vs. producción (servidor 168, `DQUALITY`) — solo proceso de Juntas

Pedido del usuario: confirmar que, a nivel de base de datos, pruebas
(`DQUALITYREC`, `172.25.2.15,1435`) y producción (servidor **168**,
`45.169.253.168`, base `DQUALITY`) están igual **para el proceso de
Juntas**. Se creó una herramienta de solo lectura,
`Herramientas/CompararJuntas` (no copia ni modifica nada), que compara
contra las dos bases reales: columnas de las 13 tablas de la sección 20 y
parámetros + `OBJECT_DEFINITION` de los 10 SP comparables, más un trato
aparte para `sp_DQS_ConsultaFiltros`/`sp_DQS_ConsultaFiltros_Core` (el
multiplexor de 13.000+ líneas).

**Credenciales de producción**: por seguridad, el usuario configuró la
clave del `sa` del servidor 168 como variable de entorno de usuario
`SQLCMDPASSWORD_PROD168` en su equipo (GUI de Windows, sin pasarla nunca
por el chat); la herramienta la lee de ahí y arma la cadena de conexión en
memoria — nunca se imprime ni se guarda en ningún archivo del repositorio.

```
dotnet run --project Herramientas/CompararJuntas
```

**Resultado (verificado en vivo, 2026-10-17)**:

| Elemento | Pruebas | Producción (168) | Resultado |
|---|---|---|---|
| `DQS_REC_USUARIOS`, `DQS_REC_CLIENTES`, `DQS_REC_IPS`, `DQS_REC_ROLMODULO`, `DQS_REC_ROLES`, `DQS_REC_MODULOS`, `DQS_OPT_LOGUEO`, `DQS_MIP_TOKEN`, `DQS_MIP_CRUZAR`, `DQS_MIP_PROFESIONALES`, `DQS_MIP_PROFESIONALESACTA`, `DQS_MIP_JUNTASC` (12 tablas) | ✅ | ✅ | **IGUAL** (mismas columnas, tipos, longitudes y nulabilidad) |
| `ImperiumSesionPuente` | tiene columna `IdRol` | **no tiene `IdRol`** | **DIFERENTE** — ver detalle abajo |
| `sp_DQS_ConsultaUsuario`, `sp_DQS_ConsultaModxRol`, `sp_DQS_ActualizaMarcaUsuario`, `sp_opt_ingresalogueo`, `sp_DQS_ConsultaProfesionalAñadido`, `sp_DQS_ConsultaProfesional`, `sp_DQS_AsignaProfesional`, `sp_DQS_EliminaProfesional`, `sp_DQS_ActualizaProfesional`, `sp_DQS_ActualizaJunta` (10 SP) | ✅ | ✅ | **IGUAL** (mismos parámetros y mismo cuerpo, texto normalizado) |
| `sp_DQS_ConsultaFiltros` (envoltorio, 2434 caracteres) | ✅ | ✅ | **IGUAL** — mismos 13 parámetros y mismo texto exacto en ambas bases |
| `sp_DQS_ConsultaFiltros_Core` (multiplexor real, 245.459 caracteres) | ✅ | ✅ | **IGUAL** — texto idéntico carácter a carácter en ambas bases |

**Única diferencia encontrada**: la tabla `dbo.ImperiumSesionPuente` (tabla
puente de sesión entre el login y los módulos — sección 20.2) en
producción (168) **no tiene la columna `IdRol`** que sí tiene pruebas
(`nvarchar(50) NULL`, agregada por `Sql/Produccion/01_Tablas.sql` cuando se
creó la base nueva `FormatosCT`). `SesionPuenteRepository.CrearAsync`
hace `INSERT` explícito en esa columna — si esta app llegara a apuntar su
cadena `DQUALITY` directamente al servidor 168, ese `INSERT` fallaría con
"Invalid column name 'IdRol'".

**Causa probable**: `ImperiumSesionPuente` en el 168 es la tabla original
creada por Recobros antes de que esta migración agregara `IdRol` (usado
para guardar el rol numérico del usuario y poder redirigir por rol — ver
sección 20.3); nunca se replicó ese `ALTER` al 168 porque el 168 no es el
destino de despliegue real de esta app (el destino real es `FormatosCT` en
`172.25.2.13`, ya creada con `01_Tablas.sql` completo, sección 19 del
documento del lado de Formatos).

**Corrección, si se decide apuntar esta app al 168 directamente**:

```sql
ALTER TABLE dbo.ImperiumSesionPuente ADD IdRol nvarchar(50) NULL;
```

**Conclusión**: para el proceso de Juntas (tablas de datos, catálogos y
los 10 SP de negocio, incluido el multiplexor completo de filtros), pruebas
y producción (168) están **100% iguales**. La única diferencia es una
columna de la tabla puente de sesión (`IdRol`), que es infraestructura de
*esta* migración (no del proceso de Juntas en sí) y solo importa si se
decide usar el 168 como destino real de despliegue en vez de `FormatosCT`.

Archivo nuevo: `Herramientas/CompararJuntas/` (`CompararJuntas.csproj`,
`Program.cs`) — herramienta de solo lectura, reutilizable para futuras
verificaciones de paridad de esquema.

## 22. Migración de los archivos ya cargados (app anterior → Azure Blob Storage)

Pedido del usuario: el app anterior (legado, aún en producción) tiene una gran
cantidad de juntas ya cargadas en disco, con la misma estructura de carpetas
que usa la nueva (`JuntasArchivoServiceAzure.cs`, sección 1 de este documento).
Se necesita (1) subir todo lo ya existente a Azure a corte de hoy, y (2) que
mientras arranca la app nueva y el legado sigue vivo en paralelo, un proceso
diario siga subiendo lo que se vaya agregando en el legado.

**Origen confirmado por el usuario** (servidor legado, con salida a
internet, sin acceso directo desde este equipo — no es una ruta de red
alcanzable por el agente):

```
C:\Inetpub\vhosts\imperiumsanitas.net.co\httpdocs\MediaUploader\890330031\JuntasP
```

Solo existe la CIA `890330031` (Sanitas) bajo Juntas por ahora — esta app hoy
la usa un solo cliente.

**Destino**: cuenta de Azure Storage `gitsanitas`, contenedor `juntas`, mismo
prefijo relativo que ya usa `JuntasArchivoServiceAzure.cs`:
`890330031/JuntasP/{prescripcion}/{archivo}` — así los archivos migrados
quedan exactamente donde la app nueva ya los busca, sin tocar código.

**Herramienta**: `Despliegue/Migrar-JuntasAzure.ps1` (nuevo), script de
PowerShell que corre **en el servidor legado** (el agente no tiene acceso a
ese servidor, así que no se pudo ejecutar desde aquí — el usuario lo copia y
lo corre allá). Usa **AzCopy** (herramienta oficial de Microsoft) en modo
`sync`: compara origen (disco) y destino (Azure) por fecha/tamaño y copia
solo lo nuevo o modificado — por eso el mismo script sirve para la carga
total de hoy (primera corrida) y para las corridas diarias siguientes (solo
sube lo que cambió desde la corrida anterior), sin lógica de fechas propia.
Por defecto no borra nada en Azure (solo agrega); tiene un parámetro
`-Borrar` opcional si algún día se quiere replicar también los borrados.

**Credencial**: el script lee la variable de entorno de **usuario** (no de
sesión, para que la vea también el Programador de tareas de Windows)
`AZCOPY_JUNTAS_SAS` con la "Blob SAS URL" del contenedor `juntas` — **no**
la cadena de conexión completa ni la clave de la cuenta. Se genera así, sin
que la clave de la cuenta pase nunca por el agente ni por el chat:

1. Portal de Azure → cuenta de almacenamiento `gitsanitas` → **Contenedores**
   → contenedor `juntas` → **Tokens de acceso compartido (SAS)**.
2. Permisos: **Lectura, Escritura, Eliminación (opcional), Enumerar, Crear**.
3. Expiración: una fecha cómoda y lejana (ej. 1 año; se puede regenerar
   cuando se quiera sin afectar la clave de la cuenta ni las demás apps).
4. Copiar la **"Blob SAS URL"** completa (empieza en `https://gitsanitas.blob.core.windows.net/juntas?sv=...`).
5. En el servidor legado: Propiedades del sistema → Variables de entorno →
   Variables de **usuario** → Nueva → nombre `AZCOPY_JUNTAS_SAS`, valor esa
   URL completa. Abrir una terminal/sesión nueva después de guardarla.

**Pasos para el usuario en el servidor legado**:

1. Instalar AzCopy (si no está): descargar de
   https://aka.ms/downloadazcopy-v10-windows, descomprimir y agregar la
   carpeta al PATH (o copiar `azcopy.exe` junto al script).
2. Generar el SAS y configurar `AZCOPY_JUNTAS_SAS` (pasos de arriba).
3. Copiar `Despliegue/Migrar-JuntasAzure.ps1` al servidor y correrlo una vez
   manualmente — esto hace la **carga total a corte de hoy**.
4. Programar una tarea diaria (Programador de tareas de Windows) que corra
   el mismo script **después de la 1:00 p.m.** todos los días, mientras el
   legado siga vivo en paralelo con la app nueva. Pasos GUI: Programador de
   tareas → Crear tarea básica → Diariamente, hora 13:00 (o la que se
   prefiera) → Acción "Iniciar un programa" → `powershell.exe` con
   argumentos `-ExecutionPolicy Bypass -File "C:\ruta\Migrar-JuntasAzure.ps1"`.
5. El día que se decida el corte final (legado apagado / todos los usuarios
   en la app nueva), desactivar o borrar esa tarea programada.

**Estado: EJECUTADO Y EN PRODUCCIÓN (17-oct-2026).**

Resultado de la carga inicial completa (ejecutada por el usuario en el
servidor legado con AzCopy v10.32.8):

- **173,661 archivos** copiados, **0 fallos**.
- **53,387,853,002 bytes** (~53.4 GB) transferidos.
- Duración: ~72.5 minutos.
- Log: `C:\MigracionJuntas\logs-migracion-juntas\sync-2026-10-17_193536.log`
  (en el servidor legado).

Tarea programada diaria creada en el servidor legado (usuario `Administrator`,
corre todos los días a las **9:30 p.m.**, mientras el legado y la app nueva
convivan en paralelo):

```powershell
$accion = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File `"C:\MigracionJuntas\Migrar-JuntasAzure.ps1`""
$disparador = New-ScheduledTaskTrigger -Daily -At "21:30"
Register-ScheduledTask -TaskName "MigracionJuntasAzure" -Action $accion -Trigger $disparador -User "Administrator" -RunLevel Highest -Description "Sincroniza diariamente los archivos de Juntas hacia Azure Blob Storage"
```

(La tarea se creó inicialmente a la 1:30 p.m. y se reprogramó a las 9:30 p.m.
el mismo día con `Set-ScheduledTask -TaskName "MigracionJuntasAzure" -Trigger (New-ScheduledTaskTrigger -Daily -At "21:30")`,
quedando activa desde esa misma noche del 17-oct-2026.)

El día del corte final (legado apagado), eliminarla con:
```powershell
Unregister-ScheduledTask -TaskName "MigracionJuntasAzure" -Confirm:$false
```

**Lecciones aprendidas durante la puesta en marcha** (útiles para procesos
similares futuros):

- **El SAS debe generarse DESDE DENTRO del contenedor**, no desde la cuenta
  de almacenamiento completa. Un SAS de cuenta (`Almacenamiento → Firma de
  acceso compartido`) genera una URL del tipo
  `https://gitsanitas.blob.core.windows.net/?sv=...&ss=bfqt&srt=sco&sp=rwdlacupiytfx...`
  — nótese que falta el segmento `/juntas` y los permisos (`ss=`/`srt=`)
  son mucho más amplios de lo necesario (acceso a los 4 servicios de
  Storage). AzCopy `sync` rechaza explícitamente estas "service level URLs"
  con el error `service level URLs (...) are not supported in sync`.
  El SAS correcto se genera así: **Portal → cuenta `gitsanitas` →
  "Almacenamiento de datos" → "Contenedores"** (la vista clásica, no
  "Explorador de almacenamiento"/Storage Browser, que no tiene esta opción)
  → marcar el checkbox de la fila `juntas` → botón **"Generar SAS"** en la
  barra de herramientas → panel "Generar URL y token de SAS" → marcar
  Lectura/Escritura/Enumerar/Eliminar (opcional)/Crear → expiración ~1 año
  → copiar el campo **"URL de SAS de Blob"**, que sí trae `/juntas?...`.
- **Las variables de entorno de usuario creadas/editadas por GUI solo las
  ven procesos NUEVOS.** Cualquier ventana de PowerShell ya abierta antes
  del cambio sigue viendo el valor viejo (o ninguno). Siempre cerrar TODAS
  las ventanas de PowerShell/cmd abiertas y abrir una nueva después de
  crear o editar `AZCOPY_JUNTAS_SAS`.
- **`Get-Command azcopy` solo busca en el PATH**, no en la carpeta actual —
  aunque `azcopy.exe` esté justo al lado del script. El script ya incluye
  un fallback a `$PSScriptRoot\azcopy.exe` para este caso; si de todas
  formas falla, verificar que `azcopy.exe` esté en la misma carpeta que
  `Migrar-JuntasAzure.ps1`.
- **El script se debe guardar como archivo `.ps1` y ejecutar con
  `.\Migrar-JuntasAzure.ps1`** — pegar el código directamente en la consola
  interactiva de PowerShell rompe variables automáticas como `$PSScriptRoot`
  (queda vacía) y produce errores de `Join-Path`. Forma más confiable de
  crear/editar el archivo sin pasar por un diálogo "Guardar como": desde
  PowerShell, `notepad .\Migrar-JuntasAzure.ps1` (confirma "crear archivo"
  si no existe), pegar el contenido, `Ctrl+S`, cerrar.
- **Seguridad**: el primer SAS generado (de cuenta completa, permisos
  amplios) quedó expuesto en una captura de pantalla compartida por chat.
  Se reemplazó por el SAS de contenedor antes de usarlo en producción;
  queda pendiente como buena práctica rotar/invalidar ese SAS de cuenta
  original si sigue activo (se puede revocar regenerando las claves de
  acceso de la cuenta, o simplemente dejar que expire — tenía vigencia
  corta, ~8 horas).

**Pendiente**: ninguno operativo — el proceso ya corre a diario. Verificar
periódicamente (o al cerrar el legado) los logs en
`C:\MigracionJuntas\logs-migracion-juntas\` para confirmar que las corridas
diarias siguen completando sin fallos.
