using Azure.Storage.Blobs;

namespace Contingencia.Web.Services;

// Equivalente a FormatosArchivoServiceAzure pero para el wizard "Nueva Contingencia": las
// CIAs listadas en "Almacenamiento:Azure:CiasContingenciasAzure" (ver
// ContingenciaArchivoServiceSelector) guardan, visualizan y eliminan sus soportes/firma en
// Azure Blob Storage en vez de App_Data\mediauploader\contingencias.
//
// Usa la MISMA cuenta/contenedor que Formatos ("formatosct", configurado en
// "Almacenamiento:Azure:ContenedorFormatos") pero en una CARPETA distinta dentro del
// contenedor para no mezclar ambos tipos de documento:
//   Formatos:            {cia}/Formatos/{radicado}{extensión}
//   Nueva Contingencia:  {cia}/Contingencias/{radicado}/soporte_{indice}{extensión} | firma.png
//
// Las rutas relativas que recibe este servicio ya vienen armadas como "{cia}/{radicado}/..."
// (ver RutaRelativaSoporte/RutaRelativaFirma en ContingenciaArchivoService, que este
// servicio reutiliza tal cual porque son simples constructores de string sin I/O); acá solo
// se les inserta "Contingencias/" después del primer segmento (CIA) para formar el nombre
// del blob.
public class ContingenciaArchivoServiceAzure : IContingenciaArchivoService
{
    private readonly ContingenciaArchivoService _constructorRutas;
    private readonly BlobContainerClient _contenedor;

    public ContingenciaArchivoServiceAzure(IConfiguration configuracion, ContingenciaArchivoService constructorRutas)
    {
        _constructorRutas = constructorRutas;

        var cadenaConexion = configuracion["Almacenamiento:Azure:ConnectionString"];
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "Falta configurar 'Almacenamiento:Azure:ConnectionString' (dotnet user-secrets set " +
                "\"Almacenamiento:Azure:ConnectionString\" \"...\" --project Contingencia.Web).");
        }

        var nombreContenedor = configuracion["Almacenamiento:Azure:ContenedorFormatos"];
        if (string.IsNullOrWhiteSpace(nombreContenedor))
        {
            nombreContenedor = "formatosct";
        }

        var servicioBlob = new BlobServiceClient(cadenaConexion);
        _contenedor = servicioBlob.GetBlobContainerClient(nombreContenedor);
        _contenedor.CreateIfNotExists();
    }

    // Inserta "Contingencias/" después del primer segmento (CIA) de la ruta relativa, p.ej.
    // "890330031/202601001/soporte_1.pdf" -> "890330031/Contingencias/202601001/soporte_1.pdf".
    private static string NombreBlob(string rutaRelativa)
    {
        var partes = rutaRelativa.Split('/', 2);
        return partes.Length == 2
            ? $"{partes[0]}/Contingencias/{partes[1]}"
            : $"Contingencias/{rutaRelativa}";
    }

    public string RutaRelativaSoporte(string cia, string radicado, int indice, string extension) =>
        _constructorRutas.RutaRelativaSoporte(cia, radicado, indice, extension);

    public string RutaRelativaFirma(string cia, string radicado) =>
        _constructorRutas.RutaRelativaFirma(cia, radicado);

    public async Task GuardarAsync(string rutaRelativa, Stream contenido)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(rutaRelativa));
        await blob.UploadAsync(contenido, overwrite: true);
    }

    public async Task GuardarBase64PngAsync(string rutaRelativa, string imagenBase64Png)
    {
        // Mismo criterio que ContingenciaArchivoService.GuardarBase64PngAsync: el data URI
        // llega como "data:image/png;base64,AAAA..." — se descarta todo lo que no sea Base64.
        var indiceComa = imagenBase64Png.IndexOf(',');
        var datos = indiceComa >= 0 ? imagenBase64Png[(indiceComa + 1)..] : imagenBase64Png;
        using var contenido = new MemoryStream(Convert.FromBase64String(datos));
        var blob = _contenedor.GetBlobClient(NombreBlob(rutaRelativa));
        await blob.UploadAsync(contenido, overwrite: true);
    }

    public bool Existe(string rutaRelativa) =>
        _contenedor.GetBlobClient(NombreBlob(rutaRelativa)).Exists();

    public bool Eliminar(string rutaRelativa) =>
        _contenedor.GetBlobClient(NombreBlob(rutaRelativa)).DeleteIfExists();

    public async Task<Stream?> AbrirAsync(string rutaRelativa)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(rutaRelativa));
        if (!await blob.ExistsAsync())
        {
            return null;
        }

        var descarga = await blob.DownloadStreamingAsync();
        return descarga.Value.Content;
    }
}
