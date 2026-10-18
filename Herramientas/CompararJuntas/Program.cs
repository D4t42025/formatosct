using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// Compara, SOLO para el proceso de Juntas, el esquema de la base de pruebas (DQUALITYREC,
// 172.25.2.15,1435 — cadena "DQUALITY" de Contingencia.Web/appsettings.Development.json) contra la
// base de producción real (servidor 168, DQUALITY — cadena armada con la variable de entorno de
// usuario SQLCMDPASSWORD_PROD168, nunca impresa). No copia ni modifica nada: solo lee metadatos
// (sys.tables/sys.columns/sys.procedures/sys.parameters + OBJECT_DEFINITION) y compara.
//
//   dotnet run --project Herramientas/CompararJuntas
//
// Requiere SQLCMDPASSWORD_PROD168 definida como variable de entorno de usuario (clave SQL de "sa"
// contra 45.169.253.168). La cadena de pruebas sale de appsettings.Development.json de Contingencia.Web
// (no hace falta ponerla aquí).

var raiz = Directory.GetParent(AppContext.BaseDirectory)!;
var raizRepo = BuscarRaizRepo(AppContext.BaseDirectory);
var configWeb = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(raizRepo, "Contingencia.Web"))
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var cadenaPruebas = configWeb.GetConnectionString("DQUALITY");
if (string.IsNullOrWhiteSpace(cadenaPruebas))
{
    Console.Error.WriteLine("No se encontró ConnectionStrings:DQUALITY en Contingencia.Web/appsettings.Development.json.");
    return 2;
}

var claveProd = Environment.GetEnvironmentVariable("SQLCMDPASSWORD_PROD168", EnvironmentVariableTarget.User)
    ?? Environment.GetEnvironmentVariable("SQLCMDPASSWORD_PROD168");
if (string.IsNullOrWhiteSpace(claveProd))
{
    Console.Error.WriteLine("Falta la variable de entorno de usuario SQLCMDPASSWORD_PROD168 (clave de 'sa' en el servidor 168).");
    return 2;
}

var cadenaProduccion = $"Server=45.169.253.168;Database=DQUALITY;User Id=sa;Password={claveProd};TrustServerCertificate=True;Connect Timeout=30";

await using var pruebas = new SqlConnection(cadenaPruebas);
await using var produccion = new SqlConnection(cadenaProduccion);
try
{
    await pruebas.OpenAsync();
    await produccion.OpenAsync();
}
catch (Exception e)
{
    // No se imprime el mensaje completo de SqlException tal cual por si acaso llegara a incluir la cadena;
    // en la práctica SqlClient no la incluye, pero se evita a propósito.
    Console.Error.WriteLine("No se pudo conectar a alguna de las dos bases (revise la clave y la conectividad de red al servidor 168).");
    Console.Error.WriteLine("Tipo de error: " + e.GetType().Name);
    return 2;
}

Console.WriteLine($"Pruebas    : {pruebas.DataSource} / {pruebas.Database}");
Console.WriteLine($"Producción : {produccion.DataSource} / {produccion.Database}");
Console.WriteLine();

// Tablas usadas en el recorrido Login -> módulo Juntas (ver MIGRACION-AZURE-JUNTAS.md, sección 20).
string[] tablas =
[
    "DQS_REC_USUARIOS", "DQS_REC_CLIENTES", "DQS_REC_IPS",
    "DQS_REC_ROLMODULO", "DQS_REC_ROLES", "DQS_REC_MODULOS", "DQS_OPT_LOGUEO",
    "DQS_MIP_TOKEN", "DQS_MIP_CRUZAR",
    "DQS_MIP_PROFESIONALES", "DQS_MIP_PROFESIONALESACTA", "DQS_MIP_JUNTASC",
    "ImperiumSesionPuente",
];

// Procedimientos usados en el mismo recorrido. sp_DQS_ConsultaFiltros se trata aparte: en pruebas es
// la versión "delgada" (02b_ProcedimientoFiltrosDelgado.sql) y en producción es el multiplexor legado
// completo — por diseño NO son el mismo texto, así que no se compara textualmente, solo existencia +
// parámetros.
string[] procedimientosComparables =
[
    "sp_DQS_ConsultaUsuario", "sp_DQS_ConsultaModxRol", "sp_DQS_ActualizaMarcaUsuario", "sp_opt_ingresalogueo",
    "sp_DQS_ConsultaProfesionalAñadido", "sp_DQS_ConsultaProfesional",
    "sp_DQS_AsignaProfesional", "sp_DQS_EliminaProfesional", "sp_DQS_ActualizaProfesional", "sp_DQS_ActualizaJunta",
];
const string procedimientoMultiplexor = "sp_DQS_ConsultaFiltros";

