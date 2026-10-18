using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

// Copia de datos entre dos bases con la misma estructura (origen: p. ej. DQUALITYREC; destino: la base nueva creada con
// Sql/Produccion/01..06). Usa SqlBulkCopy en streaming (no genera archivos ni .sql de datos) y verifica los conteos.
//
//   dotnet run --project Herramientas/CopiarCatalogos -- --origen "<cadena>" --destino "<cadena>" [opciones]
//
// Las cadenas de conexión también pueden venir de las variables de entorno CONTINGENCIA_ORIGEN y CONTINGENCIA_DESTINO
// (evita dejar claves en el historial de la terminal).
//
// Siempre copia los CATÁLOGOS. Lo demás es opcional y por CIA:
//   --tokens <cia[,cia]>     DQS_MIP_TOKEN de esas CIA (credenciales SISPRO, SECRETAS) y el token del WS de afiliados
//   --usuarios <cia[,cia]>   DQS_REC_USUARIOS de esas CIA (claves en texto plano, SECRETAS)
//   --solo-usuarios <u,u>    con --usuarios: copia solo esos nombres de usuario (p. ej. cwformatos,cwjuntas) en vez de todos los de la CIA
//   --historial <cia[,cia]>  formatos, profesionales, actas, juntas y contingencias de esas CIA (solo las filas; los archivos
//                            en disco App_Data/mediauploader se copian aparte)
//   --reemplazar             si el destino ya tiene filas de esa tabla (o de esa CIA) las borra y vuelve a copiar; sin esto
//                            esa tabla se OMITE (la herramienta es repetible)
//   --verificar              no copia: solo compara conteos origen/destino
//
// Cada tabla se copia dentro de una transacción del destino: si falla algo o el conteo no coincide, esa tabla se revierte.

const int Lote = 10_000;

var opciones = LeerArgumentos(args);
if (opciones.ContainsKey("ayuda") || opciones.ContainsKey("h"))
{
    Console.WriteLine("Uso: --origen \"<cadena>\" --destino \"<cadena>\" [--tokens cia,cia] [--usuarios cia,cia [--solo-usuarios u,u]] [--historial cia,cia] [--reemplazar] [--verificar]");
    Console.WriteLine("Las cadenas también pueden venir de CONTINGENCIA_ORIGEN y CONTINGENCIA_DESTINO. Ver BASE_DE_DATOS.md, Paso 4.");
    return 0;
}

var cadenaOrigen = opciones.GetValueOrDefault("origen") ?? Environment.GetEnvironmentVariable("CONTINGENCIA_ORIGEN");
var cadenaDestino = opciones.GetValueOrDefault("destino") ?? Environment.GetEnvironmentVariable("CONTINGENCIA_DESTINO");
if (string.IsNullOrWhiteSpace(cadenaOrigen) || string.IsNullOrWhiteSpace(cadenaDestino))
{
    Console.Error.WriteLine("Faltan --origen y/o --destino (o las variables CONTINGENCIA_ORIGEN / CONTINGENCIA_DESTINO).");
    return 2;
}

var reemplazar = opciones.ContainsKey("reemplazar");
var soloVerificar = opciones.ContainsKey("verificar");

