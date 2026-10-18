namespace Contingencia.Web.Services;

// Enrutador dinámico por CIA para "Nueva Contingencia" — equivalente a
// FormatosArchivoServiceSelector: decide si los soportes/firma de una CIA se guardan/
// visualizan/eliminan en Azure Blob Storage (mismo contenedor "formatosct" que Formatos,
// pero en la carpeta "Contingencias", ver ContingenciaArchivoServiceAzure) o en el disco
// local (App_Data\mediauploader\contingencias, comportamiento legado). Hoy solo CIA
// 890330031 (Sanitas) migra a Azure; el resto sigue en mediauploader sin cambios.
//
// La lista de CIAs en Azure es 100% configuración (sin tocar código si mañana se suma una
// CIA nueva o se migra otra):
//   "Almacenamiento": { "Azure": { "CiasContingenciasAzure": [ "890330031" ] } }
//
// A diferencia de IFormatosArchivoService (cuyos métodos reciben "cia" explícito),
// IContingenciaArchivoService trabaja con rutas relativas ya armadas ("{cia}/{radicado}/...")
// — el CIA se extrae del primer segmento de la ruta para decidir a qué backend enrutar.
//
// ContingenciaArchivoServiceAzure se resuelve de forma perezosa (Lazy, vía IServiceProvider)
// para que, si ninguna CIA está configurada todavía, nunca se construya — y por lo tanto
// nunca exija "Almacenamiento:Azure:ConnectionString" en los ambientes donde Nueva
// Contingencia aún no usa Azure en absoluto.
public class ContingenciaArchivoServiceSelector : IContingenciaArchivoService
{
    private readonly ContingenciaArchivoService _disco;
    private readonly Lazy<ContingenciaArchivoServiceAzure> _azure;
    private readonly HashSet<string> _ciasAzure;

    public ContingenciaArchivoServiceSelector(ContingenciaArchivoService disco, IServiceProvider servicios, IConfiguration configuracion)
    {
        _disco = disco;
        _azure = new Lazy<ContingenciaArchivoServiceAzure>(servicios.GetRequiredService<ContingenciaArchivoServiceAzure>);
        _ciasAzure = (configuracion.GetSection("Almacenamiento:Azure:CiasContingenciasAzure").Get<string[]>()
                ?? ["890330031"])
            .Select(c => c.Trim())
            .Where(c => c.Length > 0)
            .ToHashSet();
    }

    private IContingenciaArchivoService Resolver(string cia) => _ciasAzure.Contains(cia) ? _azure.Value : _disco;

    // Primer segmento de la ruta relativa ("{cia}/{radicado}/...") — ver comentario de clase.
    private static string CiaDeRuta(string rutaRelativa) => rutaRelativa.Split('/', 2)[0];

    // RutaRelativaSoporte/RutaRelativaFirma son simples constructores de string (sin I/O) y
    // su formato es idéntico en disco y Azure, así que siempre se resuelven contra _disco.
    public string RutaRelativaSoporte(string cia, string radicado, int indice, string extension) =>
        _disco.RutaRelativaSoporte(cia, radicado, indice, extension);

    public string RutaRelativaFirma(string cia, string radicado) =>
        _disco.RutaRelativaFirma(cia, radicado);

    public Task GuardarAsync(string rutaRelativa, Stream contenido) =>
        Resolver(CiaDeRuta(rutaRelativa)).GuardarAsync(rutaRelativa, contenido);

    public Task GuardarBase64PngAsync(string rutaRelativa, string imagenBase64Png) =>
        Resolver(CiaDeRuta(rutaRelativa)).GuardarBase64PngAsync(rutaRelativa, imagenBase64Png);

    public bool Existe(string rutaRelativa) =>
        Resolver(CiaDeRuta(rutaRelativa)).Existe(rutaRelativa);

    public bool Eliminar(string rutaRelativa) =>
        Resolver(CiaDeRuta(rutaRelativa)).Eliminar(rutaRelativa);

    public Task<Stream?> AbrirAsync(string rutaRelativa) =>
        Resolver(CiaDeRuta(rutaRelativa)).AbrirAsync(rutaRelativa);
}
