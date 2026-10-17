namespace Contingencia.Web.Services;

// Guarda la firma de un profesional (dibujada en canvas o subida como archivo) para
// /profesionales — equivalente a la mitad "guardar en disco" de MIP_PROFESIONALES.aspx.vb
// (CarpetaDeFirma/btnSubirFirma_Click/GuardarFirma), bajo App_Data en vez de la carpeta
// pública del legado (mismo criterio de Formatos/Cargas: no servir archivos sin
// autenticación). Usa la MISMA forma de ruta relativa que el legado
// (MediaUploader/Files-Gestion-Usuarios/Firmas/<cia>/<clave>/Firma.<ext>) para que
// IFirmaImagenService (Infrastructure) la resuelva igual sin importar cuál de las dos apps
// la escribió — ver esa clase.
public interface IProfesionalFirmaArchivoService
{
    // "clave" identifica la carpeta del profesional — acá se usa el número de documento
    // directamente (a diferencia del legado, que resuelve un "usuario" con
    // sp_DQS_MIP_ProfesionalesResolverUsuario; ver nota en BASE_DE_DATOS.md). Es único por
    // CIA, que es lo único que hace falta para no chocar entre profesionales.
    Task<string> GuardarArchivoAsync(string cia, string clave, string nombreArchivoOriginal, Stream contenido);

    Task<string> GuardarDibujoAsync(string cia, string clave, string imagenBase64Png);

    // Data URI (data:image/...;base64,...) lista para un <img>, o null si la ruta no
    // resuelve a un archivo local (p.ej. todavía no se ha migrado MIP_PROFESIONALES.aspx y
    // la firma vive solo en el legado — no hay preview local para esas, se vuelve a subir).
    Task<string?> ObtenerDataUriAsync(string? rutaRelativa);
}
