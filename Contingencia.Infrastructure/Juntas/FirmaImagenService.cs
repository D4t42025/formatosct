using Contingencia.Infrastructure.Almacenamiento;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Contingencia.Infrastructure.Juntas;

// La columna Firma puede apuntar a dos sitios distintos, con la misma forma de ruta
// relativa (MediaUploader/Files-Gestion-Usuarios/Firmas/<cia>/<usuario>/Firma.<ext>):
//   - Firmas nuevas, capturadas desde /profesionales (esta app): quedan en disco local,
//     bajo App_Data/ — ver ProfesionalFirmaArchivoService.
//   - Firmas viejas, capturadas desde MIP_PROFESIONALES.aspx (legado, todavía no migrado):
//     solo existen en el disco del legado, servidas por HTTP sin autenticación.
// Por eso primero se busca en disco local, y solo si no está ahí se intenta por HTTP.
public class FirmaImagenService(HttpClient httpClient, IConfiguration configuration, IHostEnvironment entorno) : IFirmaImagenService
{
    public async Task<byte[]?> DescargarAsync(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return null;

        var ruta = rutaRelativa.Trim().Replace('\\', '/').TrimStart('~').TrimStart('/');

        var rutaLocal = Path.Combine(RutaAlmacenamiento.Base(configuration, entorno.ContentRootPath), ruta.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(rutaLocal))
        {
            try
            {
                return await File.ReadAllBytesAsync(rutaLocal);
            }
            catch
            {
                return null;
            }
        }

        var baseUrl = configuration["Legacy:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;

        try
        {
            var url = $"{baseUrl.TrimEnd('/')}/{ruta}";

            using var respuesta = await httpClient.GetAsync(url);
            if (!respuesta.IsSuccessStatusCode) return null;

            return await respuesta.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return null;
        }
    }
}
