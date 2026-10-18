using Azure.Storage.Blobs;

namespace Contingencia.Web.Services;

// Equivalente a JuntasArchivoServiceAzure pero para Formatos: las CIAs listadas en
// "Almacenamiento:Azure:CiasFormatosAzure" (ver FormatosArchivoServiceSelector) guardan,
// visualizan y eliminan sus formatos en Azure Blob Storage (contenedor "formatosct" por
// defecto) en vez de App_Data\mediauploader\formatos. Mismo esquema lógico de "carpeta"
// ({cia}/Formatos/{radicado}{extensión}) que ya usa FormatosArchivoService en disco, solo
// que como prefijo del nombre del blob.
//
// Configuración esperada (dotnet user-secrets / variable de entorno, nunca en el código ni
// en git — reutiliza la misma cuenta de Azure que Juntas, solo cambia el contenedor):
//   "Almacenamiento": {
//     "Azure": {
//       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
//       "ContenedorFormatos": "formatosct"
//     }
//   }
public class FormatosArchivoServiceAzure : IFormatosArchivoService
{
    private readonly BlobContainerClient _contenedor;

    public FormatosArchivoServiceAzure(IConfiguration configuracion)
    {
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

    private static string NombreBlob(string cia, string radicado, string extension) =>
        $"{Path.GetFileName(cia)}/Formatos/{Path.GetFileName(radicado)}{Path.GetFileName(extension)}";

    public async Task GuardarAsync(string cia, string radicado, string extension, Stream contenido)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(cia, radicado, extension));
        await blob.UploadAsync(contenido, overwrite: true);
    }

    public async Task<Stream?> AbrirAsync(string cia, string radicado, string extension)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(cia, radicado, extension));
        if (!await blob.ExistsAsync())
        {
            return null;
        }

        var descarga = await blob.DownloadStreamingAsync();
        return descarga.Value.Content;
    }

    public bool Eliminar(string cia, string radicado, string extension)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(cia, radicado, extension));
        return blob.DeleteIfExists();
    }

    public bool Existe(string cia, string radicado, string extension) =>
        _contenedor.GetBlobClient(NombreBlob(cia, radicado, extension)).Exists();
}
