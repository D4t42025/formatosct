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
    string? RutaFisica(string cia, string prescripcion, string nombreArchivo);

    // Réplica de la validación de archivo existente en gvconsulta_RowDataBound
    // (JuntaProfesional.aspx.vb) — decide qué ícono mostrar en la columna "Cargas" de la
    // grilla principal ("ya tiene documentos" vs. "cargar documentos").
    bool TieneArchivos(string cia, string prescripcion);
}
