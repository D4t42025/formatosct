using Contingencia.Infrastructure.Almacenamiento;
namespace Contingencia.Web.Services;

public class JuntasArchivoService(IWebHostEnvironment entorno, IConfiguration configuracion) : IJuntasArchivoService
{
    private readonly string _directorioBase = Path.Combine(RutaAlmacenamiento.Base(configuracion, entorno.ContentRootPath), "mediauploader");

    private string CarpetaCargue(string cia, string prescripcion) =>
        Path.Combine(_directorioBase, JuntasBucketCompartido.Normalizar(Path.GetFileName(cia)), "JuntasP", Path.GetFileName(prescripcion));

    public IReadOnlyList<ArchivoCargue> Listar(string cia, string prescripcion)
    {
        var carpeta = CarpetaCargue(cia, prescripcion);
        if (!Directory.Exists(carpeta))
        {
            return [];
        }

        return new DirectoryInfo(carpeta).GetFiles()
            .Select(f => new ArchivoCargue(
                f.Name,
                $"/api/juntas/cargue?prescripcion={Uri.EscapeDataString(prescripcion)}&archivo={Uri.EscapeDataString(f.Name)}"))
            .ToList();
    }

    public async Task GuardarAsync(string cia, string prescripcion, string nombreArchivo, Stream contenido)
    {
        var carpeta = CarpetaCargue(cia, prescripcion);
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, Path.GetFileName(nombreArchivo));
        await using var destino = File.Create(ruta);
        await contenido.CopyToAsync(destino);
    }

    public bool Eliminar(string cia, string prescripcion, string nombreArchivo)
    {
        var ruta = Path.Combine(CarpetaCargue(cia, prescripcion), Path.GetFileName(nombreArchivo));
        if (!File.Exists(ruta))
        {
            return false;
        }

        File.Delete(ruta);
        return true;
    }

    public Task<Stream?> AbrirAsync(string cia, string prescripcion, string nombreArchivo)
    {
        var ruta = Path.Combine(CarpetaCargue(cia, prescripcion), Path.GetFileName(nombreArchivo));
        Stream? contenido = File.Exists(ruta) ? File.OpenRead(ruta) : null;
        return Task.FromResult(contenido);
    }

    public bool TieneArchivos(string cia, string prescripcion)
    {
        var carpeta = CarpetaCargue(cia, prescripcion);
        return Directory.Exists(carpeta) && Directory.EnumerateFiles(carpeta).Any();
    }

    public Task<HashSet<string>> ObtenerPrescripcionesConArchivosAsync(string cia, IEnumerable<string> prescripciones)
    {
        var carpetaCia = Path.Combine(_directorioBase, JuntasBucketCompartido.Normalizar(Path.GetFileName(cia)), "JuntasP");
        if (!Directory.Exists(carpetaCia))
        {
            return Task.FromResult(new HashSet<string>());
        }

        var conArchivos = new DirectoryInfo(carpetaCia).GetDirectories()
            .Where(d => d.EnumerateFiles().Any())
            .Select(d => d.Name)
            .ToHashSet();
        return Task.FromResult(conArchivos);
    }
}
