namespace Contingencia.Infrastructure.Almacenamiento;

// Réplica del bloque "If cia = 'EPSS2468' Or cia = 'ESS024' Or ... Then cia = 'EPSS2468'"
// repetido 3 veces en JuntasCargueDoc.aspx.vb (y una vez más en JuntaProfesional.aspx.vb) —
// varias EPS comparten un mismo bucket de archivos de Juntas. Extraído a un helper único
// para que tanto el almacenamiento en disco (JuntasArchivoService) como el de Azure Blob
// Storage (JuntasArchivoServiceAzure) normalicen la CIA exactamente igual.
public static class JuntasBucketCompartido
{
    private static readonly HashSet<string> CiasEnBucketCompartido =
    [
        "EPSS2468", "ESS024", "EPSS2405", "EPSS2408", "EPSS2413", "EPSS2415", "EPSS2420",
        "EPSS2423", "EPSS2447", "EPSS2454", "EPSS2470", "EPSS2476", "EPSS2494",
    ];

    public static string Normalizar(string cia) => CiasEnBucketCompartido.Contains(cia) ? "EPSS2468" : cia;
}
