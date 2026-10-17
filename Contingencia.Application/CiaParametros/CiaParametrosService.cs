using System.Data;
using Contingencia.Infrastructure.CiaParametros;

namespace Contingencia.Application.CiaParametros;

public class CiaParametrosService(ICiaParametrosRepository repository) : ICiaParametrosService
{
    public const string ClaveAfiliadosOrigen = "AFILIADOS_ORIGEN";
    public const string ClaveAfiliadosEntidad = "AFILIADOS_ENTIDAD";
    public const string ClavePrestadoresNit = "PRESTADORES_NIT";
    public const string ClaveCodigosCatalogo = "CODIGOS_CATALOGO";
    public const string ClaveAfiliadosWsUrl = "AFILIADOS_WS_URL";

    public async Task<ParametrosCia> ObtenerAsync(string cia)
    {
        var tabla = await repository.ConsultarAsync(cia);
        var valores = tabla.AsEnumerable().ToDictionary(
            f => Convert.ToString(f["CLAVE"]) ?? "",
            f => Convert.ToString(f["VALOR"]) ?? "",
            StringComparer.OrdinalIgnoreCase);

        var origen = valores.GetValueOrDefault(ClaveAfiliadosOrigen)?.Trim();
        var entidad = valores.GetValueOrDefault(ClaveAfiliadosEntidad)?.Trim();
        var nit = valores.GetValueOrDefault(ClavePrestadoresNit)?.Trim();
        var catalogo = valores.GetValueOrDefault(ClaveCodigosCatalogo)?.Trim();
        var urlAfiliados = valores.GetValueOrDefault(ClaveAfiliadosWsUrl)?.Trim();

        return new ParametrosCia(
            string.Equals(origen, "WS", StringComparison.OrdinalIgnoreCase) ? OrigenAfiliados.WebService : OrigenAfiliados.Manual,
            string.IsNullOrWhiteSpace(entidad) ? null : entidad,
            string.IsNullOrWhiteSpace(nit) ? null : nit,
            string.IsNullOrWhiteSpace(catalogo) ? ParametrosCia.CatalogoPorDefecto : catalogo,
            string.IsNullOrWhiteSpace(urlAfiliados) ? null : urlAfiliados);
    }
}