static string[] Lista(Dictionary<string, string?> o, string clave) =>
    (o.GetValueOrDefault(clave) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var tokensCia = Lista(opciones, "tokens");
var usuariosCia = Lista(opciones, "usuarios");
var historialCia = Lista(opciones, "historial");
var soloUsuarios = Lista(opciones, "solo-usuarios");

// Plan: (tabla, columna de CIA para filtrar o null = toda la tabla, valor de CIA, etiqueta, secreta)
var plan = new List<Item>();
foreach (var t in new[] { "DQS_MIP_COD_HAB", "DQS_MIP_CRUZAR", "DQS_MIP_DCI", "DQS_MIP_CODIGOS", "DQS_REC_CARGUECIE10", "DQS_MIP_CONT_PARAMETRICAS", "DQS_REC_IPS" })
    plan.Add(new Item(t, null, null, "catálogo", false));
foreach (var cia in tokensCia) plan.Add(new Item("DQS_MIP_TOKEN", "Cia", cia, "tokens SISPRO", true));
if (tokensCia.Length > 0) plan.Add(new Item("DQS_MIP_AFILIADOS_TOKEN", null, null, "token WS afiliados", true));
foreach (var cia in usuariosCia) plan.Add(new Item("DQS_REC_USUARIOS", "CIA", cia, "usuarios", true, soloUsuarios.Length > 0 ? soloUsuarios : null));
foreach (var cia in historialCia)
    foreach (var t in new[] { "DQS_MIP_FORMATOS", "DQS_MIP_PROFESIONALES", "DQS_MIP_PROFESIONALESACTA", "DQS_MIP_JUNTASC", "DQS_MIP_CONTINGENCIAS", "DQS_MIP_CONTINGENCIAS_TEC" })
        plan.Add(new Item(t, "CIA", cia, "historial", false));

await using var origen = new SqlConnection(cadenaOrigen);
await using var destino = new SqlConnection(cadenaDestino);
try
{
    await origen.OpenAsync();
    await destino.OpenAsync();
}
catch (Exception e)
{
    Console.Error.WriteLine("No se pudo conectar: " + e.Message);
    return 2;
}

// La intercalación de las dos bases debe ser la misma (las reales usan Modern_Spanish_CI_AS): SqlBulkCopy rechaza columnas de texto
// con distinta configuración regional y, además, la base nueva se comportaría distinto en comparaciones y búsquedas.
var collationOrigen = await Escalar(origen, "SELECT CONVERT(varchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))");
var collationDestino = await Escalar(destino, "SELECT CONVERT(varchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))");
if (!string.Equals(collationOrigen, collationDestino, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"La intercalación (collation) no coincide: origen {collationOrigen}, destino {collationDestino}.");
    Console.Error.WriteLine("Cree la base nueva con la misma intercalación (ver Sql/Produccion/00_CrearBaseDeDatos.sql) y vuelva a desplegar 01..06.");
    return 2;
}

Console.WriteLine($"Origen : {origen.DataSource} / {origen.Database} ({collationOrigen})");
Console.WriteLine($"Destino: {destino.DataSource} / {destino.Database} ({collationDestino})");
Console.WriteLine(soloVerificar ? "Modo: SOLO VERIFICAR (no se escribe nada)" : reemplazar ? "Modo: copiar y REEMPLAZAR lo existente" : "Modo: copiar (se omite lo que ya tenga filas)");
Console.WriteLine();
Console.WriteLine($"{"Tabla",-30} {"Alcance",-12} {"Origen",9} {"Destino",9}  Resultado");

var fallos = 0;
foreach (var item in plan.Where(i => i.Usuarios is not null).Take(1))
    Console.WriteLine($"(*) {item.Tabla}: solo los usuarios {string.Join(", ", item.Usuarios!)}");
foreach (var item in plan)
{
    var alcance = item.Cia is null ? "(toda)" : item.Usuarios is null ? item.Cia : $"{item.Cia}*";
    var reloj = Stopwatch.StartNew();
    try
    {
        var filas = await Copiar(item);
        Console.WriteLine($"{item.Tabla,-30} {alcance,-12} {filas.origen,9:N0} {filas.destino,9:N0}  {filas.resultado} ({reloj.Elapsed.TotalSeconds:0.0}s)");
        if (filas.fallo) fallos++;
    }
    catch (Exception e)
    {
        fallos++;
        Console.WriteLine($"{item.Tabla,-30} {alcance,-12} {"-",9} {"-",9}  ERROR: {e.Message}");
    }
}

Console.WriteLine();
Console.WriteLine(fallos == 0 ? "TERMINADO SIN FALLOS" : $"TERMINADO CON {fallos} FALLO(S)");
return fallos == 0 ? 0 : 1;

// ---------------------------------------------------------------------------------------------------------------------

async Task<(long origen, long destino, string resultado, bool fallo)> Copiar(Item item)
{
    var filtro = item.ColumnaCia is null ? "" : $" WHERE [{item.ColumnaCia}] = @cia";
    if (item.Usuarios is { Length: > 0 })
        filtro += " AND [USUARIO] IN (" + string.Join(", ", item.Usuarios.Select((_, i) => $"@u{i}")) + ")";

    var totalOrigen = await Contar(origen, item, filtro, null);
    if (!await Existe(destino, item.Tabla))
        return (totalOrigen, 0, "FALLO: la tabla no existe en el destino (ejecutar primero Sql/Produccion/01_Tablas.sql)", true);

    var totalDestino = await Contar(destino, item, filtro, null);

    if (soloVerificar)
        return (totalOrigen, totalDestino, totalOrigen == totalDestino ? "IGUAL" : "DIFERENTE", totalOrigen != totalDestino);

    if (totalDestino > 0 && !reemplazar)
        return totalDestino == totalOrigen
            ? (totalOrigen, totalDestino, "omitida (el destino ya la tiene completa)", false)
            : (totalOrigen, totalDestino, "INCOMPLETA: el destino ya tiene filas pero distintas del origen; usar --reemplazar para rehacerla", true);

    var columnas = await ColumnasComunes(item.Tabla);
    var identidad = await TieneIdentidad(destino, item.Tabla, columnas);

    await using var tx = (SqlTransaction)await destino.BeginTransactionAsync();
    try
    {
        if (totalDestino > 0)
        {
            await using var borrar = new SqlCommand($"DELETE FROM [dbo].[{item.Tabla}]{filtro}", destino, tx) { CommandTimeout = 0 };
            Parametros(borrar, item);
            await borrar.ExecuteNonQueryAsync();
        }

        var lista = string.Join(", ", columnas.Select(c => $"[{c}]"));
        await using var leer = new SqlCommand($"SELECT {lista} FROM [dbo].[{item.Tabla}]{filtro}", origen) { CommandTimeout = 0 };
        Parametros(leer, item);
        await using var lector = await leer.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

        var opcionesCopia = SqlBulkCopyOptions.TableLock | (identidad ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default);
        using var copia = new SqlBulkCopy(destino, opcionesCopia, tx)
        {
            DestinationTableName = $"[dbo].[{item.Tabla}]",
            BatchSize = Lote,
            BulkCopyTimeout = 0,
            EnableStreaming = true,
        };
        foreach (var c in columnas) copia.ColumnMappings.Add(c, c);
        await copia.WriteToServerAsync(lector);
        await lector.CloseAsync();

        var enDestino = await Contar(destino, item, filtro, tx);
        if (enDestino != totalOrigen)
        {
            await tx.RollbackAsync();
            return (totalOrigen, totalDestino, $"FALLO: se copiaron {enDestino:N0} de {totalOrigen:N0}; revertida", true);
        }

        await tx.CommitAsync();
        return (totalOrigen, enDestino, totalDestino > 0 ? "copiada (reemplazó lo anterior)" : "copiada", false);
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}

// Columnas del destino (sin calculadas) que también existen en el origen. Si al destino le queda una columna obligatoria sin
// equivalente en el origen, se avisa en vez de fallar a medias.
async Task<List<string>> ColumnasComunes(string tabla)
{
    var enOrigen = await Columnas(origen, tabla);
    var enDestino = await Columnas(destino, tabla);
    var comunes = enDestino.Where(d => !d.calculada && enOrigen.Any(o => string.Equals(o.nombre, d.nombre, StringComparison.OrdinalIgnoreCase)))
        .Select(d => enOrigen.First(o => string.Equals(o.nombre, d.nombre, StringComparison.OrdinalIgnoreCase)).nombre).ToList();
    var faltan = enDestino.Where(d => !d.calculada && !d.identidad && !d.admiteNulos && !comunes.Contains(d.nombre, StringComparer.OrdinalIgnoreCase)).Select(d => d.nombre).ToList();
    if (faltan.Count > 0)
        throw new InvalidOperationException($"el destino tiene columnas obligatorias que el origen no trae: {string.Join(", ", faltan)}");
    return comunes;
}

static async Task<List<(string nombre, bool calculada, bool identidad, bool admiteNulos)>> Columnas(SqlConnection cn, string tabla)
{
    await using var cmd = new SqlCommand(
        "SELECT c.name, c.is_computed, c.is_identity, c.is_nullable FROM sys.columns c WHERE c.object_id = OBJECT_ID(@t) ORDER BY c.column_id", cn);
    cmd.Parameters.AddWithValue("@t", "dbo." + tabla);
    var lista = new List<(string, bool, bool, bool)>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync()) lista.Add((r.GetString(0), r.GetBoolean(1), r.GetBoolean(2), r.GetBoolean(3)));
    return lista;
}

static async Task<bool> TieneIdentidad(SqlConnection cn, string tabla, List<string> columnas)
{
    var cols = await Columnas(cn, tabla);
    return cols.Any(c => c.identidad && columnas.Contains(c.nombre, StringComparer.OrdinalIgnoreCase));
}

static async Task<string?> Escalar(SqlConnection cn, string sql)
{
    await using var cmd = new SqlCommand(sql, cn);
    return Convert.ToString(await cmd.ExecuteScalarAsync());
}

static async Task<bool> Existe(SqlConnection cn, string tabla)
{
    await using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@t, 'U') IS NULL THEN 0 ELSE 1 END", cn);
    cmd.Parameters.AddWithValue("@t", "dbo." + tabla);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
}

