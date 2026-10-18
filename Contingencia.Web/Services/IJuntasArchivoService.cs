namespace Contingencia.Web.Services;

public record ArchivoCargue(string Nombre, string Url);

// Equivalente a JuntasCargueDoc.aspx — bajo App_Data (fuera de wwwroot) en vez de la
// carpeta pública ~/MediaUploader/ del legado, mismo criterio ya usado por
// FormatosArchivoService (no servir archivos de usuarios sin autenticación).
public interface IJuntasArchivoService
{
    IReadOnlyList<ArchivoCargue> Listar(string cia, string prescripcion);
    Task GuardarAsync(string cia, string prescripcion, string nombreArchivo, Stream contenido);
    bool Eliminar(string cia, string prescripcion, string nombreArchivo);

    // Réplica de la validación de archivo existente en gvconsulta_RowDataBound
    // (JuntaProfesional.aspx.vb) — decide qué ícono mostrar en la columna "Cargas" de la
    // grilla principal ("ya tiene documentos" vs. "cargar documentos").
    bool TieneArchivos(string cia, string prescripcion);

    // Versión en lote de TieneArchivos: una sola consulta de almacenamiento para TODAS las
    // prescripciones de la página consultada, en vez de una llamada por fila. Con Azure
    // Blob Storage, TieneArchivos por fila implica un round-trip HTTP por fila — con 25+
    // filas por página esto hacía que "Consultar" se sintiera muy lento. Usar siempre este
    // método para pintar la grilla; TieneArchivos queda para casos puntuales (p.ej. tras
    // subir/eliminar un archivo de una sola fila).
    Task<HashSet<string>> ObtenerPrescripcionesConArchivosAsync(string cia, IEnumerable<string> prescripciones);

    // Antes era RutaFisica(...) (una ruta en disco que el endpoint /api/juntas/cargue le
    // pasaba directo a Results.File). Con la implementación de Azure Blob Storage ya no
    // existe una ruta de archivo local, así que el endpoint abre el contenido como stream
    // sin importar de dónde venga (disco o blob).
    Task<Stream?> AbrirAsync(string cia, string prescripcion, string nombreArchivo);
}
