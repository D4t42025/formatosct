using Contingencia.Infrastructure.Almacenamiento;
namespace Contingencia.Web.Services;

public class JuntasArchivoService(IWebHostEnvironment entorno, IConfiguration configuracion) : IJuntasArchivoService
{
    private readonly string _directorioBase = Path.Combine(RutaAlmacenamiento.Base(configuracion, entorno.ContentRootPath), "mediauploader");

    // Réplica del bloque "If cia = 'EPSS2468' Or cia = 'ESS024' Or ... Then cia =
    // 'EPSS2468'" repetido 3 veces en JuntasCargueDoc.aspx.vb (y una vez más en
    // JuntaProfesional.aspx.vb) — varias EPS comparten un mismo bucket de archivos.
    // Ninguna de esas CIA es la que usa esta app (890330031, EPS Sanitas), así que nunca
    // dispara para un usuario real de Contingencia hoy, pero se replica por si este mismo
    // módulo llega a servir esas otras compañías más adelante.
    private static readonly HashSet<string> CiasEnBucketCompartido =
    [
        "EPSS2468", "ESS024", "EPSS2405", "EPSS2408", "EPSS2413", "EPSS2415", "EPSS2420",
        "EPSS2423", "EPSS2447", "EPSS2454", "EPSS2470", "EPSS2476", "EPSS2494",
    ];

    private static string NormalizarCia(string cia) => CiasEnBucketCompartido.Contains(cia) ? "EPSS2468" : cia;

    private string CarpetaCargue(string cia, string prescripcion) =>
        Path.Combine(_directorioBase, NormalizarCia(Path.GetFileName(cia)), "JuntasP", Path.GetFileName(prescripcion));

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

    public string? RutaFisica(string cia, string prescripcion, string nombreArchivo)
    {
        var ruta = Path.Combine(CarpetaCargue(cia, prescripcion), Path.GetFileName(nombreArchivo));
        return File.Exists(ruta) ? ruta : null;
    }

    public bool TieneArchivos(string cia, string prescripcion)
    {
        var carpeta = CarpetaCargue(cia, prescripcion);
        return Directory.Exists(carpeta) && Directory.EnumerateFiles(carpeta).Any();
    }
}