static void Parametros(SqlCommand cmd, Item item)
{
    if (item.Cia is not null) cmd.Parameters.AddWithValue("@cia", item.Cia);
    if (item.Usuarios is not null)
        for (var i = 0; i < item.Usuarios.Length; i++) cmd.Parameters.AddWithValue($"@u{i}", item.Usuarios[i]);
}

static async Task<long> Contar(SqlConnection cn, Item item, string filtro, SqlTransaction? tx)
{
    if (tx is null && !await Existe(cn, item.Tabla)) return 0;
    await using var cmd = new SqlCommand($"SELECT COUNT_BIG(*) FROM [dbo].[{item.Tabla}]{filtro}", cn, tx) { CommandTimeout = 0 };
    Parametros(cmd, item);
    return Convert.ToInt64(await cmd.ExecuteScalarAsync());
}

static Dictionary<string, string?> LeerArgumentos(string[] argumentos)
{
    var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < argumentos.Length; i++)
    {
        if (!argumentos[i].StartsWith("--")) continue;
        var clave = argumentos[i][2..];
        var valor = i + 1 < argumentos.Length && !argumentos[i + 1].StartsWith("--") ? argumentos[++i] : null;
        d[clave] = valor;
    }
    return d;
}

internal sealed record Item(string Tabla, string? ColumnaCia, string? Cia, string Etiqueta, bool Secreta, string[]? Usuarios = null);
