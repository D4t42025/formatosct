using Contingencia.Infrastructure.Almacenamiento;
namespace Contingencia.Web.Services;

public class ProfesionalFirmaArchivoService(IWebHostEnvironment entorno, IConfiguration configuracion) : IProfesionalFirmaArchivoService
{
    // "mediauploader" en minúsculas para seguir la misma convención que
    // FormatosArchivoService/JuntasArchivoService (Linux es sensible a mayúsculas) — el resto
    // del segmento imita el nombre real de la carpeta del legado solo por trazabilidad, no
    // porque haga falta que coincida exactamente (el recorrido local de escritura/lectura de
    // esta clase e IFirmaImagenService siempre usa este mismo valor, nunca el del legado).
    private const string CarpetaBase = "mediauploader/Files-Gestion-Usuarios/Firmas";
    private static readonly string[] ExtensionesPermitidas = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".pdf"];

    public async Task<string> GuardarArchivoAsync(string cia, string clave, string nombreArchivoOriginal, Stream contenido)
    {
        var extension = Path.GetExtension(nombreArchivoOriginal).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            throw new InvalidOperationException("Formato de archivo no permitido para la firma.");
        }

        var rutaRelativa = $"{CarpetaBase}/{SoloNombreDeCarpeta(cia)}/{SoloNombreDeCarpeta(clave)}/Firma{extension}";
        var rutaFisica = RutaFisica(rutaRelativa);

        LimpiarAnteriores(rutaFisica);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);

        await using var destino = File.Create(rutaFisica);
        await contenido.CopyToAsync(destino);

        return rutaRelativa;
    }

    public async Task<string> GuardarDibujoAsync(string cia, string clave, string imagenBase64Png)
    {
        var rutaRelativa = $"{CarpetaBase}/{SoloNombreDeCarpeta(cia)}/{SoloNombreDeCarpeta(clave)}/Firma.png";
        var rutaFisica = RutaFisica(rutaRelativa);

        LimpiarAnteriores(rutaFisica);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);

        // El data URI llega como "data:image/png;base64,AAAA..." — se descarta todo lo que
        // no sea Base64.
        var indiceComa = imagenBase64Png.IndexOf(',');
        var datos = indiceComa >= 0 ? imagenBase64Png[(indiceComa + 1)..] : imagenBase64Png;
        await File.WriteAllBytesAsync(rutaFisica, Convert.FromBase64String(datos));

        return rutaRelativa;
    }

    public async Task<string?> ObtenerDataUriAsync(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return null;

        var rutaFisica = RutaFisica(rutaRelativa);
        if (!File.Exists(rutaFisica)) return null;

        var bytes = await File.ReadAllBytesAsync(rutaFisica);
        var tipo = Path.GetExtension(rutaFisica).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            _ => null,
        };

        // Un PDF cargado como firma no tiene preview de imagen — el legado tampoco lo
        // muestra inline, solo ofrece el enlace de descarga (hlFirmaArchivo).
        return tipo is null ? null : $"data:{tipo};base64,{Convert.ToBase64String(bytes)}";
    }

    private string RutaFisica(string rutaRelativa) =>
        Path.Combine(RutaAlmacenamiento.Base(configuracion, entorno.ContentRootPath), rutaRelativa.Replace('/', Path.DirectorySeparatorChar));

    // Se borra cualquier Firma.* anterior (otra extensión, o la que se hubiera dibujado
    // antes de decidir subir un archivo) — mismo criterio que btnSubirFirma_Click/
    // GuardarFirma en el legado: el archivo siempre se llama igual.
    private static void LimpiarAnteriores(string rutaFisicaNueva)
    {
        var carpeta = Path.GetDirectoryName(rutaFisicaNueva)!;
        if (!Directory.Exists(carpeta)) return;

        foreach (var anterior in Directory.GetFiles(carpeta, "Firma.*"))
        {
            File.Delete(anterior);
        }
    }

    // Nada que pueda salirse del directorio de firmas.
    private static string SoloNombreDeCarpeta(string valor) =>
        string.Concat(valor.Trim().Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_'));
}
