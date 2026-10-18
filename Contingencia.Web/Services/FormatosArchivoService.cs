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

    private string RutaFisica(string cia, string radicado, string extension) =>
        Path.Combine(_directorioBase, Path.GetFileName(cia), Path.GetFileName(radicado) + Path.GetFileName(extension));

    public async Task GuardarAsync(string cia, string radicado, string extension, Stream contenido)
    {
        var rutaFisica = RutaFisica(cia, radicado, extension);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);
        await using var destino = File.Create(rutaFisica);
        await contenido.CopyToAsync(destino);
    }

    public Task<Stream?> AbrirAsync(string cia, string radicado, string extension)
    {
        var rutaFisica = RutaFisica(cia, radicado, extension);
        Stream? contenido = File.Exists(rutaFisica) ? File.OpenRead(rutaFisica) : null;
        return Task.FromResult(contenido);
    }

    public bool Eliminar(string cia, string radicado, string extension)
    {
        var rutaFisica = RutaFisica(cia, radicado, extension);
        if (!File.Exists(rutaFisica)) return false;
        File.Delete(rutaFisica);
        return true;
    }

    public bool Existe(string cia, string radicado, string extension) => File.Exists(RutaFisica(cia, radicado, extension));
}
