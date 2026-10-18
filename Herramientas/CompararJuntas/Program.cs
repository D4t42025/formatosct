using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// Compara el esquema de una base "izquierda" contra la producción real (servidor 168, DQUALITY —
// cadena armada con la variable de entorno de usuario SQLCMDPASSWORD_PROD168, nunca impresa). No
// copia ni modifica nada: solo lee metadatos (sys.tables/sys.columns/sys.procedures/sys.parameters +
// OBJECT_DEFINITION) y compara.
//
//   dotnet run --project Herramientas/CompararJuntas                 -> pruebas (DQUALITYREC) vs 168
//   dotnet run --project Herramientas/CompararJuntas -- formatosct   -> server 13 (FormatosCT) vs 168
//
// Requiere SQLCMDPASSWORD_PROD168 (clave de 'sa' contra 45.169.253.168) siempre, y además
// SQLCMDPASSWORD_FORMATOSCT (clave de 'admin' contra 172.25.2.13) en modo "formatosct".
// La cadena de pruebas sale de appsettings.Development.json de Contingencia.Web (no hace falta
// ponerla aquí).

var modo = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "pruebas";
if (modo is not ("pruebas" or "formatosct"))
{
    Console.Error.WriteLine("Uso: dotnet run --project Herramientas/CompararJuntas [-- pruebas|formatosct]");
    return 2;
}

var raizRepo = BuscarRaizRepo(AppContext.BaseDirectory);

var claveProd = Environment.GetEnvironmentVariable("SQLCMDPASSWORD_PROD168", EnvironmentVariableTarget.User)
    ?? Environment.GetEnvironmentVariable("SQLCMDPASSWORD_PROD168");
if (string.IsNullOrWhiteSpace(claveProd))
{
    Console.Error.WriteLine("Falta la variable de entorno de usuario SQLCMDPASSWORD_PROD168 (clave de 'sa' en el servidor 168).");
    return 2;
}

string cadenaIzquierda;
string etiquetaIzquierda;
if (modo == "pruebas")
{
    var configWeb = new ConfigurationBuilder()
        .SetBasePath(Path.Combine(raizRepo, "Contingencia.Web"))
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();
    cadenaIzquierda = configWeb.GetConnectionString("DQUALITY") ?? "";
    if (string.IsNullOrWhiteSpace(cadenaIzquierda))
    {
        Console.Error.WriteLine("No se encontró ConnectionStrings:DQUALITY en Contingencia.Web/appsettings.Development.json.");
        return 2;
    }
    etiquetaIzquierda = "Pruebas";
}
else
{
    var claveFormatosCt = Environment.GetEnvironmentVariable("SQLCMDPASSWORD_FORMATOSCT", EnvironmentVariableTarget.User)
        ?? Environment.GetEnvironmentVariable("SQLCMDPASSWORD_FORMATOSCT");
    if (string.IsNullOrWhiteSpace(claveFormatosCt))
    {
        Console.Error.WriteLine("Falta la variable de entorno de usuario SQLCMDPASSWORD_FORMATOSCT (clave de 'admin' en el servidor 13, 172.25.2.13).");
        return 2;
    }
    cadenaIzquierda = $"Server=172.25.2.13;Database=FormatosCT;User Id=admin;Password={claveFormatosCt};TrustServerCertificate=True;Connect Timeout=30";
    etiquetaIzquierda = "FormatosCT (server 13)";
}
var cadenaProduccion = $"Server=45.169.253.168;Database=DQUALITY;User Id=sa;Password={claveProd};TrustServerCertificate=True;Connect Timeout=30";

await using var pruebas = new SqlConnection(cadenaIzquierda);
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
    Console.Error.WriteLine($"No se pudo conectar a alguna de las dos bases (revise la clave y la conectividad de red al servidor 168 / {etiquetaIzquierda}).");
    Console.Error.WriteLine("Tipo de error: " + e.GetType().Name);
    return 2;
}

Console.WriteLine($"{etiquetaIzquierda,-24}: {pruebas.DataSource} / {pruebas.Database}");
Console.WriteLine($"{"Producción (168)",-24}: {produccion.DataSource} / {produccion.Database}");
Console.WriteLine();

// Tablas a comparar. En modo "pruebas" solo las del recorrido Login -> módulo Juntas (ver
// MIGRACION-AZURE-JUNTAS.md, sección 20). En modo "formatosct" las 24 tablas reales del paquete de
// despliegue (Contingencia.Infrastructure/Sql/Produccion/01_Tablas.sql) — Formatos + Juntas completos.
string[] tablas = modo == "pruebas"
    ?
    [
        "DQS_REC_USUARIOS", "DQS_REC_CLIENTES", "DQS_REC_IPS",
        "DQS_REC_ROLMODULO", "DQS_REC_ROLES", "DQS_REC_MODULOS", "DQS_OPT_LOGUEO",
        "DQS_MIP_TOKEN", "DQS_MIP_CRUZAR",
        "DQS_MIP_PROFESIONALES", "DQS_MIP_PROFESIONALESACTA", "DQS_MIP_JUNTASC",
        "ImperiumSesionPuente",
    ]
    :
    [
        "DQS_REC_USUARIOS", "DQS_REC_ROLES", "DQS_REC_ROLMODULO", "DQS_REC_MODULOS", "DQS_REC_CLIENTES",
        "DQS_REC_IPS", "DQS_REC_NOTIFICACIONES", "DQS_OPT_LOGUEO", "ImperiumSesionPuente",
        "DQS_MIP_FORMATOS", "DQS_MIP_COD_HAB", "DQS_MIP_TOKEN", "DQS_MIP_PROFESIONALES",
        "DQS_MIP_PROFESIONALESACTA", "DQS_MIP_JUNTASC", "DQS_MIP_CRUZAR", "DQS_MIP_CONTINGENCIAS",
        "DQS_MIP_CONTINGENCIAS_TEC", "DQS_MIP_CONT_PARAMETRICAS", "DQS_MIP_AFILIADOS_TOKEN",
        "DQS_MIP_CIA_PARAMETROS", "DQS_MIP_DCI", "DQS_MIP_CODIGOS", "DQS_REC_CARGUECIE10",
    ];

