using Azure.Storage.Blobs;
using Contingencia.Infrastructure.Almacenamiento;

namespace Contingencia.Web.Services;

// Reemplazo de JuntasArchivoService (disco, App_Data\mediauploader) para el módulo de
// Juntas: los documentos ahora se guardan y se leen de un contenedor de Azure Blob
// Storage. Mismo esquema lógico de "carpeta" ({cia normalizada}/JuntasP/{prescripción}/
// {archivo}), solo que como prefijo del nombre del blob en vez de una ruta en disco.
//
// Configuración esperada (ver appsettings / dotnet user-secrets, nunca en el código ni en
// git):
//   "Almacenamiento": {
//     "Azure": {
//       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
//       "Contenedor": "juntas"
//     }
//   }
public class JuntasArchivoServiceAzure : IJuntasArchivoService
{
    private readonly BlobContainerClient _contenedor;

    public JuntasArchivoServiceAzure(IConfiguration configuracion)
    {
        var cadenaConexion = configuracion["Almacenamiento:Azure:ConnectionString"];
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "Falta configurar 'Almacenamiento:Azure:ConnectionString' (dotnet user-secrets set " +
                "\"Almacenamiento:Azure:ConnectionString\" \"...\" --project Contingencia.Web).");
        }

        var nombreContenedor = configuracion["Almacenamiento:Azure:Contenedor"];
        if (string.IsNullOrWhiteSpace(nombreContenedor))
        {
            nombreContenedor = "juntas";
        }

        var servicioBlob = new BlobServiceClient(cadenaConexion);
        _contenedor = servicioBlob.GetBlobContainerClient(nombreContenedor);
        _contenedor.CreateIfNotExists();
    }

    private static string Prefijo(string cia, string prescripcion) =>
        $"{JuntasBucketCompartido.Normalizar(Path.GetFileName(cia))}/JuntasP/{Path.GetFileName(prescripcion)}/";

    private static string NombreBlob(string cia, string prescripcion, string nombreArchivo) =>
        $"{Prefijo(cia, prescripcion)}{Path.GetFileName(nombreArchivo)}";

    public IReadOnlyList<ArchivoCargue> Listar(string cia, string prescripcion)
    {
        var prefijo = Prefijo(cia, prescripcion);
        return _contenedor.GetBlobs(prefix: prefijo, traits: Azure.Storage.Blobs.Models.BlobTraits.None, states: Azure.Storage.Blobs.Models.BlobStates.None, cancellationToken: default)
            .Select(blob => blob.Name[prefijo.Length..])
            .Select(nombre => new ArchivoCargue(
                nombre,
                $"/api/juntas/cargue?prescripcion={Uri.EscapeDataString(prescripcion)}&archivo={Uri.EscapeDataString(nombre)}"))
            .ToList();
    }

    public async Task GuardarAsync(string cia, string prescripcion, string nombreArchivo, Stream contenido)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(cia, prescripcion, nombreArchivo));
        await blob.UploadAsync(contenido, overwrite: true);
    }

    public bool Eliminar(string cia, string prescripcion, string nombreArchivo)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(cia, prescripcion, nombreArchivo));
        return blob.DeleteIfExists();
    }

    public bool TieneArchivos(string cia, string prescripcion) =>
        _contenedor.GetBlobs(prefix: Prefijo(cia, prescripcion), traits: Azure.Storage.Blobs.Models.BlobTraits.None, states: Azure.Storage.Blobs.Models.BlobStates.None, cancellationToken: default).Any();

    public async Task<HashSet<string>> ObtenerPrescripcionesConArchivosAsync(string cia, IEnumerable<string> prescripciones)
    {
        // Una sola llamada "listar blobs" con el prefijo de la CIA (en vez de una por cada
        // prescripción) — evita N round-trips HTTP a Azure al pintar la grilla de resultados.
        var prefijoCia = $"{JuntasBucketCompartido.Normalizar(Path.GetFileName(cia))}/JuntasP/";
        var candidatas = prescripciones.Select(p => Path.GetFileName(p)).ToHashSet();
        var encontradas = new HashSet<string>();

        await foreach (var blob in _contenedor.GetBlobsAsync(
            prefix: prefijoCia,
            traits: Azure.Storage.Blobs.Models.BlobTraits.None,
            states: Azure.Storage.Blobs.Models.BlobStates.None,
            cancellationToken: default))
        {
            var resto = blob.Name[prefijoCia.Length..];
            var barra = resto.IndexOf('/');
            if (barra <= 0)
            {
                continue;
            }

            var prescripcion = resto[..barra];
            if (candidatas.Contains(prescripcion))
            {
                encontradas.Add(prescripcion);
            }
        }

        return encontradas;
    }

    public async Task<Stream?> AbrirAsync(string cia, string prescripcion, string nombreArchivo)
    {
        var blob = _contenedor.GetBlobClient(NombreBlob(cia, prescripcion, nombreArchivo));
        if (!await blob.ExistsAsync())
        {
            return null;
        }

        var descarga = await blob.DownloadStreamingAsync();
        return descarga.Value.Content;
    }
}