var difs = 0;

Console.WriteLine("== TABLAS (estructura de columnas) ==");
Console.WriteLine($"{"Tabla",-30} {"Pruebas",-10} {"Producción",-10}  Resultado");
foreach (var tabla in tablas)
{
    var colsPruebas = await Columnas(pruebas, tabla);
    var colsProd = await Columnas(produccion, tabla);
    var existePruebas = colsPruebas.Count > 0;
    var existeProd = colsProd.Count > 0;
    string resultado;
    if (!existePruebas && !existeProd) { resultado = "no existe en ninguna"; }
    else if (!existePruebas) { resultado = "FALTA en pruebas"; difs++; }
    else if (!existeProd) { resultado = "FALTA en producción"; difs++; }
    else
    {
        var igual = string.Join("|", colsPruebas) == string.Join("|", colsProd);
        resultado = igual ? "IGUAL" : "DIFERENTE (ver detalle abajo)";
        if (!igual) difs++;
    }

    Console.WriteLine($"{tabla,-30} {(existePruebas ? "sí" : "no"),-10} {(existeProd ? "sí" : "no"),-10}  {resultado}");
    if (existePruebas && existeProd && string.Join("|", colsPruebas) != string.Join("|", colsProd))
    {
        MostrarDiferenciaColumnas(tabla, colsPruebas, colsProd);
    }
}

Console.WriteLine();
Console.WriteLine("== PROCEDIMIENTOS (parámetros + definición) ==");
Console.WriteLine($"{"Procedimiento",-35} {"Pruebas",-10} {"Producción",-10}  Resultado");
foreach (var sp in procedimientosComparables)
{
    var defPruebas = await DefinicionProcedimiento(pruebas, sp);
    var defProd = await DefinicionProcedimiento(produccion, sp);
    string resultado;
    if (defPruebas is null && defProd is null) { resultado = "no existe en ninguna"; }
    else if (defPruebas is null) { resultado = "FALTA en pruebas"; difs++; }
    else if (defProd is null) { resultado = "FALTA en producción"; difs++; }
    else
    {
        var parametrosPruebas = await Parametros(pruebas, sp);
        var parametrosProd = await Parametros(produccion, sp);
        var mismosParametros = parametrosPruebas == parametrosProd;
        var mismaDefinicion = NormalizarTexto(defPruebas) == NormalizarTexto(defProd);
        if (mismosParametros && mismaDefinicion) resultado = "IGUAL";
        else if (mismosParametros) resultado = "parámetros iguales, cuerpo DIFERENTE";
        else { resultado = "DIFERENTE (parámetros distintos)"; }
        if (!mismosParametros || !mismaDefinicion) difs++;
    }

    Console.WriteLine($"{sp,-35} {(defPruebas is null ? "no" : "sí"),-10} {(defProd is null ? "no" : "sí"),-10}  {resultado}");
}

Console.WriteLine();
Console.WriteLine($"== {procedimientoMultiplexor} (tratado aparte: pruebas usa la versión delgada, producción el multiplexor legado) ==");
var defMultPruebas = await DefinicionProcedimiento(pruebas, procedimientoMultiplexor);
var defMultProd = await DefinicionProcedimiento(produccion, procedimientoMultiplexor);
var paramMultPruebas = await Parametros(pruebas, procedimientoMultiplexor);
var paramMultProd = await Parametros(produccion, procedimientoMultiplexor);
Console.WriteLine($"Existe en pruebas: {(defMultPruebas is null ? "no" : "sí")} ({defMultPruebas?.Length ?? 0} caracteres) | parámetros: {paramMultPruebas}");
Console.WriteLine($"Existe en producción: {(defMultProd is null ? "no" : "sí")} ({defMultProd?.Length ?? 0} caracteres) | parámetros: {paramMultProd}");
Console.WriteLine(paramMultPruebas == paramMultProd
    ? "Firma de parámetros IGUAL (lo relevante: que @STRCIA/@INTVALIDA y el resto coincidan en orden y tipo)."
    : "AVISO: la firma de parámetros DIFIERE — revisar manualmente antes de confiar en que @INTVALIDA=136/138 se comporten igual.");