// Procedimientos a comparar con el mismo criterio. ImperiumSesionPuente es tabla nueva de esta
// migración (no viene del 168) — su diferencia conocida (columna IdRol) ya está documentada.
// sp_DQS_ConsultaFiltros se trata aparte en ambos modos: en "pruebas"/"formatosct" es la versión
// "delgada" (02b_ProcedimientoFiltrosDelgado.sql) y en producción (168) es el multiplexor legado
// completo — por diseño NO son el mismo texto, así que no se compara textualmente, solo existencia +
// parámetros.
string[] procedimientosComparables = modo == "pruebas"
    ?
    [
        "sp_DQS_ConsultaUsuario", "sp_DQS_ConsultaModxRol", "sp_DQS_ActualizaMarcaUsuario", "sp_opt_ingresalogueo",
        "sp_DQS_ConsultaProfesionalAñadido", "sp_DQS_ConsultaProfesional",
        "sp_DQS_AsignaProfesional", "sp_DQS_EliminaProfesional", "sp_DQS_ActualizaProfesional", "sp_DQS_ActualizaJunta",
    ]
    :
    [
        // Copias directas del legado (deberían coincidir cuerpo a cuerpo con el 168):
        "sp_DQS_ConsultaUsuario", "sp_DQS_ConsultaModxRol", "sp_DQS_ActualizaMarcaUsuario", "sp_OPT_IngresaLogueo",
        "sp_DQS_ActualizaPwd",
        "sp_DQS_ConsultaProfesionalAñadido", "sp_DQS_ConsultaProfesional",
        "sp_DQS_AsignaProfesional", "sp_DQS_EliminaProfesional", "sp_DQS_ActualizaProfesional", "sp_DQS_ActualizaJunta",
        // SP nuevos de esta app (es normal que NO existan en el 168 — no son del legado):
        "sp_MIP_InsertaFormatoCnt", "sp_MIP_ConsultaFormatosCNT", "sp_MIP_ActualizaFormatoCnt",
        "sp_DQS_MIP_ProfesionalesResolverUsuario", "sp_DQS_MIP_ProfesionalesSincronizarUsuario",
        "sp_DQS_MIP_ProfesionalesConsultar", "sp_DQS_MIP_ProfesionalesObtener", "sp_DQS_MIP_ProfesionalesInsertar",
        "sp_DQS_MIP_ProfesionalesActualizar", "sp_DQS_MIP_ProfesionalesCambiarEstado",
        "sp_DQS_MIP_ProfesionalesResumen", "sp_DQS_MIP_ProfesionalesListas",
        "sp_MIP_InsertaContingencia", "sp_MIP_ConsultaTokenAfiliados", "sp_MIP_InsertaTokenAfiliados",
        "sp_MIP_ActualizaContingenciaArchivos", "sp_MIP_ConsultaContingencias", "sp_MIP_ConsultaContingenciaTecnologias",
        "sp_MIP_ConsultaParametricas", "sp_MIP_InsertaParametrica", "sp_MIP_ConsultaCiaParametros",
        "sp_MIP_CrearUsuarioFormatos", "sp_MIP_CambiarClaveUsuario",
    ];
// Nombres de SP que es normal que falten en el 168 (son nuevos de esta app, no existían en el legado).
HashSet<string> nuevosEnEstaApp = modo == "formatosct"
    ?
    [
        "sp_MIP_InsertaFormatoCnt", "sp_MIP_ConsultaFormatosCNT", "sp_MIP_ActualizaFormatoCnt",
        "sp_DQS_MIP_ProfesionalesResolverUsuario", "sp_DQS_MIP_ProfesionalesSincronizarUsuario",
        "sp_DQS_MIP_ProfesionalesConsultar", "sp_DQS_MIP_ProfesionalesObtener", "sp_DQS_MIP_ProfesionalesInsertar",
        "sp_DQS_MIP_ProfesionalesActualizar", "sp_DQS_MIP_ProfesionalesCambiarEstado",
        "sp_DQS_MIP_ProfesionalesResumen", "sp_DQS_MIP_ProfesionalesListas",
        "sp_MIP_InsertaContingencia", "sp_MIP_ConsultaTokenAfiliados", "sp_MIP_InsertaTokenAfiliados",
        "sp_MIP_ActualizaContingenciaArchivos", "sp_MIP_ConsultaContingencias", "sp_MIP_ConsultaContingenciaTecnologias",
        "sp_MIP_ConsultaParametricas", "sp_MIP_InsertaParametrica", "sp_MIP_ConsultaCiaParametros",
        "sp_MIP_CrearUsuarioFormatos", "sp_MIP_CambiarClaveUsuario",
    ]
    : [];
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
    var esNuevoDeEstaApp = nuevosEnEstaApp.Contains(sp);
    string resultado;
    if (defPruebas is null && defProd is null) { resultado = "no existe en ninguna"; }
    else if (defPruebas is null) { resultado = "FALTA en pruebas"; difs++; }
    else if (defProd is null) { resultado = esNuevoDeEstaApp ? "nuevo de esta app (no existía en 168, es esperado)" : "FALTA en producción"; if (!esNuevoDeEstaApp) difs++; }
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
