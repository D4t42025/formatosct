namespace Contingencia.Web.Services;

// Enrutador dinámico por CIA: decide si los formatos de una CIA se guardan/visualizan/
// eliminan en Azure Blob Storage (contenedor "formatosct") o en el disco local
// (App_Data\mediauploader\formatos, comportamiento legado). Hoy solo CIA 890330031
// (Sanitas) migra a Azure; el resto sigue en mediauploader sin cambios.
//
// La lista de CIAs en Azure es 100% configuración (sin tocar código si mañana se suma una
// CIA nueva o se migra otra):
//   "Almacenamiento": { "Azure": { "CiasFormatosAzure": [ "890330031" ] } }
//
// FormatosArchivoServiceAzure se resuelve de forma perezosa (Lazy, vía IServiceProvider)
// para que, si ninguna CIA está configurada todavía, nunca se construya — y por lo tanto
// nunca exija "Almacenamiento:Azure:ConnectionString" en los ambientes donde Formatos aún
// no usa Azure en absoluto.
public class FormatosArchivoServiceSelector : IFormatosArchivoService
{
    private readonly FormatosArchivoService _disco;
    private readonly Lazy<FormatosArchivoServiceAzure> _azure;
    private readonly HashSet<string> _ciasAzure;

    public FormatosArchivoServiceSelector(FormatosArchivoService disco, IServiceProvider servicios, IConfiguration configuracion)
    {
        _disco = disco;
        _azure = new Lazy<FormatosArchivoServiceAzure>(servicios.GetRequiredService<FormatosArchivoServiceAzure>);
        _ciasAzure = (configuracion.GetSection("Almacenamiento:Azure:CiasFormatosAzure").Get<string[]>()
                ?? ["890330031"])
            .Select(c => c.Trim())
            .Where(c => c.Length > 0)
            .ToHashSet();
    }

    private IFormatosArchivoService Resolver(string cia) => _ciasAzure.Contains(cia) ? _azure.Value : _disco;

    public Task GuardarAsync(string cia, string radicado, string extension, Stream contenido) =>
        Resolver(cia).GuardarAsync(cia, radicado, extension, contenido);

    public Task<Stream?> AbrirAsync(string cia, string radicado, string extension) =>
        Resolver(cia).AbrirAsync(cia, radicado, extension);

    public bool Eliminar(string cia, string radicado, string extension) =>
        Resolver(cia).Eliminar(cia, radicado, extension);

    public bool Existe(string cia, string radicado, string extension) =>
        Resolver(cia).Existe(cia, radicado, extension);
}