if (paramMultPruebas != paramMultProd) difs++;
var textoIgual = defMultPruebas is not null && defMultProd is not null && NormalizarTexto(defMultPruebas) == NormalizarTexto(defMultProd);
Console.WriteLine($"Cuerpo del SP (texto normalizado) IGUAL: {textoIgual}");
var coreMultPruebas = await DefinicionProcedimiento(pruebas, "sp_DQS_ConsultaFiltros_Core");
var coreMultProd = await DefinicionProcedimiento(produccion, "sp_DQS_ConsultaFiltros_Core");
Console.WriteLine($"sp_DQS_ConsultaFiltros_Core existe en pruebas: {(coreMultPruebas is null ? "no" : "sí")} ({coreMultPruebas?.Length ?? 0} caracteres)");
Console.WriteLine($"sp_DQS_ConsultaFiltros_Core existe en producción: {(coreMultProd is null ? "no" : "sí")} ({coreMultProd?.Length ?? 0} caracteres)");

Console.WriteLine();
Console.WriteLine(difs == 0 ? "RESULTADO: sin diferencias relevantes." : $"RESULTADO: {difs} diferencia(s) encontradas (ver detalle arriba).");
return difs == 0 ? 0 : 1;

// ---------------------------------------------------------------------------------------------------------------------

static string BuscarRaizRepo(string desde)
{
    var dir = new DirectoryInfo(desde);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Contingencia.Core.slnx")) && !Directory.Exists(Path.Combine(dir.FullName, "Contingencia.Web")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio (buscando Contingencia.Web).");
}

static async Task<List<string>> Columnas(SqlConnection cn, string tabla)
{
    const string sql = """
        SELECT c.name, t.name AS tipo, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity
        FROM sys.columns c
        JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(@t)
        ORDER BY c.column_id
        """;
    await using var cmd = new SqlCommand(sql, cn);
    cmd.Parameters.AddWithValue("@t", "dbo." + tabla);
    var lista = new List<string>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        lista.Add($"{r.GetString(0)}:{r.GetString(1)}({r.GetInt16(2)},{r.GetByte(3)},{r.GetByte(4)}):null={r.GetBoolean(5)}:id={r.GetBoolean(6)}");
    return lista;
}

static void MostrarDiferenciaColumnas(string tabla, List<string> a, List<string> b)
{
    var soloA = a.Except(b).ToList();
    var soloB = b.Except(a).ToList();
    if (soloA.Count > 0) Console.WriteLine($"   solo en pruebas    ({tabla}): {string.Join(", ", soloA)}");
    if (soloB.Count > 0) Console.WriteLine($"   solo en producción ({tabla}): {string.Join(", ", soloB)}");
}

static async Task<string?> DefinicionProcedimiento(SqlConnection cn, string nombre)
{
    const string sql = "SELECT OBJECT_DEFINITION(OBJECT_ID(@n))";
    await using var cmd = new SqlCommand(sql, cn);
    cmd.Parameters.AddWithValue("@n", "dbo." + nombre);
    var r = await cmd.ExecuteScalarAsync();
    return r is DBNull or null ? null : (string)r;
}

static async Task<string> Parametros(SqlConnection cn, string nombre)
{
    const string sql = """
        SELECT p.name, t.name, p.max_length, p.is_output
        FROM sys.parameters p
        JOIN sys.types t ON t.user_type_id = p.user_type_id
        WHERE p.object_id = OBJECT_ID(@n)
        ORDER BY p.parameter_id
        """;
    await using var cmd = new SqlCommand(sql, cn);
    cmd.Parameters.AddWithValue("@n", "dbo." + nombre);
    var partes = new List<string>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        partes.Add($"{r.GetString(0)} {r.GetString(1)}({r.GetInt16(2)}){(r.GetBoolean(3) ? " OUT" : "")}");
    return string.Join(", ", partes);
}

static string NormalizarTexto(string texto)
{
    // Ignora diferencias de espacios en blanco/saltos de línea al comparar el cuerpo del SP.
    var sb = new StringBuilder(texto.Length);
    var espacioAnterior = false;
    foreach (var c in texto)
    {
        if (char.IsWhiteSpace(c))
        {
            if (!espacioAnterior) sb.Append(' ');
            espacioAnterior = true;
        }
        else
        {
            sb.Append(c);
            espacioAnterior = false;
        }
    }
    return sb.ToString().Trim();
}
