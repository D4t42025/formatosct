using Contingencia.Infrastructure.Almacenamiento;
namespace Contingencia.Web.Services;

// "mediauploader"/"contingencias" en minúsculas — misma convención Linux-safe que
// FormatosArchivoService/ProfesionalFirmaArchivoService. Estructura física:
// App_Data/mediauploader/contingencias/{cia}/{radicado}/soporte_{indice}{ext} | firma.png
public class ContingenciaArchivoService(IWebHostEnvironment entorno, IConfiguration configuracion) : IContingenciaArchivoService
{
    private readonly string _directorioBase = Path.Combine(RutaAlmacenamiento.Base(configuracion, entorno.ContentRootPath), "mediauploader", "contingencias");

    public string RutaRelativaSoporte(string cia, string radicado, int indice, string extension) =>
        $"{cia}/{radicado}/soporte_{indice}{extension}";

    public string RutaRelativaFirma(string cia, string radicado) => $"{cia}/{radicado}/firma.png";

    public string RutaFisica(string rutaRelativa) =>
        Path.Combine(_directorioBase, rutaRelativa.Replace('/', Path.DirectorySeparatorChar));

    public async Task GuardarAsync(string rutaRelativa, Stream contenido)
    {
        var rutaFisica = RutaFisica(rutaRelativa);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);
        await using var destino = File.Create(rutaFisica);
        await contenido.CopyToAsync(destino);
    }

    public async Task GuardarBase64PngAsync(string rutaRelativa, string imagenBase64Png)
    {
        var rutaFisica = RutaFisica(rutaRelativa);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);

        // El data URI llega como "data:image/png;base64,AAAA..." — se descarta todo lo que
        // no sea Base64 (mismo criterio que ProfesionalFirmaArchivoService.GuardarDibujoAsync).
        var indiceComa = imagenBase64Png.IndexOf(',');
        var datos = indiceComa >= 0 ? imagenBase64Png[(indiceComa + 1)..] : imagenBase64Png;
        await File.WriteAllBytesAsync(rutaFisica, Convert.FromBase64String(datos));
    }

    public bool Existe(string rutaRelativa) => File.Exists(RutaFisica(rutaRelativa));
}
