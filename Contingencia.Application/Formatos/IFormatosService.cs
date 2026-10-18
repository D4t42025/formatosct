namespace Contingencia.Application.Formatos;

public record PrestadorHabilitacion(string Codigo, string Nombre);

// Detalle de un prestador puntual (panel informativo tras seleccionarlo, wizard "Nueva
// Contingencia" — paso 2). Solo trae los campos que realmente existen en DQS_MIP_COD_HAB:
// esa tabla NO tiene dirección, teléfono ni correo (confirmado contra las 199.423 filas de
// DQUALITYREC, columnas Extra_IX/Extra_X/Descripcion siempre vacías) — no se inventan esos
// campos.
public record DetallePrestador(
    string Codigo, string Nombre, string? Nit, string? Departamento, string? Municipio,
    string? Clase, bool Habilitado);

public record ResultadoCargaFormato(bool Exito, string? Mensaje, string? Radicado);

// Columnas devueltas por sp_MIP_ConsultaFormatosCNT para la rama de rol 601 (única que
// atiende esta app): Gestion siempre llega vacío en esa rama (solo aplica a otros roles,
// que muestran un enlace de gestión — fuera de alcance, ver BASE_DE_DATOS.md).
public record FormatoConsultado(
    string Cia, string TipoId, string NumId, string Nit, string Obj, string Radicado,
    string Ruta, string Fecha, string FechaFormato, string NoPrescripcion,
    string Observaciones, string Estado);

public record ResultadoConsultaFormatos(bool Exito, string? Mensaje, IReadOnlyList<FormatoConsultado> Formatos);

public record ResultadoGestionFormato(bool Exito, string? Mensaje);

public interface IFormatosService
{
    // nit != null (parámetro PRESTADORES_NIT de la CIA) limita la búsqueda a las sedes de esa IPS.
    Task<IReadOnlyList<PrestadorHabilitacion>> BuscarPrestadoresAsync(string prefijo, string? nit = null);

    // Panel informativo del wizard "Nueva Contingencia" (paso 2) tras elegir una opción del
    // datalist — null si el código ya no existe (no debería pasar, viene del mismo catálogo).
    Task<DetallePrestador?> ObtenerDetallePrestadorAsync(string codigo, string? nit = null);

    // Valida en el mismo orden exacto que btngenerar_ServerClick en JuntasFormatoCntC.aspx.vb
    // (extensión → tipo doc → no documento → prestador → email → fecha) y, si todo pasa,
    // llama sp_MIP_InsertaFormatoCnt. NO guarda el archivo en disco — eso lo hace el
    // llamador (capa Web, con el stream del InputFile) una vez tiene el radicado, mismo
    // orden que el legado (llama al SP primero para obtener el nombre de archivo).
    Task<ResultadoCargaFormato> ValidarYRegistrarAsync(
        string cia, string usuario, string tipoDocumento, string numeroDocumento,
        string prestador, string observaciones, string extensionArchivo, bool hayArchivo,
        string email, string fechaFormato);

    // Réplica de la validación de btnconsultar_ServerClick / imgexporta_Click en
    // JuntasFormatoCntCO.aspx.vb (mismas dos reglas, mismos mensajes exactos — "Fecha Fin" y
    // "Estado" NUNCA formaron parte de esa validación, ni en el legado ni acá). Los campos
    // vacíos se sustituyen por "%" antes de llamar al SP, tal como hace el legado.
    //
    // "Fecha Fin" y "Estado" (stRCAMPO7/stRCAMPO8 en el legado) confirmados por lectura de
    // OBJECT_DEFINITION como NO usados por sp_MIP_ConsultaFormatosCNT (que solo acepta 7
    // parámetros) — pero el usuario reportó que en la UI real del legado SÍ filtran los
    // resultados visibles, lo que solo se explica si la BLL compilada (fuente no disponible,
    // mismo caso que RangoFechasAct) los aplica como filtro en memoria DESPUÉS de traer los
    // datos del SP, no como parámetro de la consulta SQL. Se replica ese comportamiento
    // filtrando en C# sobre lo que ya devolvió el SP (ver FormatosService.ConsultarAsync).
    Task<ResultadoConsultaFormatos> ConsultarAsync(
        string cia, string usuario, string tipoDocumento, string numeroDocumento,
        string prestador, string radicado, string fechaInicio, string fechaFin, string estado);

    // Réplica de btnguardar_ServerClick en JuntasFormatoCntCO2.aspx.vb (el modal de
    // "Gestionar formato", migrado a un modal dentro de ConsultarFormato.razor). El control
    // de qué roles pueden llamar esto (27/29/30) vive en el llamador (Razor), no acá — este
    // método asume que ya se validó eso.
    Task<ResultadoGestionFormato> GestionarAsync(
        string cia, string usuario, string radicado, string mipres, string observaciones, string estado);

    // Borra el registro de DQS_MIP_FORMATOS. Restringido en el llamador (Razor) a la cuenta
    // "Imperium" vía ClaimsPrincipalExtensions.PuedeEliminarComoImperium — este método no
    // valida permisos, asume que ya se hizo. No borra el archivo adjunto (lo hace el
    // llamador, vía IFormatosArchivoService, antes de llamar esto).
    Task<ResultadoGestionFormato> EliminarAsync(string cia, string radicado);
}
