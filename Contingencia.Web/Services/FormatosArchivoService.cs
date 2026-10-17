using Contingencia.Infrastructure.Almacenamiento;
namespace Contingencia.Web.Services;

// Equivalente a Server.MapPath("~/MediaUploader/Formatos/" & cia & "/") en
// JuntasFormatoCntC.aspx.vb — bajo App_Data (fuera de wwwroot) en vez de una carpeta
// pública, mismo criterio ya usado por Soportes/Indexación/Imágenes en Recobros. El nombre
// de archivo es el radicado (mismo comportamiento que el legado: descarta el nombre
// original que trae el usuario).
public class FormatosArchivoService(IWebHostEnvironment entorno, IConfiguration configuracion) : IFormatosArchivoService
{
    private readonly string _directorioBase = Path.Combine(RutaAlmacenamiento.Base(configuracion, entorno.ContentRootPath), "mediauploader", "formatos");

    public string RutaFisica(string cia, string radicado, string extension) =>
        Path.Combine(_directorioBase, cia, radicado + extension);

    public async Task GuardarAsync(string rutaFisica, Stream contenido)
    {
        var directorio = Path.GetDirectoryName(rutaFisica)!;
        Directory.CreateDirectory(directorio);
        await using var destino = File.Create(rutaFisica);
        await contenido.CopyToAsync(destino);
    }

    public bool Existe(string rutaFisica) => File.Exists(rutaFisica);
}
